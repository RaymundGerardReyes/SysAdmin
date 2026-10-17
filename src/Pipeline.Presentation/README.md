# Pipeline.Presentation

> Frontend architecture for PipelineSolution using React Native (Windows + iOS) + Expo Modules + NativeWind v4 + TypeScript.

## Engineering Laws & Invariants
- **[L-01]** Concentric layer dependency direction: imports flow inward and downward only.
- **[L-02]** One concept, one home: contracts mirror backend snapshots.
- **[L-03]** Pure core (`src/core/`), impure edges (`src/bridge/`).
- **[L-04]** Discriminated unions for async and intent states (`src/core/intent/intentMachine.ts`).
- **[L-07]** Single logging door: only `security/secureLogger.ts` may call `console.*`.
- **[R-01, B-01]** Single bridge door: only `features/*/api/*.ts` call `bridge/`.
- **[B-02]** No blind retries on write timeout (`OutcomeUnknown` phase).

## Verification
```bash
npm run verify
```
Runs:
1. `check:structure` (folder grammar, manifests, per-folder READMEs)
2. `check:contracts` (drift check against `contracts-snapshots/`)
3. `check:trace` (traceability of R-xx, B-xx, N-xx, L-xx rules)
4. `typecheck` (TypeScript strict mode)
5. `test` (Jest unit, bridge, intent, and boundary tests)
