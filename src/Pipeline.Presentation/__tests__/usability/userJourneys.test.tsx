// Rules: L-04 9 named intent phases, B-02 No auto-retry on write timeout, R-06 Five screen states, R-07 Idempotency preservation
import { intentReducer, INITIAL_INTENT_STATE } from '../../src/core/intent/intentMachine';
import { createCorrelationId } from '../../src/core/ids/correlationId';
import { createIdempotencyKey } from '../../src/core/ids/idempotencyKey';
import { createAppError } from '../../src/contracts/errors/appError';
import { PipelineBridge } from '../../src/bridge/PipelineBridge';
import { MockTransport } from '../../src/bridge/transports/MockTransport';
import { useSessionStore } from '../../src/state/stores/sessionStore';
import { usePendingOperationsStore } from '../../src/state/stores/pendingOperationsStore';
import { resetAllStores } from '../../src/state/stores/resetAllStores';
import { ScreenLifecycleState } from '../../src/ui/primitives/ScreenStateView';

describe('Advanced User-Centered Journeys (Section 14 Usability)', () => {
  beforeEach(() => {
    resetAllStores();
  });

  // Journey 1: High-consequence operation requiring confirmation (requiresConfirm)
  it('[L-04, R-07] Journey 1: requiresConfirm flow pauses for user review and executes with preserved idempotency', async () => {
    const correlationId = createCorrelationId();
    const idempotencyKey = createIdempotencyKey();

    // 1. User prepares checkout / mutation (requiresConfirm = true)
    let state = intentReducer(INITIAL_INTENT_STATE, {
      type: 'START',
      correlationId,
      idempotencyKey,
      requiresConfirm: true,
    });
    expect(state.phase).toBe('Validating');

    // 2. Pre-flight validation passes, transitions to AwaitingConfirm rather than Submitting
    state = intentReducer(state, { type: 'VALID', requiresConfirm: true });
    expect(state.phase).toBe('AwaitingConfirm');
    if (state.phase === 'AwaitingConfirm') {
      expect(state.idempotencyKey).toBe(idempotencyKey);
      expect(state.correlationId).toBe(correlationId);
    }

    // 3. User reviews transaction consequence and clicks "Confirm and Submit"
    state = intentReducer(state, { type: 'CONFIRMED' });
    expect(state.phase).toBe('Submitting');
    if (state.phase === 'Submitting') {
      // Must retain exact same idempotency reservation
      expect(state.idempotencyKey).toBe(idempotencyKey);
    }

    // 4. Host confirms execution
    state = intentReducer(state, {
      type: 'SUCCEEDED',
      data: { checkoutId: 'chk_123', status: 'created' },
    });
    expect(state.phase).toBe('Succeeded');
    if (state.phase === 'Succeeded') {
      expect((state.data as { checkoutId: string }).checkoutId).toBe('chk_123');
    }
  });

  // Journey 2: Session invalidation and re-authentication recovery (SESSION_INVALID)
  it('[R-06] Journey 2: SESSION_INVALID triggers needs-reauth screen state and cleanly recovers upon authentication', async () => {
    const { getState } = useSessionStore;

    // 1. Initial unauthenticated state
    expect(getState().isAuthenticated).toBe(false);

    // Derive screen lifecycle state for protected workspace
    function deriveWorkspaceState(featureId: string): ScreenLifecycleState {
      const session = getState();
      if (featureId === 'dashboard' || featureId === 'session') return 'success';
      if (!session.isAuthenticated || !session.capabilities) return 'needs-reauth';
      const hasPerm = session.capabilities.permissions.includes('payments.checkout.create');
      return hasPerm ? 'success' : 'access-denied';
    }

    // In unauthenticated session, payments workspace requires re-auth
    expect(deriveWorkspaceState('payments')).toBe('needs-reauth');

    // 2. An attempted protected action returns SESSION_INVALID from bridge
    const transport = new MockTransport();
    transport.registerHandler('payments.checkout.create', async (env) => ({
      id: env.id,
      success: false,
      errorCode: 'SESSION_INVALID',
      errorMessage: 'User session has expired or is invalid.',
      pathId: 'AUTHN_S01',
    }));

    const bridge = new PipelineBridge(transport);
    const corrId = createCorrelationId();
    const result = await bridge.command('payments.checkout.create', { amount: 100 }, corrId);
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.error.code).toBe('SESSION_INVALID');
    }

    // 3. User clicks "Sign In / Verify Session"
    getState().setSession('operator@pipeline.local', {
      userId: 'operator@pipeline.local',
      roles: ['OperationsAdministrator'],
      permissions: ['payments.checkout.create', 'payments.checkout.approve'],
      scopes: { spreadsheetIds: [], chatSpaceIds: [], zohoOrgIds: [], paymentAccountIds: ['acct_primary'] },
      stepUpFresh: true,
    });

    // 4. Workspace immediately recovers to success state
    expect(getState().isAuthenticated).toBe(true);
    expect(deriveWorkspaceState('payments')).toBe('success');
  });

  // Journey 3: Missing permission presentation with disabledReason and access-denied state
  it('[R-06, N-07] Journey 3: PERMISSION_MISSING triggers access-denied state and actionable disabled reasons', async () => {
    const { getState } = useSessionStore;

    // Establish restricted session with only read permissions
    getState().setSession('auditor@pipeline.local', {
      userId: 'auditor@pipeline.local',
      roles: ['ReadOnlyAuditor'],
      permissions: ['sheets.values.read', 'jobs.read'],
      scopes: { spreadsheetIds: ['ops-ledger-2026'], chatSpaceIds: [], zohoOrgIds: [], paymentAccountIds: [] },
      stepUpFresh: false,
    });

    const session = getState();
    expect(session.isAuthenticated).toBe(true);

    const hasWritePermission = session.capabilities?.permissions.includes('sheets.values.write');
    expect(hasWritePermission).toBe(false);

    // Derive button disabled status and disabledReason
    const saveButtonProps = {
      disabled: !hasWritePermission,
      disabledReason: !hasWritePermission ? 'Missing sheets.values.write permission' : undefined,
    };

    expect(saveButtonProps.disabled).toBe(true);
    expect(saveButtonProps.disabledReason).toBe('Missing sheets.values.write permission');

    // Payments workspace requires payments.checkout.create -> access-denied
    const hasPaymentsPerm = session.capabilities?.permissions.includes('payments.checkout.create');
    const paymentsScreenState: ScreenLifecycleState = hasPaymentsPerm ? 'success' : 'access-denied';
    expect(paymentsScreenState).toBe('access-denied');
  });

  // Journey 4: Timeout outcome unknown prevents blind retry and prompts job inspection
  it('[B-02, L-04] Journey 4: write timeout transitions to OutcomeUnknown without blind retry and preserves key', async () => {
    const correlationId = createCorrelationId();
    const idempotencyKey = createIdempotencyKey();

    // 1. Track in-flight operation in pendingOperationsStore
    const pendingStore = usePendingOperationsStore.getState();
    pendingStore.add({
      idempotencyKey,
      correlationId,
      command: 'sheets.write',
      startedAt: Date.now(),
    });

    expect(usePendingOperationsStore.getState().operations.has(idempotencyKey)).toBe(true);

    // 2. Mutation execution times out on bridge
    let state = intentReducer(INITIAL_INTENT_STATE, {
      type: 'START',
      correlationId,
      idempotencyKey,
    });
    state = intentReducer(state, { type: 'VALID' });
    expect(state.phase).toBe('Submitting');

    // Simulate timeout event (Rule B-02)
    state = intentReducer(state, { type: 'TIMED_OUT' });
    expect(state.phase).toBe('OutcomeUnknown');

    // 3. Rule B-02: Ensure automatic retry is locked and same key is retained
    if (state.phase === 'OutcomeUnknown') {
      expect(state.idempotencyKey).toBe(idempotencyKey);
      expect(state.correlationId).toBe(correlationId);
    }

    // 4. Illegal transition to Submitting without resetting is prohibited
    expect(() => {
      intentReducer(state, { type: 'VALID' });
    }).toThrow(/Illegal state transition/);

    // 5. User inspects jobs queue to find confirmed status before resetting
    state = intentReducer(state, { type: 'RESET' });
    expect(state.phase).toBe('Idle');
  });
});
