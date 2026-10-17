import { useState, useCallback } from 'react';
import { intentReducer, INITIAL_INTENT_STATE } from '../core/intent/intentMachine';
import { IntentState, IntentEvent } from '../core/intent/intent.types';
import { createCorrelationId, CorrelationId } from '../core/ids/correlationId';
import { createIdempotencyKey, IdempotencyKey } from '../core/ids/idempotencyKey';
import { Result } from '../core/result';
import { AppError } from '../contracts/errors/appError';
import { track } from '../observability/track';

export function useMutationIntent<TPayload, TResult>(
  mutationFn: (payload: TPayload, correlationId: CorrelationId, idempotencyKey: IdempotencyKey) => Promise<Result<TResult, AppError>>
) {
  const [state, setState] = useState<IntentState>(INITIAL_INTENT_STATE);

  const dispatch = useCallback((event: IntentEvent) => {
    setState((prev) => {
      const next = intentReducer(prev, event);
      track(`intent.${next.phase.toLowerCase()}` as any, { phase: next.phase });
      return next;
    });
  }, []);

  const execute = useCallback(
    async (payload: TPayload, requiresConfirm = false) => {
      const correlationId = createCorrelationId();
      const idempotencyKey = createIdempotencyKey();

      dispatch({ type: 'START', correlationId, idempotencyKey, requiresConfirm });
      dispatch({ type: 'VALID' });

      if (requiresConfirm) {
        // Form displays confirm step
        return;
      }

      const res = await mutationFn(payload, correlationId, idempotencyKey);
      if (res.ok) {
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
    [dispatch, mutationFn]
  );

  return { state, execute, dispatch };
}
