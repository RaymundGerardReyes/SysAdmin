# Canonical Architecture & Engineering Rules

## 1. Engineering Laws (L-01 to L-12)
- **L-01:** Dependencies point inward and downward only (Layer 7 -> Layer 0).
- **L-02:** One concept, one home (no duplicated definitions; single source of truth).
- **L-03:** Pure core, impure edges (decision logic in core/ has no React and no native code).
- **L-04:** Make illegal states unrepresentable (discriminated unions for async/intent state).
- **L-05:** Fail loudly in dev, safely in prod.
- **L-06:** Every user action has an identity (correlation ID on all actions; idempotency key on writes).
- **L-07:** Logs are a product (structured tracking, no raw console.* calls except secureLogger).
- **L-08:** Decisions are written down (ADRs for architectural changes).
- **L-09:** Small, named, boring files.
- **L-10:** Delete over deprecate (no dead code or unused exports).
- **L-11:** Cosmetic UI security, authoritative backend.
- **L-12:** A bug fix ships with a regression test.

## 2. Core Frontend Rules (R-01 to R-12)
- **R-01:** One bridge door to C# (only features/*/api and app bootstrap call bridge).
- **R-02:** Feature slice isolation.
- **R-03:** Capability gating is cosmetic UX.
- **R-04:** No cross-feature imports.
- **R-06:** Five states for every screen (loading, success, error, needs-reauth, access-denied).
- **R-07:** Double-submit guard while Submitting.
- **R-08:** Platform divergence isolated to platform/.

## 3. Bridge Rules (B-01 to B-07)
- **B-01:** Structured command envelope { id, command, payload, correlationId, idempotencyKey }.
- **B-02:** Write timeout transitions to OutcomeUnknown (never auto-retry writes).
- **B-03:** Single transport interface.
- **B-05:** Cancellation on unmount for reads.
- **B-07:** Drop token-like fields from bridge responses.

## 4. Styling & Native Rules (N-01 to N-10)
- **N-06:** No inline hex colors or magic numbers.
- **N-07:** No dynamic class string construction in Tailwind.
- **N-10:** Design tokens in tailwind.config.js only.
