# PipelineSolution Workspace Architecture & Security Rules

Always enforce these architectural invariants when creating, modifying, or reviewing code in `E:\AdminInfrastructure`.

## 1. Toolchain & Solution Standards
- **Backend:** .NET 10 LTS (`net10.0`), `PipelineSolution.slnx`, Central Package Management (`Directory.Packages.props`), `TreatWarningsAsErrors=true`.
- **Frontend (`src/Pipeline.Presentation`):** Node.js 22 LTS, TypeScript (`strict: true`, `noUncheckedIndexedAccess: true`, `exactOptionalPropertyTypes: true`), React Native + `react-native-windows` (strictly matching minor versions) + Expo Modules + NativeWind v4.

## 2. Backend Anti-Bypass & RBAC Invariants (P1–P9, C1–C10)
- **Single Choke Point (P3, C1):** UI, IPC/NativeBridge, and `Pipeline.LocalApi` must ONLY invoke business logic through `IUseCaseDispatcher`. Never reference provider connectors outside their module.
- **Fixed Pipeline Order:** `UseCaseDispatcher` always runs behaviors in this exact order: `Authentication -> Validation -> Authorization -> RateLimit -> Idempotency -> Audit -> Handler`.
- **Mandatory Permissions (P1, P2, C4):** Every use-case handler must declare `[RequiresPermission("<provider>.<resource>.<action>")]` with a permission from `PermissionCatalog`. Never check `role == "Admin"` in business logic.
- **Capability Tickets (P7, C2, C3):** `ICredentialStore` and `DpapiCredentialStore` are `internal`. Handlers may only obtain provider tokens via `ICredentialBroker.GetAccessTokenAsync(AuthorizationTicket, ...)`, where `AuthorizationTicket` has an `internal` constructor and is issued solely by `AuthorizationService` (steps `S01`–`S11`).
- **Egress & Redaction (C6):** All outbound `HttpClient` instances must attach `EgressAllowlistHandler` (HTTPS only, pinned provider hosts, caller `Authorization` header stripped). Never log or return raw tokens, secrets, or stack traces (`RedactionPolicy`).

## 3. Frontend Engineering Laws (L-01–L-12, R-01–R-12, B-01–B-07)
- **Concentric Layer Direction (L-01):** Imports must point inward/downward only:
  `Layer 7 app` -> `Layer 6 features/*` -> `Layer 5 intent, platform` -> `Layer 4 security, observability` -> `Layer 3 ui, state` -> `Layer 2 bridge` -> `Layer 1 core, config, utils, i18n, hooks` -> `Layer 0 contracts`.
- **No Cross-Feature Imports (R-04):** `features/<a>` must never import from `features/<b>`.
- **One Bridge Door (R-01, B-01):** Only `features/*/api/*.ts` (and `app/` bootstrap) may import `bridge/`. Only `bridge/native/NativePipelineModule.ts` may reference native modules.
- **Pure Core & Intent Pipeline (L-03, L-04, B-02):** All state-changing actions (`T1`–`T3`) must run through `useMutationIntent` and the pure `core/intent/intentMachine.ts` reducer. Never auto-retry a write after a timeout (`OutcomeUnknown` phase instead).
- **Single Source of Truth & Drift Protection (L-02):** `contracts/` mirrors `contracts-snapshots/*.json` exported from the backend. Never hand-duplicate command, permission, or error lists.
- **One Logging Door (L-07):** Only `security/secureLogger.ts` may call `console.*`. All other code uses `observability/track.ts` with closed event names (`observability/events.ts`) and automatic redaction.

## 4. Testing & Traceability Discipline
- **Backend Tests:** Every test class must carry `[Trait("Category", "...")]` (`Unit`, `Path`, `Architecture`, `Api`, `Integration`, `Contract`, `Security`, `Regression`, `Sandbox`). Maintain 100% coverage of `AUTHZ_P01`..`AUTHZ_P11`, `AUTHZ_PX`, and `IDEM_P01`..`IDEM_P05`.
- **Frontend Tests:** Core, bridge, security, intent, and observability files must include a file header citing their rule IDs (`Rules: R-xx, B-xx, L-xx`), and test names must prefix `[RuleID]` so `npm run check:trace` passes.
