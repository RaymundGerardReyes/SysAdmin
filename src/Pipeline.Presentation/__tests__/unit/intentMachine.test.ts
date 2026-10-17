// Rules: L-03 Pure core, L-04 Make illegal states unrepresentable, B-02 Timeout policy, R-07 Idempotency
import { intentReducer, INITIAL_INTENT_STATE } from '../../src/core/intent/intentMachine';
import { createCorrelationId } from '../../src/core/ids/correlationId';
import { createIdempotencyKey } from '../../src/core/ids/idempotencyKey';
import { createAppError } from '../../src/contracts/errors/appError';

describe('intentMachine', () => {
  it('[L-04] transitions from Idle to Validating to Submitting to Succeeded', () => {
    const correlationId = createCorrelationId();
    const idempotencyKey = createIdempotencyKey();

    let state = intentReducer(INITIAL_INTENT_STATE, {
      type: 'START',
      correlationId,
      idempotencyKey,
    });
    expect(state.phase).toBe('Validating');

    state = intentReducer(state, { type: 'VALID' });
    expect(state.phase).toBe('Submitting');

    state = intentReducer(state, { type: 'SUCCEEDED', data: { ok: true } });
    expect(state.phase).toBe('Succeeded');
  });

  it('[B-02] timeout on write transitions to OutcomeUnknown and never auto-retries', () => {
    const correlationId = createCorrelationId();
    const idempotencyKey = createIdempotencyKey();

    let state = intentReducer(INITIAL_INTENT_STATE, {
      type: 'START',
      correlationId,
      idempotencyKey,
    });
    state = intentReducer(state, { type: 'VALID' });
    expect(state.phase).toBe('Submitting');

    state = intentReducer(state, { type: 'TIMED_OUT' });
    expect(state.phase).toBe('OutcomeUnknown');
    if (state.phase === 'OutcomeUnknown') {
      expect(state.idempotencyKey).toBe(idempotencyKey);
    }
  });

  it('[R-07] step-up challenge transitions to AwaitingStepUp and retains same idempotency key', () => {
    const correlationId = createCorrelationId();
    const idempotencyKey = createIdempotencyKey();

    let state = intentReducer(INITIAL_INTENT_STATE, {
      type: 'START',
      correlationId,
      idempotencyKey,
    });
    state = intentReducer(state, { type: 'VALID' });
    state = intentReducer(state, { type: 'CHALLENGED', reasonCode: 'STEPUP_REQUIRED' });

    expect(state.phase).toBe('AwaitingStepUp');
    if (state.phase === 'AwaitingStepUp') {
      expect(state.idempotencyKey).toBe(idempotencyKey);
    }

    // After step-up re-auth, user confirms and resumes with SAME key
    state = intentReducer(state, { type: 'CONFIRMED' });
    expect(state.phase).toBe('Submitting');
    if (state.phase === 'Submitting') {
      expect(state.idempotencyKey).toBe(idempotencyKey);
    }
  });

  it('[L-04] requiresConfirm transitions to AwaitingConfirm and resumes to Submitting on CONFIRMED', () => {
    const correlationId = createCorrelationId();
    const idempotencyKey = createIdempotencyKey();

    let state = intentReducer(INITIAL_INTENT_STATE, {
      type: 'START',
      correlationId,
      idempotencyKey,
      requiresConfirm: true,
    });
    expect(state.phase).toBe('Validating');

    state = intentReducer(state, { type: 'VALID' });
    expect(state.phase).toBe('AwaitingConfirm');

    state = intentReducer(state, { type: 'CONFIRMED' });
    expect(state.phase).toBe('Submitting');
  });

  it('[L-04] throws an error on illegal state transitions', () => {
    expect(() => {
      intentReducer(INITIAL_INTENT_STATE, { type: 'SUCCEEDED' });
    }).toThrow(/Illegal state transition/);
  });
});
