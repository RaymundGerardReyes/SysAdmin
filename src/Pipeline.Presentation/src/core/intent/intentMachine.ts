// Rules: L-03 Pure core, L-04 Make illegal states unrepresentable, B-02 No auto-retry on timeout
import { IntentState, IntentEvent } from './intent.types';
import { createAppError } from '../../contracts/errors/appError';

export const INITIAL_INTENT_STATE: IntentState = { phase: 'Idle' };

export function intentReducer(state: IntentState, event: IntentEvent): IntentState {
  switch (state.phase) {
    case 'Idle': {
      if (event.type === 'START') {
        return {
          phase: 'Validating',
          correlationId: event.correlationId,
          idempotencyKey: event.idempotencyKey,
        };
      }
      break;
    }

    case 'Validating': {
      if (event.type === 'VALID') {
        return {
          phase: 'Submitting',
          correlationId: state.correlationId,
          idempotencyKey: state.idempotencyKey,
        };
      }
      if (event.type === 'INVALID') {
        return {
          phase: 'Failed',
          correlationId: state.correlationId,
          error: createAppError('VALIDATION_FAILED', 'Validation failed.', state.correlationId),
        };
      }
      break;
    }

    case 'AwaitingConfirm': {
      if (event.type === 'CONFIRMED') {
        return {
          phase: 'Submitting',
          correlationId: state.correlationId,
          idempotencyKey: state.idempotencyKey,
        };
      }
      if (event.type === 'RESET') {
        return INITIAL_INTENT_STATE;
      }
      break;
    }

    case 'Submitting': {
      if (event.type === 'SUCCEEDED') {
        return {
          phase: 'Succeeded',
          correlationId: state.correlationId,
          data: event.data,
        };
      }
      if (event.type === 'CHALLENGED') {
        return {
          phase: 'AwaitingStepUp',
          correlationId: state.correlationId,
          idempotencyKey: state.idempotencyKey, // Retains same key
        };
      }
      if (event.type === 'PENDING_APPROVAL') {
        return {
          phase: 'PendingApproval',
          correlationId: state.correlationId,
          idempotencyKey: state.idempotencyKey,
          approvalId: event.approvalId,
        };
      }
      if (event.type === 'TIMED_OUT') {
        // Rule B-02: Timeout on write transitions to OutcomeUnknown, NEVER auto-retries!
        return {
          phase: 'OutcomeUnknown',
          correlationId: state.correlationId,
          idempotencyKey: state.idempotencyKey,
        };
      }
      if (event.type === 'FAILED') {
        return {
          phase: 'Failed',
          correlationId: state.correlationId,
          error: event.error,
        };
      }
      break;
    }

    case 'AwaitingStepUp': {
      if (event.type === 'CONFIRMED') {
        return {
          phase: 'Submitting',
          correlationId: state.correlationId,
          idempotencyKey: state.idempotencyKey, // Submits with SAME key
        };
      }
      if (event.type === 'RESET') {
        return INITIAL_INTENT_STATE;
      }
      break;
    }

    case 'PendingApproval': {
      if (event.type === 'RESET') {
        return INITIAL_INTENT_STATE;
      }
      break;
    }

    case 'OutcomeUnknown': {
      if (event.type === 'RESET') {
        return INITIAL_INTENT_STATE;
      }
      break;
    }

    case 'Succeeded': {
      if (event.type === 'RESET') {
        return INITIAL_INTENT_STATE;
      }
      break;
    }

    case 'Failed': {
      if (event.type === 'RESET') {
        return INITIAL_INTENT_STATE;
      }
      break;
    }
  }

  throw new Error(`Illegal state transition from phase '${state.phase}' with event '${event.type}'`);
}
