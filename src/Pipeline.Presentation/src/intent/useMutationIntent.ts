// Rules: L-03 Pure core, L-04 Make illegal states unrepresentable, B-02 No auto-retry on timeout, R-07 Idempotency
import { useState, useCallback, useRef } from 'react';
import { intentReducer, INITIAL_INTENT_STATE } from '../core/intent/intentMachine';
import { IntentState, IntentEvent } from '../core/intent/intent.types';
import { createCorrelationId, CorrelationId } from '../core/ids/correlationId';
import { createIdempotencyKey, IdempotencyKey } from '../core/ids/idempotencyKey';
import { Result } from '../core/result';
import { AppError } from '../contracts/errors/appError';
import { track } from '../observability/track';
import { usePendingOperationsStore } from '../state/stores/pendingOperationsStore';

export function useMutationIntent<TPayload, TResult>(
  mutationFn: (
    payload: TPayload,
    correlationId: CorrelationId,
    idempotencyKey: IdempotencyKey
  ) => Promise<Result<TResult, AppError>>,
  commandLabel = 'operation'
) {
  const [state, setState] = useState<IntentState>(INITIAL_INTENT_STATE);
  const pendingRef = useRef<{
    payload: TPayload;
    correlationId: CorrelationId;
    idempotencyKey: IdempotencyKey;
  } | null>(null);

  const addPending = usePendingOperationsStore((s) => s.add);
  const removePending = usePendingOperationsStore((s) => s.remove);

  const dispatch = useCallback((event: IntentEvent) => {
    setState((prev) => {
      const next = intentReducer(prev, event);
      track(`intent.${next.phase.toLowerCase()}` as any, { phase: next.phase });
      return next;
    });
  }, []);

  const runSubmission = useCallback(
    async (
      payload: TPayload,
      correlationId: CorrelationId,
      idempotencyKey: IdempotencyKey
    ) => {
      addPending({
        idempotencyKey,
        correlationId,
        command: commandLabel,
        startedAt: Date.now(),
      });

      const res = await mutationFn(payload, correlationId, idempotencyKey);
      removePending(idempotencyKey);

      if (res.ok) {
        pendingRef.current = null;
        dispatch({ type: 'SUCCEEDED', data: res.value });
      } else {
        if (res.error.code === 'STEPUP_REQUIRED') {
          dispatch({ type: 'CHALLENGED', reasonCode: res.error.code });
        } else if (res.error.code === 'APPROVAL_REQUIRED') {
          dispatch({ type: 'PENDING_APPROVAL', approvalId: res.error.approvalId ?? '' });
        } else if (res.error.code === 'TIMEOUT') {
          dispatch({ type: 'TIMED_OUT' });
        } else {
          dispatch({ type: 'FAILED', error: res.error });
        }
      }
    },
    [addPending, commandLabel, dispatch, mutationFn, removePending]
  );

  const execute = useCallback(
    async (payload: TPayload, requiresConfirm = false) => {
      const correlationId = createCorrelationId();
      const idempotencyKey = createIdempotencyKey();
      pendingRef.current = { payload, correlationId, idempotencyKey };

      dispatch({ type: 'START', correlationId, idempotencyKey, requiresConfirm });
      dispatch({ type: 'VALID', requiresConfirm });

      if (requiresConfirm) {
        return;
      }

      await runSubmission(payload, correlationId, idempotencyKey);
    },
    [dispatch, runSubmission]
  );

  const confirm = useCallback(async () => {
    const current = pendingRef.current;
    if (!current) return;
    dispatch({ type: 'CONFIRMED' });
    await runSubmission(current.payload, current.correlationId, current.idempotencyKey);
  }, [dispatch, runSubmission]);

  const reset = useCallback(() => {
    pendingRef.current = null;
    dispatch({ type: 'RESET' });
  }, [dispatch]);

  return { state, execute, confirm, reset, dispatch };
}
