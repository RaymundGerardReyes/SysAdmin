import { CorrelationId } from '../ids/correlationId';
import { IdempotencyKey } from '../ids/idempotencyKey';
import { AppError } from '../../contracts/errors/appError';

export type IntentPhase =
  | 'Idle'
  | 'Validating'
  | 'AwaitingConfirm'
  | 'Submitting'
  | 'AwaitingStepUp'
  | 'PendingApproval'
  | 'OutcomeUnknown'
  | 'Succeeded'
  | 'Failed';

export type IntentState =
  | { readonly phase: 'Idle' }
  | { readonly phase: 'Validating'; readonly correlationId: CorrelationId; readonly idempotencyKey: IdempotencyKey }
  | { readonly phase: 'AwaitingConfirm'; readonly correlationId: CorrelationId; readonly idempotencyKey: IdempotencyKey }
  | { readonly phase: 'Submitting'; readonly correlationId: CorrelationId; readonly idempotencyKey: IdempotencyKey }
  | { readonly phase: 'AwaitingStepUp'; readonly correlationId: CorrelationId; readonly idempotencyKey: IdempotencyKey }
  | { readonly phase: 'PendingApproval'; readonly correlationId: CorrelationId; readonly idempotencyKey: IdempotencyKey; readonly approvalId: string }
  | { readonly phase: 'OutcomeUnknown'; readonly correlationId: CorrelationId; readonly idempotencyKey: IdempotencyKey }
  | { readonly phase: 'Succeeded'; readonly correlationId: CorrelationId; readonly data?: unknown }
  | { readonly phase: 'Failed'; readonly correlationId: CorrelationId; readonly error: AppError };

export type IntentEvent =
  | { readonly type: 'START'; readonly correlationId: CorrelationId; readonly idempotencyKey: IdempotencyKey; readonly requiresConfirm?: boolean }
  | { readonly type: 'VALID' }
  | { readonly type: 'INVALID'; readonly errors: Record<string, string> }
  | { readonly type: 'CONFIRMED' }
  | { readonly type: 'SUCCEEDED'; readonly data?: unknown }
  | { readonly type: 'CHALLENGED'; readonly reasonCode: string }
  | { readonly type: 'PENDING_APPROVAL'; readonly approvalId: string }
  | { readonly type: 'TIMED_OUT' }
  | { readonly type: 'FAILED'; readonly error: AppError }
  | { readonly type: 'RESET' };
