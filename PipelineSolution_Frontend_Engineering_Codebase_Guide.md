# PipelineSolution: Frontend Engineering & Codebase Infrastructure Guide

> **Audience:** every developer who will build, review, debug, or inherit `Pipeline.Presentation/` (React Native: Windows + iOS, TypeScript, NativeWind).
> **Mindset:** Principal Software Engineer. The code must be **scalable, maintainable, debuggable, trackable, organizable, and understandable**, and each of those words must map to a concrete mechanism in the repo, not a wish.
> **Builds on:** `PipelineSolution_Frontend_ReactNative_Scaffold.md` (the *what*) and `PipelineSolution_RBAC_Security_Testing_Blueprint.md` (the *backend guarantees*). This guide adds the *how we keep it healthy*: the logic pipeline, the observability layer, the documentation infrastructure, the automation, and the working agreements.
> **Contains:** structure, rules, templates, checklists. Code appears only as tiny illustrations.

---

## Table of Contents

1. [How the six qualities become mechanisms](#1-how-the-six-qualities-become-mechanisms)
2. [Engineering laws (the non-negotiables)](#2-engineering-laws)
3. [Repository infrastructure (the whole repo, not just `src/`)](#3-repository-infrastructure)
4. [Complete frontend codebase structure](#4-complete-frontend-codebase-structure)
5. [Layer map and dependency direction](#5-layer-map-and-dependency-direction)
6. [The frontend logic pipeline (one user action, end to end)](#6-the-frontend-logic-pipeline)
7. [Feature module standard (the unit of scale)](#7-feature-module-standard)
8. [Observability: debuggable and trackable by design](#8-observability-debuggable-and-trackable-by-design)
9. [Documentation infrastructure (markdown codebase)](#9-documentation-infrastructure)
10. [Conventions: naming, headers, size limits, commits](#10-conventions)
11. [Automation: scripts, generators, and CI gates](#11-automation-scripts-generators-and-ci-gates)
12. [Testing architecture and traceability](#12-testing-architecture-and-traceability)
13. [Operating playbooks (add a feature, command, permission, error)](#13-operating-playbooks)
14. [Debugging runbook by symptom](#14-debugging-runbook-by-symptom)
15. [Roadmap: milestones with exit criteria](#15-roadmap-milestones-with-exit-criteria)
16. [Amendments to the existing scaffold](#16-amendments-to-the-existing-scaffold)
17. [Anti-patterns and review checklist](#17-anti-patterns-and-review-checklist)
18. [Templates (copy-paste markdown)](#18-templates)

---

## 1. How the six qualities become mechanisms

A quality that has no enforcing mechanism decays within three months. Each row below names what enforces it.

| Quality | What it means here | Mechanisms (where to look) | Enforced by |
|---|---|---|---|
| **Scalable** | A new feature, command, or platform costs *additive* work, never edits to unrelated code | Feature-sliced modules (§7); `feature.manifest.ts` registry; closed command registry; platform isolation in `src/platform/`; generator script | Boundary lint, `tools/generate-feature`, architecture tests |
| **Maintainable** | Changing one thing touches one place | One door to backend (bridge); tokens only in `tailwind.config.js`; one error type; one logger; contracts as single source of truth | ESLint rules, contract drift tests, file-size limits |
| **Debuggable** | Any bug report can be traced to a layer in minutes | Correlation ID on every action; typed `AppError`; intent state machine with named phases; breadcrumb ring buffer; diagnostics screen; support bundle | Observability event catalog test, runbook (§14) |
| **Trackable** | You can answer "why does this code exist, who owns it, what covers it, when did it change" | Rule IDs (`R-xx`, `B-xx`, `N-xx`) cited in code headers and test names; ADRs; CODEOWNERS; Conventional Commits; generated traceability matrix | CI traceability check, PR template |
| **Organizable** | A developer can predict where a file lives before searching | Fixed folder grammar; fixed filename grammar; per-folder `README.md`; barrel rules | Structure test (`tools/check-structure`), lint |
| **Understandable** | A new developer ships a safe change on day 3 | `docs/` onboarding path; architecture diagrams; glossary; per-feature README; "why" comments only; small files | Onboarding checklist, docs-link checker, PR review |

---

## 2. Engineering laws

These extend the scaffold's R-01 to R-12. They are written as laws because reviewers will quote them.

| ID | Law | Consequence if violated |
|---|---|---|
| L-01 | **Dependencies point inward and downward only.** Direction is defined once in §5 and enforced by tooling, not by memory. | Merge blocked by boundary lint |
| L-02 | **One concept, one home.** Permissions, error codes, job statuses, commands, and tokens each have exactly one definition file. Everything else imports it. | Drift tests fail |
| L-03 | **Pure core, impure edges.** Decision logic (intent state machine, route guards, formatters, reducers) is pure `.ts` with no React and no native calls, so it is trivially testable. Side effects live at the edges (`bridge/`, hooks, providers). | Review rejection |
| L-04 | **Make illegal states unrepresentable.** Use discriminated unions for async/intent state, never `isLoading && isError && data` boolean soup. Exhaustive `switch` with `assertNever`. | Compile error |
| L-05 | **Fail loudly in dev, safely in prod.** Contract mismatch throws in dev/test; becomes `BRIDGE_CONTRACT_MISMATCH` with a correlation ID in release. | Test + manual check |
| L-06 | **Every user action has an identity.** One action = one correlation ID = (for writes) one idempotency key. Both appear in every log line and every error screen. | Event catalog test |
| L-07 | **Logs are a product.** Events have stable names, structured fields, and redaction. Free-text `console.log` debugging is forbidden in committed code. | Lint (`no-console`) |
| L-08 | **Decisions are written down.** Any change to a boundary, a library, a contract shape, or a security behavior needs an ADR (§9.3) before merge. | PR template gate |
| L-09 | **Small, named, boring.** Files do one thing and say so in their name. Prefer 10 small files over 1 clever file. | Size limits (§10.4) |
| L-10 | **Delete over deprecate.** No dead code, commented-out code, or unused exports. Tooling removes them. | `knip`/unused-export check in CI |
| L-11 | **Cosmetic UI security, authoritative backend.** (Restated from R-03.) UI gating improves UX only. Every mutation still handles denial. | Security tests |
| L-12 | **A bug fix ships with a regression test** that carries the bug ID in its name. | PR template gate |

---

## 3. Repository infrastructure

The scaffold defined `Pipeline.Presentation/`. A healthy codebase also needs the *surroundings*. This is the recommended repo layout for the frontend project (adapt to wherever it sits inside the solution).

```
Pipeline.Presentation/
│
├── README.md                          ← 5-minute orientation + links to docs/
├── CONTRIBUTING.md                    ← working agreements, PR process, commit format
├── CODEOWNERS                         ← ownership per folder/feature (also at .github/)
├── CHANGELOG.md                       ← human-readable, per release
├── SECURITY.md                        ← how to report issues; secret-handling rules
├── LICENSE / NOTICE                   (as applicable)
│
├── .github/
│   ├── PULL_REQUEST_TEMPLATE.md       ← checklist from §17
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md              ← requires correlation ID + support bundle
│   │   ├── feature_request.md         ← requires permission/tier/command answers
│   │   └── tech_debt.md
│   └── workflows/
│       ├── ci.yml                     ← lint, typecheck, tests, drift, audit (§11.3)
│       ├── nightly.yml                ← mutation/e2e/dep-update checks (as adopted)
│       └── release-windows.yml / release-ios.yml
│
├── docs/                              ← THE markdown codebase (see §9)
│   ├── README.md                      ← docs index (map of maps)
│   ├── 00-start-here/
│   ├── 01-architecture/
│   ├── 02-conventions/
│   ├── 03-how-to/
│   ├── 04-operations/
│   ├── 05-decisions/                  ← ADRs
│   ├── 06-reference/                  ← generated + hand-maintained catalogs
│   └── diagrams/
│
├── tools/                             ← repo automation (plain Node/TS scripts; see §11)
│   ├── generate-feature/
│   ├── generate-command/
│   ├── check-structure.ts
│   ├── check-contract-drift.ts
│   ├── check-i18n-keys.ts
│   ├── check-traceability.ts
│   ├── check-docs-links.ts
│   └── build-docs-reference.ts
│
├── .config/                           ← tool configs that don't need to be in root
│   ├── dependency-cruiser.cjs         ← boundary rules (§5) as machine-checked config
│   ├── eslint/
│   │   ├── boundaries.js
│   │   ├── no-console.js
│   │   └── no-native-module-outside-bridge.js
│   └── knip.json                      ← dead-code/unused-export detection
│
├── contracts-snapshots/               ← pinned JSON snapshots exported from backend CI
│   ├── permissions.snapshot.json
│   ├── errorCodes.snapshot.json
│   ├── jobStatus.snapshot.json
│   └── commands.snapshot.json
│
├── (root configs from scaffold §6: index.js, App.tsx, tsconfig.json, …)
├── windows/                           ← RN Windows host + Pipeline.NativeBridge (C#)
├── ios/                               ← iOS host + PipelineNativeBridge
├── src/                               ← §4
└── __tests__/                         ← §12
```

**Why `contracts-snapshots/` matters:** the backend CI exports permission/error/status/command lists as JSON. The frontend commits the latest snapshot and `check-contract-drift` compares it to `src/contracts/`. A backend change that isn't mirrored here breaks the frontend build *immediately* instead of in production.

---

## 4. Complete frontend codebase structure

Legend: `NEW` = added by this guide on top of the scaffold; `(opt)` optional; unchanged scaffold entries keep their original responsibilities (scaffold §8). Every folder contains a short `README.md` (3 to 10 lines: purpose, allowed imports, link to the doc).

```
src/
├── app/                                   (composition root; imports anything below)
│   ├── README.md
│   ├── AppProviders.tsx
│   ├── AppBootstrap.tsx
│   ├── AppErrorBoundary.tsx
│   ├── featureRegistry.ts                 NEW  collects every feature.manifest (one line per feature)
│   └── navigation/
│       ├── RootNavigator.tsx
│       ├── AuthNavigator.tsx
│       ├── MainNavigator.tsx
│       ├── navigation.types.ts
│       ├── routes.ts                      (derived from manifests; verified against scaffold §11.2)
│       ├── linking.ts                     (opt)
│       └── navigationGuards.ts
│
├── core/                                  NEW  pure, framework-free building blocks (leaf layer)
│   ├── README.md
│   ├── ids/
│   │   ├── requestId.ts
│   │   ├── correlationId.ts               (moved from bridge/correlation.ts)
│   │   └── idempotencyKey.ts              (moved from bridge/idempotency.ts)
│   ├── result.ts                          Result<T, AppError> helpers (ok/err/match)
│   ├── clock.ts                           injectable time source (deterministic tests)
│   └── intent/
│       ├── intent.types.ts                IntentPhase, IntentState, IntentEvent unions
│       ├── intentMachine.ts               PURE reducer: (state, event) → state
│       └── intentPolicy.ts                per-tier rules (confirm? step-up? lock after submit?)
│
├── contracts/                             (leaf layer; unchanged from scaffold)
│   ├── index.ts
│   ├── permissions.ts   riskTiers.ts   capabilities.ts   jobStatus.ts
│   ├── commands.ts                        NEW  command names + metadata (permission, tier, mutating)
│   ├── requests/   responses/   errors/
│   └── README.md
│
├── bridge/                                (the ONE door; unchanged except moves noted)
│   ├── README.md
│   ├── PipelineBridge.ts   createPipelineBridge.ts   bridge.types.ts
│   ├── commandRegistry.ts                 derives from contracts/commands.ts (no second list)
│   ├── envelope.ts   responseValidator.ts   errorMapper.ts   timeouts.ts
│   ├── native/NativePipelineModule.ts
│   └── transports/
│       ├── Transport.ts   NativeModuleTransport.ts
│       ├── LocalHttpTransport.ts          (opt, Windows only)
│       └── MockTransport.ts
│
├── state/                                 (server cache + small stores)
│   ├── README.md
│   ├── queryClient.ts   queryKeys.ts
│   └── stores/
│       ├── sessionStore.ts   uiStore.ts   pendingOperationsStore.ts
│       └── resetAllStores.ts              NEW  single sign-out reset entry point
│
├── security/                              (UI-side capability handling; cosmetic gating)
│   ├── README.md
│   ├── CapabilityProvider.tsx   PermissionGate.tsx   ProtectedScreen.tsx
│   ├── StepUpModal.tsx   ApprovalPendingBanner.tsx
│   ├── useCapabilities.ts   usePermission.ts   useStepUp.ts
│   ├── redact.ts
│   └── secureLogger.ts                    (the ONLY file allowed to call console.*)
│
├── observability/                         NEW  debuggable + trackable infrastructure
│   ├── README.md
│   ├── events.ts                          closed catalog of event names (typed)
│   ├── track.ts                           track(event, fields): adds correlation, redacts, logs
│   ├── breadcrumbs.ts                     in-memory ring buffer (last N redacted events)
│   ├── supportBundle.ts                   builds redacted bundle: version, platform, breadcrumbs, last errors
│   └── perfMarks.ts                       (opt) startup + screen-ready timing marks
│
├── intent/                                NEW  the frontend "logic pipeline" runtime (React-aware)
│   ├── README.md
│   ├── useIntent.ts                       drives intentMachine + pendingOperationsStore + step-up
│   ├── useMutationIntent.ts               wraps a feature mutation in useIntent (default for all writes)
│   ├── mapAppErrorToUi.ts                 AppError → UI directive (scaffold §12 as one pure table)
│   └── IntentBoundary.tsx                 (opt) renders confirm / step-up / pending / outcome-unknown UI
│
├── features/                              (feature-sliced; each is a mini-app)
│   ├── README.md                          the feature contract + how to add one (§7, §13)
│   ├── _template/                         NEW  the golden copy used by tools/generate-feature
│   ├── session/  dashboard/  jobs/  integrations/  sheets/  gmail/  chat/
│   ├── zohoExpense/  bigin/  payments/  approvals/  audit/  admin/  settings/
│   ├── diagnostics/                       NEW  dev/support only: breadcrumbs, support bundle (flag-gated)
│   └── <each feature>/
│       ├── README.md                      NEW  purpose, commands used, permissions, tier, owner
│       ├── feature.manifest.ts            NEW  id, routes, required permission, nav label key, tier
│       ├── screens/   components/   hooks/   api/
│       ├── __fixtures__/                  NEW  MockTransport scripts + sample responses for this feature
│       └── index.ts                       public surface only
│
├── ui/                                    (design system; knows nothing about the app)
│   ├── README.md
│   ├── theme/ (tokens.ts, useAppTheme.ts, platformTheme.ts)
│   ├── primitives/  layout/  feedback/  data/
│   ├── forms/ (Form.tsx, FormField.tsx, schemas/)
│   └── gallery/                           NEW  (opt, dev only) screen rendering every primitive/state
│
├── platform/                              (the ONLY place for Windows/iOS divergence)
│   ├── README.md
│   ├── PlatformShell.windows.tsx   PlatformShell.ios.tsx
│   ├── useKeyboardShortcuts.windows.ts   useKeyboardShortcuts.ios.ts
│   ├── usePlatformLayout.ts   windowSize.ts
│
├── hooks/        (generic only: useAppActive, useDebouncedValue, useIsOnline, useDoubleSubmitGuard)
├── utils/        (pure: formatDate, formatCurrency, truncate, assertNever, typeGuards)
├── config/       (appConfig.ts, featureFlags.ts, constants.ts, buildInfo.ts NEW: version, commit, build mode)
├── i18n/         (index.ts, locales/en.ts, locales/fil.ts opt; errors.* keys required for every error code)
├── assets/       (fonts/, icons/, images/)
└── types/        (global.d.ts, env.d.ts)
```

### 4.1 What changed vs. the scaffold, in one glance

| Addition | Why it exists |
|---|---|
| `core/` | Puts identity, `Result`, clock, and the **intent state machine** in a pure leaf layer that both `bridge/` and `intent/` can use without breaking boundaries |
| `contracts/commands.ts` | The command list was duplicated (scaffold §8.2 `commandRegistry` + §10.1 table). One definition (L-02); registry and docs derive from it |
| `observability/` | Debuggable + trackable need a first-class home, not scattered `console` calls |
| `intent/` | Writes were described as scattered rules (idempotency, double-submit, step-up, approval, outcome-unknown). One runtime makes them uniform for every feature |
| `feature.manifest.ts` + `app/featureRegistry.ts` | Adding a feature = new folder + one registry line; navigation, permission map, and docs derive from manifests |
| `features/_template`, `tools/` | Scalability by generation: consistent shape without relying on memory |
| `docs/`, `contracts-snapshots/`, `.config/` | Understandable and trackable infrastructure around the code |

---

## 5. Layer map and dependency direction

Think of the code as concentric rings. **Arrows only go from outer (top) to inner (bottom).** Anything that violates this is a bug in the design, not a "quick fix."

```
 Layer 7  app/                       composition root
 Layer 6  features/*                 vertical slices (never import each other)
 Layer 5  intent/   platform/        action runtime; platform shells
 Layer 4  security/ observability/   capability UI; logging/tracking
 Layer 3  ui/       state/           design system; caches + stores
 Layer 2  bridge/                    the one door to C#
 Layer 1  core/     config/ utils/ i18n/ hooks/(generic)
 Layer 0  contracts/                 leaf; imports nothing
```

### 5.1 Allowed-imports table (replaces scaffold §7 as the machine-readable rule)

| Layer | May import | Must NOT import |
|---|---|---|
| `contracts/` | nothing | everything |
| `core/`, `utils/`, `config/` | `contracts`, each other (no cycles) | all layers above |
| `i18n/`, generic `hooks/` | `core`, `utils`, `config` | `features`, `bridge`, `ui` |
| `bridge/` | `contracts`, `core`, `utils`, `config` | `features`, `ui`, `security`, `state`, `intent` |
| `ui/` | `utils`, `config`, `i18n`, `core` | `features`, `bridge`, `state`, `security`, `intent` |
| `state/` | `contracts`, `core`, `utils` | `features`, `ui`, `bridge` internals |
| `security/` | `ui`, `contracts`, `state`, `core`, `utils`, `observability` | `features`, `bridge/native` |
| `observability/` | `contracts`, `core`, `utils`, `config`, `security/redact`, `security/secureLogger` | `features`, `ui`, `bridge` |
| `intent/` | `core`, `contracts`, `state`, `security`, `observability`, `hooks`, `ui/feedback` (types only if needed), `i18n` | `features`, `bridge` |
| `platform/` | `ui`, `utils`, `config`, `core` | `features`, `bridge` |
| `features/<x>/` | `ui`, `security`, `state`, `intent`, `observability`, `contracts`, `core`, `hooks`, `utils`, `config`, `i18n`, `platform`; **`bridge` only inside `api/*.ts`** | other `features/*`, `bridge/native`, native modules |
| `app/` | everything | n/a |

### 5.2 Hard rules (tooling-enforced)

1. **Bridge access:** only `features/*/api/*.ts` import `bridge/`. (Also `app/` for bridge creation and bootstrap.)
2. **Native module:** only `bridge/native/NativePipelineModule.ts` references it.
3. **No cycles**, checked by dependency-cruiser and an architecture test.
4. **No cross-feature imports.** If two features need the same thing, it belongs in `ui/`, `security/`, `intent/`, or `core/`.
5. **Barrel discipline:** `index.ts` only at feature roots and `contracts/`. No barrels inside `ui/` subfolders.
6. **Unused exports** fail CI (knip).

### 5.3 Where does new code go? (decision tree)

```
Is it a type/shape shared with C#?                      → contracts/
Is it pure logic with no React, no native, reusable?    → core/ or utils/
Does it call the backend?                               → features/<x>/api/ (through bridge)
Is it a visual building block with no business meaning? → ui/
Does it decide who sees/does what (cosmetic)?           → security/
Does it coordinate a write (confirm/step-up/idempotency)? → intent/
Is it logging/tracking/diagnostics?                     → observability/
Does it differ between Windows and iOS?                 → platform/
Is it specific to one business area?                    → features/<x>/
```

---

## 6. The frontend logic pipeline

This is the heart of the guide. **Every state-changing action in every feature passes through the same pipeline.** Reads use a lighter version. Uniform flow = predictable debugging.

### 6.1 Write pipeline (T1 to T3)

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant S as Screen/Form (features/x)
    participant I as intent/useMutationIntent
    participant M as core/intentMachine (pure)
    participant A as features/x/api
    participant B as bridge (envelope, validator, mapper)
    participant C as C# host (IUseCaseDispatcher)

    U->>S: Submit
    S->>I: start(intent payload)
    I->>M: event START  (assign correlationId + idempotencyKey)
    M-->>I: phase = Validating
    I->>I: schema validation (UX only)
    I->>M: VALID
    M-->>I: phase = AwaitingConfirm (T2/T3) or Submitting (T0/T1)
    U->>I: Confirm
    I->>M: CONFIRMED  (double-submit lock ON)
    I->>A: execute(payload, ids)
    A->>B: bridge.command(envelope)
    B->>C: send (NativeModuleTransport)
    C-->>B: result | denial | challenge | pending approval
    B->>B: validate response shape, map to AppError
    B-->>A: Result<T, AppError>
    A-->>I: Result
    alt success
        I->>M: SUCCEEDED → phase = Succeeded
    else STEPUP_REQUIRED
        I->>M: CHALLENGED → phase = AwaitingStepUp (same idempotency key retained)
        I-->>S: open StepUpModal → on success RESUME same key
    else APPROVAL_REQUIRED
        I->>M: PENDING_APPROVAL → phase = PendingApproval (lock resubmission)
    else timeout on write
        I->>M: TIMED_OUT → phase = OutcomeUnknown (NO auto resend; link to job status)
    else other error
        I->>M: FAILED(appError) → phase = Failed (mapAppErrorToUi decides presentation)
    end
```

### 6.2 Intent phases (the vocabulary everyone shares)

| Phase | Meaning | UI must show | Allowed next |
|---|---|---|---|
| `Idle` | No active intent | Form editable | `Validating` |
| `Validating` | UX-level validation running | Inline field errors | `AwaitingConfirm`, `Submitting`, `Idle` |
| `AwaitingConfirm` | T2/T3 summary shown | `ConfirmDialog` / locked summary | `Submitting`, `Idle` (cancel) |
| `Submitting` | Request in flight | Loading, controls locked | `Succeeded`, `AwaitingStepUp`, `PendingApproval`, `OutcomeUnknown`, `Failed` |
| `AwaitingStepUp` | Backend demanded re-auth | `StepUpModal` | `Submitting` (same key), `Idle` (cancel) |
| `PendingApproval` | T3 awaiting a second person | `ApprovalPendingBanner`, link to record | terminal for this intent |
| `OutcomeUnknown` | Write timed out or transport broke mid-flight | "Outcome unknown, check job status" | `Idle` after user checks status; **never auto-resend** |
| `Succeeded` | Done | Success feedback | `Idle` |
| `Failed` | Typed error | Directive from `mapAppErrorToUi` | `Idle` (edit and retry creates **new** intent only if content changed or error is non-retryable) |

> **Rule:** `intentMachine.ts` is a pure reducer with exhaustive transition tests. Illegal transitions throw in dev/test (L-04, L-05). Features never invent their own flags like `isSending`.

### 6.3 Per-tier policy (`core/intent/intentPolicy.ts`)

| Tier | Confirm step | Locked summary | Step-up expected | Approval possible | Optimistic update |
|---|---|---|---|---|---|
| T0 | no | no | no | no | allowed (reads) |
| T1 | no (clear result feedback) | no | no | no | **no** |
| T2 | yes (effect summary) | no | if challenged | no | **no** |
| T3 | yes | yes | yes | yes | **no** |

Policy is *data*, so changing it is a one-file diff with a test, not a hunt through screens.

### 6.4 Read pipeline (T0)

`Screen → feature hook (TanStack Query) → features/x/api → bridge → C#`. Same envelope, same validator, same error mapper. Differences: cancellable on unmount (B-05), safe to retry on transport error, bounded polling that stops at terminal states or when the app is inactive.

### 6.5 Error-to-UI is one table

`intent/mapAppErrorToUi.ts` encodes scaffold §12 as a single pure function: `AppError → UiDirective` (e.g., `{ kind: "OPEN_STEP_UP" }`, `{ kind: "SHOW_ACCESS_DENIED", refreshCapabilities: true }`, `{ kind: "FIELD_ERRORS", keys }`). Screens render directives; they do not interpret codes. A test iterates **every** code in `errorCodes.ts` and fails if one has no directive or i18n key.

### 6.6 Traceability of one action across the stack

```
UI click
  └─ correlationId = c_8f2…           (core/ids)
      ├─ breadcrumb: intent.started          (observability/track)
      ├─ envelope.correlationId = c_8f2…     (bridge/envelope)
      ├─ C# logs & audit entry carry c_8f2…  (backend contract)
      ├─ breadcrumb: bridge.responded(code=STEPUP_REQUIRED)
      └─ ErrorState shows "Ref: c_8f2…"      (ui/feedback)
```

A user reads out the reference; a developer greps the same ID in frontend breadcrumbs, host logs, and the audit log. **That is "trackable."**

---

## 7. Feature module standard

A feature is a **mini-application with a public surface**. It is the unit of scale, ownership, and testing.

### 7.1 Required shape

```
features/<feature>/
├── README.md                 purpose · commands used · permissions · tier · owner · known limits
├── feature.manifest.ts       id, routes[], requiredPermission, navLabelKey, tier, owner
├── screens/                  route-level; wrapped in ProtectedScreen; five states (R-06)
├── components/               feature UI composed from ui/ primitives; no data fetching
├── hooks/                    queries/mutations; writes use useMutationIntent
├── api/<feature>Api.ts       ONLY bridge caller; one function per backend use case
├── __fixtures__/             MockTransport scripts: success, denial, challenge, approval, timeout
└── index.ts                  exports screens (+ manifest); nothing else leaks
```

### 7.2 `feature.manifest.ts` (what it declares; fields, not code)

| Field | Purpose |
|---|---|
| `id` | Stable identifier (`"gmail"`); used in logs, query keys, docs |
| `routes[]` | Route name, screen component, required permission, platform visibility |
| `navigation` | Label i18n key, icon name, order, section |
| `commands[]` | Commands this feature may call (checked against `api/` imports in a test) |
| `riskTier` | Highest tier the feature performs |
| `owner` | Team/person handle (mirrors CODEOWNERS; checked in CI) |

**Benefits:** `routes.ts` and the screen-to-permission table (scaffold §11.2) are *derived and verified*, not retyped. The docs reference (`docs/06-reference/features.md`) is generated from manifests. Navigation guards read manifests.

### 7.3 Feature completeness checklist (Definition of Done per feature)

- [ ] Manifest present; registered in `app/featureRegistry.ts`
- [ ] README filled (commands, permissions, tier, owner)
- [ ] `api/` has exactly one function per command; no other bridge access
- [ ] Every screen passes the five-state test (loading, empty, success, error, access-denied)
- [ ] Every write uses `useMutationIntent` (idempotency, double-submit guard, confirm per tier, outcome-unknown)
- [ ] Fixtures cover denial, step-up, approval (if T3), conflict, rate-limit, timeout
- [ ] i18n keys added; no hard-coded strings; error keys complete
- [ ] Tokens only; accessibility labels; keyboard path verified on Windows
- [ ] Coverage thresholds met; mutation paths have regression tests
- [ ] Observability events emitted for start/success/failure of each intent

### 7.4 Sizing guidance

| Signal | Action |
|---|---|
| A screen file exceeds ~200 lines | Extract components/hooks |
| A feature has more than ~12 components | Introduce `components/<subarea>/` subfolders |
| Two features need the same component | Promote it to `ui/` (generic) or `security/`/`intent/` (behavioral) |
| A feature needs another feature's data | It calls the **same backend command** through its own `api/`; it never imports the other feature |

---

## 8. Observability: debuggable and trackable by design

### 8.1 Principles

1. **Structured, not prose.** Events are `{ name, correlationId, feature, phase, code?, durationMs?, platform, appVersion }`.
2. **Closed vocabulary.** `observability/events.ts` lists every event name as a typed union; `track()` accepts only those.
3. **Redacted at the source.** `track()` always passes through `redact.ts`. Payload bodies are never logged (scaffold §8.4).
4. **One logging door.** Only `secureLogger.ts` calls `console.*`; everything else calls `track()` (or the logger indirectly).
5. **Bounded memory.** `breadcrumbs.ts` is a ring buffer (e.g., last 200 events), cleared on sign-out.
6. **Release-safe.** Verbose levels disabled in release; the diagnostics feature is flag-gated and absent from release unless explicitly enabled for support builds.

### 8.2 Event naming grammar

`<area>.<subject>.<phase>`, lowercase, dot-separated.

| Example | Emitted by |
|---|---|
| `app.bootstrap.started` / `app.bootstrap.ready` / `app.bootstrap.failed` | `AppBootstrap` |
| `session.signin.started` / `.succeeded` / `.failed` | session feature |
| `intent.started` / `intent.confirmed` / `intent.stepup.requested` / `intent.pending_approval` / `intent.outcome_unknown` / `intent.succeeded` / `intent.failed` | `intent/` |
| `bridge.request.sent` / `bridge.response.received` / `bridge.contract.mismatch` / `bridge.timeout` | `bridge/` (command name + duration only) |
| `capability.refreshed` / `capability.policy_version_changed` | `CapabilityProvider` |
| `nav.route.denied` | navigation guards |
| `ui.error_boundary.caught` | `AppErrorBoundary` |

### 8.3 Fields policy

| Always include | Never include |
|---|---|
| `correlationId`, `feature`, command name, phase, error `code`, duration, platform, app version/build | Tokens, secrets, request/response bodies, recipient lists, amounts with payee, full emails, free-text user content |

### 8.4 Diagnostics and support bundle

| Artifact | Purpose | Safety |
|---|---|---|
| `features/diagnostics` screen | Shows last breadcrumbs, last N `AppError`s (code + correlation ID), build info, policy version, bridge mode | Flag-gated; requires an admin capability in production support builds; redacted |
| `observability/supportBundle.ts` | Produces a copyable text/JSON bundle for bug reports | Redaction test enforced; no payloads |
| Error screens | Always show "Ref: `<correlationId>`" | Already required by scaffold §12 |
| `config/buildInfo.ts` | Version, commit hash, build mode, platform | Non-secret only |

### 8.5 What "debuggable" means as a measurable target

| Target | How we verify |
|---|---|
| Any user-reported error can be located to a **layer** (UI, intent, bridge, host, provider) from the correlation ID and error code alone | Runbook (§14) walk-through in onboarding |
| Any state-changing action's lifecycle is reconstructable from breadcrumbs | Test: scripted intent produces the exact expected event sequence |
| No `console.*` outside `secureLogger.ts` | Lint |
| No payload data in breadcrumbs or bundles | Redaction tests with seeded fake secrets |

---

## 9. Documentation infrastructure

The markdown files are part of the codebase: reviewed in PRs, linked-checked in CI, versioned with the code.

### 9.1 `docs/` tree

```
docs/
├── README.md                                  index: "if you want X, read Y"
│
├── 00-start-here/
│   ├── 01-what-is-this.md                     goals, non-goals, who talks to what (diagram)
│   ├── 02-day-one-setup.md                    Windows 11 toolchain, run on Windows, run with MockTransport
│   ├── 03-first-week-path.md                  reading order + 3 starter tasks of increasing difficulty
│   ├── 04-glossary.md                         correlation ID, intent, capability, tier, step-up, outcome-unknown…
│   └── 05-faq.md
│
├── 01-architecture/
│   ├── 01-system-context.md                   frontend ↔ native bridge ↔ C# dispatcher ↔ connectors
│   ├── 02-layers-and-boundaries.md            §5 of this guide, kept current
│   ├── 03-logic-pipeline.md                   §6: phases, tier policy, sequence diagrams
│   ├── 04-bridge-and-contracts.md             envelope, command catalog, drift protection
│   ├── 05-security-model-ui-side.md           cosmetic gating, what UI never does, mapping to RBAC blueprint
│   ├── 06-state-management.md                 server state vs UI state vs pending ops
│   ├── 07-styling-and-design-system.md        tokens, NativeWind verified-feature list (N-10)
│   ├── 08-platform-strategy.md                Windows vs iOS, what is isolated and why
│   └── 09-observability.md                    §8: events, breadcrumbs, support bundle
│
├── 02-conventions/
│   ├── naming.md   file-headers.md   folder-grammar.md
│   ├── git-and-commits.md                     Conventional Commits with feature scope
│   ├── code-review.md                         §17 checklist + reviewer expectations
│   ├── error-handling.md                      AppError, directives, i18n keys
│   └── accessibility.md
│
├── 03-how-to/
│   ├── add-a-feature.md                       §13.1
│   ├── add-a-backend-command.md               §13.2
│   ├── add-a-permission.md                    §13.3
│   ├── add-an-error-code.md                   §13.4
│   ├── add-a-ui-primitive.md
│   ├── add-a-platform-specific-file.md
│   ├── write-a-mock-transport-script.md
│   └── upgrade-a-dependency.md
│
├── 04-operations/
│   ├── debugging-runbook.md                   §14 (symptom → cause → where to look)
│   ├── release-windows.md   release-ios.md    packaging, signing handled outside repo
│   ├── incident-playbook.md                   contract drift in prod, policy invalid, audit chain broken
│   └── support-bundle-guide.md
│
├── 05-decisions/                              ADRs (immutable once accepted; supersede, don't edit)
│   ├── README.md                              index table: number · title · status · date
│   ├── 0001-feature-sliced-structure.md
│   ├── 0002-single-bridge-closed-commands.md
│   ├── 0003-intent-state-machine.md
│   ├── 0004-core-layer-and-id-generation.md
│   ├── 0005-nativewind-on-windows.md          (written after spike S1)
│   ├── 0006-ios-csharp-integration.md         (written after spike S3)
│   └── template.md
│
├── 06-reference/                              (some GENERATED by tools/build-docs-reference.ts)
│   ├── commands.md                            generated from contracts/commands.ts
│   ├── permissions-and-screens.md             generated from manifests
│   ├── error-codes.md                         generated: code → directive → i18n key → meaning
│   ├── events.md                              generated from observability/events.ts
│   ├── features.md                            generated from manifests + READMEs
│   ├── risk-tiers.md
│   └── verified-styling-features.md           output of spike S1; the allow-list for components
│
└── diagrams/
    ├── system-context.mmd   layers.mmd   intent-state-machine.mmd   write-sequence.mmd
```

### 9.2 Doc rules

| Rule | Detail |
|---|---|
| Docs live next to what they describe | Folder `README.md` for local rules; `docs/` for cross-cutting |
| Generated docs are never hand-edited | Header says `GENERATED: do not edit`; CI regenerates and fails on diff |
| Diagrams are text (Mermaid) | Reviewable in PRs |
| Every doc has an owner and "last verified" line | Stale docs get flagged by a quarterly review ticket |
| Links are checked | `tools/check-docs-links.ts` in CI |
| One fact, one place | Link, don't copy |

### 9.3 ADR discipline (trackable decisions)

An ADR is required when you change: a layer boundary, a state/form/query library, a bridge or envelope shape, a security behavior, the styling approach, or a platform strategy. Format in [§18.2](#182-adr-template). Statuses: `Proposed → Accepted → Superseded by ADR-xxxx`. ADRs are never rewritten; they are superseded, so history stays readable.

### 9.4 Folder `README.md` standard

Each `src/<folder>/README.md` has exactly: **Purpose · May import · Must not import · Key files · How to extend · Related docs**. Short enough to read in 30 seconds.

---

## 10. Conventions

### 10.1 Filename and symbol grammar

| Thing | Rule | Example |
|---|---|---|
| Component / screen | `PascalCase.tsx`, name = what it *is* | `SendConfirmDialog.tsx` |
| Hook | `useVerbNoun.ts`; returns a stable, typed shape | `useSendEmail.ts` |
| API wrapper | `<feature>Api.ts`; functions named after the backend use case | `gmailApi.sendMessage` |
| Pure logic | `camelCase.ts`, verb or noun, no React | `intentMachine.ts` |
| Types file | `<area>.types.ts` | `intent.types.ts` |
| Constants | `UPPER_SNAKE` for true constants; `camelCase` objects for catalogs | `ERROR_CODES` |
| Booleans | `is/has/can/should` prefix | `canCancel` |
| Event handlers | `handleX` inside components; `onX` as props | `onConfirm` |
| Tests | Mirror path; `.test.ts(x)` | `__tests__/bridge/errorMapper.test.ts` |
| Platform files | `Name.windows.tsx` / `Name.ios.tsx` | `PlatformShell.ios.tsx` |

### 10.2 File header (only where it adds value)

Core, bridge, security, intent, and observability files start with a 3-to-6 line header. Feature UI files do not need one.

```
/**
 * Purpose : <one line: what this file is>
 * Rules   : R-07, B-02            ← rule IDs this file implements (traceability)
 * Owner   : <layer/team>
 * Notes   : <the non-obvious WHY, if any>
 */
```

### 10.3 Comments policy

| Do | Don't |
|---|---|
| Explain **why** (a constraint, a bug it avoids, a rule ID) | Narrate **what** the code already says |
| Link to an ADR or doc for non-obvious decisions | Leave `TODO` without a ticket ID (`TODO(PIPE-123)`) |
| Mark workarounds with a removal condition | Commit commented-out code |

### 10.4 Size and complexity limits (lint-enforced; exceptions need a comment + reviewer approval)

| Metric | Limit |
|---|---|
| File length | 300 lines (screens target ≤ 200) |
| Function length | 50 lines |
| Cyclomatic complexity | ≤ 10 |
| Function parameters | ≤ 4 (otherwise an options object) |
| Component props | ≤ 10 (otherwise compose) |
| Nesting depth | ≤ 3 |

### 10.5 TypeScript rules (supplement to R-10)

- `strict`, `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes` recommended.
- No `any`, no non-null `!`, no `as` on bridge data (validate instead).
- Prefer `type` unions over `enum`; derive types from `as const` catalogs (single source for value + type).
- Discriminated unions for async and intent state; `assertNever` in every exhaustive switch.
- Branded types for IDs that must not be mixed (`CorrelationId`, `IdempotencyKey`, `JobId`).
- Public function signatures are explicit; return types declared for exported functions.

### 10.6 Git and commits (trackable history)

| Item | Rule |
|---|---|
| Branches | `feat/<ticket>-<slug>`, `fix/<ticket>-<slug>`, `chore/…`, `docs/…` |
| Commits | Conventional Commits with scope = feature or layer: `feat(gmail): add send confirm dialog`, `fix(bridge): drop token-like fields (B-07)` |
| PR size | Target < 400 changed lines; larger needs a split plan |
| PR content | Template checklist (§17); linked ticket; screenshots for UI (Windows + iOS); ADR link when applicable |
| Merge | Squash with a Conventional Commit title; `CHANGELOG.md` generated from commits |
| Ownership | `CODEOWNERS` gives each layer and feature an owner; security-sensitive folders (`bridge/`, `security/`, `contracts/`, `core/intent/`) require two approvals |

---

## 11. Automation: scripts, generators, and CI gates

### 11.1 `package.json` scripts (names are the contract; implementations may evolve)

| Script | Purpose |
|---|---|
| `start`, `windows`, `ios` | Run Metro / platform targets |
| `lint`, `typecheck`, `format`, `format:check` | Static quality |
| `test`, `test:ci`, `test:coverage` | Jest |
| `check:structure` | `tools/check-structure.ts`: folder grammar, README presence, manifests, barrels |
| `check:boundaries` | dependency-cruiser using `.config/dependency-cruiser.cjs` |
| `check:contracts` | Drift vs `contracts-snapshots/` |
| `check:i18n` | Every error code and referenced key exists |
| `check:trace` | Rule-ID traceability (§12.3) |
| `check:docs` | Link check + generated-docs-up-to-date |
| `check:deadcode` | knip |
| `docs:build` | Regenerates `docs/06-reference/*` |
| `gen:feature <name>` | Scaffolds a feature from `features/_template` |
| `gen:command <Name>` | Adds a command skeleton across contracts, api, fixtures, docs |
| `audit` | Dependency audit |
| `verify` | Runs everything above locally (same as CI) |

### 11.2 Generators (scalability without drift)

| Generator | Creates | Then prints |
|---|---|---|
| `gen:feature` | Folder from `_template` (README, manifest, screens/components/hooks/api, fixtures, test stubs), registry line, i18n key stubs | A checklist of the remaining manual steps (§13.1) |
| `gen:command` | Entry in `contracts/commands.ts`, request/response types stubs, `api/` function stub, fixture stub, docs row | Reminder to update the backend catalog and snapshot |

### 11.3 CI pipeline (stages and gates)

| Stage | Gate | Fails when |
|---|---|---|
| 1. Install | Locked install | Lockfile drift |
| 2. Static | `typecheck`, `lint`, `format:check` | Any error |
| 3. Architecture | `check:boundaries`, `check:structure`, `check:deadcode` | Boundary breach, cycle, missing README/manifest, unused export |
| 4. Contracts | `check:contracts`, `check:i18n` | Any mismatch with backend snapshots; missing error keys |
| 5. Tests | `test:ci` with coverage | Failures or coverage below thresholds (§12.2) |
| 6. Security | `audit`, secret scan (gitleaks or equivalent), forbidden-pattern scan (`console.`, `AsyncStorage` with sensitive keys, token-like literals) | Any finding |
| 7. Docs | `check:docs`, `check:trace` | Broken links, stale generated docs, orphan rule IDs |
| 8. Build | Windows Release build; iOS build when the cloud service is wired | Build failure; `MockTransport` present in release bundle |
| 9. Nightly (opt) | E2E smoke per platform; dependency update check; bundle-size trend | Regression beyond budget |

---

## 12. Testing architecture and traceability

### 12.1 Test pyramid for this codebase

| Level | Folder | Share | What it proves |
|---|---|---|---|
| Pure logic | `__tests__/unit/` (+ `core/`, `intent/`) | Largest | Intent machine transitions, policy table, formatters, schemas, query keys, route guards |
| Boundary | `__tests__/bridge/` | Large | Envelope, validator, error mapper, timeouts, no-retry-on-write, token-field dropping |
| Behavior | `__tests__/security/` | Large | Gate behavior, backend denial overrides UI, step-up resumes with same key, sign-out clears everything, redaction |
| Component | `__tests__/components/` | Medium | Primitives: disabled/loading, a11y props, keyboard |
| Screen | `__tests__/screens/` | Medium | Five states per screen; confirm for T2/T3; error→directive map |
| Architecture | `__tests__/architecture/` **NEW** | Small, critical | Boundaries, cycles, one-door rules, manifest↔registry consistency, i18n completeness |
| Contract | `__tests__/contracts/` | Small, critical | Snapshots equal `src/contracts/` |
| E2E (opt) | `__tests__/e2e/` | Smallest | Sign-in, dashboard, denied action, step-up, T3 pending approval |

### 12.2 Coverage thresholds (from scaffold §16, unchanged)

`bridge/`, `security/`, `contracts/`, plus **`core/` and `intent/`** → at least **90%** line/branch. Everything else → at least **75%**. Contract drift → 100% pass.

### 12.3 Traceability matrix (rule → code → test)

Every rule ID in the scaffold (`R-xx`, `B-xx`, `N-xx`) and this guide (`L-xx`) must appear in:

1. At least one **file header** or doc that implements it, and
2. At least one **test name** that verifies it, written as `[R-07] double submit is blocked while Submitting`.

`tools/check-traceability.ts` scans both and generates `docs/06-reference/traceability.md`. A rule with no test, or a test referencing an unknown rule, fails CI. This mirrors the backend's `AUTHZ_P*` path-ID idea, so frontend and backend share one auditing habit.

### 12.4 Test authoring standards

| Standard | Detail |
|---|---|
| Naming | `[RuleID] behavior in plain words` |
| Structure | Arrange / Act / Assert; one behavior per test |
| Determinism | Inject `core/clock`; no real timers or network; `MockTransport` only |
| Fixtures | Feature `__fixtures__/` are reused by screen tests and the diagnostics gallery |
| Regression | Bug fix PR includes `[BUG-123]` test that failed before the fix |
| No snapshot abuse | Snapshots only for token-driven variants; never whole screens |

---

## 13. Operating playbooks

### 13.1 Add a new feature (e.g., "invoices")

| # | Step | Where | Proof |
|---|---|---|---|
| 1 | Confirm the backend command(s), permission, tier exist (or open a backend ticket) | RBAC blueprint, backend catalog | Ticket link |
| 2 | `gen:feature invoices` | `src/features/invoices/` | Folder created |
| 3 | Add/confirm contract types and command entries | `contracts/` | Drift check green |
| 4 | Fill `feature.manifest.ts` (route, permission, tier, owner) | feature root | `check:structure` |
| 5 | Implement `api/invoicesApi.ts` (one function per command) | `api/` | Boundary lint |
| 6 | Reads: query hooks; Writes: `useMutationIntent` | `hooks/` | Intent tests |
| 7 | Build screens with five states inside `ProtectedScreen` | `screens/` | Screen tests |
| 8 | Add i18n keys (labels, errors) | `i18n/locales/` | `check:i18n` |
| 9 | Add fixtures for success, denial, step-up, approval, conflict, rate-limit, timeout | `__fixtures__/` | Fixtures used in tests |
| 10 | Register in `app/featureRegistry.ts` | app | Navigation shows route only with capability |
| 11 | Write README, run `docs:build`, open PR with template | `docs/`, `.github` | CI green |

### 13.2 Add a backend command

1. Backend: add use case + `[RequiresPermission]` + catalog entry; CI exports new `commands.snapshot.json`.
2. Frontend: update `contracts-snapshots/`; `gen:command`; fill request/response types.
3. Add `api/` function, fixture(s), tests. The command registry derives automatically.
4. `check:contracts` must pass; docs regenerate.

### 13.3 Add a permission

1. Backend `PermissionCatalog` first; snapshot exported.
2. Frontend `contracts/permissions.ts` updated; drift test passes.
3. Update the manifest of the consuming feature; screen-to-permission reference regenerates.
4. Add gate tests: visible with permission, hidden/disabled without, backend denial still handled.

### 13.4 Add an error code

1. Backend adds it; snapshot updated.
2. Frontend: `errorCodes.ts`, `errorMapper.ts` mapping, `mapAppErrorToUi.ts` directive, i18n key.
3. The "every code has directive + key" test must pass. Add a screen test showing the new behavior.
4. Update `docs/06-reference/error-codes.md` via `docs:build`.

### 13.5 Upgrade a dependency

1. Check the **verified list** (RN Windows, NativeWind) and spike notes first.
2. One dependency (or tightly coupled group) per PR; ADR if it changes a platform decision.
3. Run Windows Release build + full `verify`; record outcome in the PR.
4. Audit must pass; update the lockfile only through the package manager.

---

## 14. Debugging runbook by symptom

> Always start with the **correlation ID** and **error code** on the screen (or from the support bundle).

| Symptom | Likely layer | Check first | Where to look |
|---|---|---|---|
| Blank screen on launch | `app/AppBootstrap` | `app.bootstrap.*` breadcrumbs; bridge readiness | `AppBootstrap.tsx`, host startup validation (policy bundle/audit chain) |
| Button does nothing | `intent/` phase | Current phase in diagnostics; is it stuck in `Submitting` or `PendingApproval`? | `pendingOperationsStore`, `useIntent.ts` |
| "Access denied" but user should have access | Capabilities | `capability.refreshed` events; policy version | `CapabilityProvider`, backend `Capabilities.Get`, role assignment |
| Menu item visible but action denied | Expected (cosmetic UI) | Backend code `PERMISSION_MISSING`; refresh capabilities | Manifest permission vs backend permission mapping |
| Step-up loops | `intent/` + `security/` | Is the same idempotency key retained? Did step-up freshness reach backend? | `useStepUp.ts`, `intentMachine` AwaitingStepUp→Submitting |
| Duplicate side effect (e.g., two emails) | Idempotency | Were two keys generated for one intent? Did something resend after timeout? | `core/ids/idempotencyKey.ts`, `timeouts.ts`, B-01/B-02 tests |
| "Outcome unknown" shown | Bridge timeout | `bridge.timeout` event; command; job status | Jobs screen; backend job log by correlation ID |
| Generic error + correlation ID | `BRIDGE_CONTRACT_MISMATCH` | `bridge.contract.mismatch` event field path | `responseValidator.ts`; compare `contracts/` with backend snapshot; run `check:contracts` |
| Styles missing/wrong on Windows | NativeWind/Tailwind | Is the class in the **verified styling features** list? Dynamic class strings? `content` globs? | `tailwind.config.js`, N-06/N-07/N-10, ADR-0005 |
| Works on iOS, breaks on Windows (or reverse) | `platform/` | Is logic leaking into feature code? Platform-suffixed file present? | `src/platform/`, R-08 |
| Stale data after sign-in as another user | State | Query keys scoped by user? `resetAllStores` called? | `queryKeys.ts`, `resetAllStores.ts`, sign-out test |
| Token-like text in logs | Observability | Redaction rules; who bypassed `track()`? | `redact.ts`, lint output, `bridge` validator B-07 |
| CI red on "boundaries" | Architecture | Which import? Which direction? | `.config/dependency-cruiser.cjs`, §5 table |
| CI red on "contracts" | Drift | Backend changed? Snapshot not updated? | `contracts-snapshots/` vs `src/contracts/` |

---

## 15. Roadmap: milestones with exit criteria

Principle: **walking skeleton first**: a thin, real vertical slice through every layer before breadth. This refines scaffold §18.2.

| Milestone | Scope | Exit criteria (all must be true) |
|---|---|---|
| **M0 Foundations** | Repo hygiene: root configs, `docs/` skeleton, `tools/` check scripts, CI stage 1 to 4, CODEOWNERS, PR template, ADR 0001 to 0004 | Blank app runs on Windows; `verify` runs green; boundary lint demonstrably fails on a seeded violation |
| **M1 Contracts + core** | `contracts/` incl. `commands.ts`; `core/` (ids, Result, clock, intent machine + policy); drift tests | Exhaustive intent-transition tests pass; snapshots wired |
| **M2 Bridge on Mock** | `bridge/` with `MockTransport`; envelope, validator, mapper, timeouts | B-01 to B-07 tests pass without native code |
| **M3 Observability + security UI** | `observability/`, `security/`, `state/`, `intent/` | Scripted intent yields exact event sequence; sign-out clears all; redaction proven with seeded fake secrets |
| **M4 Design system** | `ui/` tokens, primitives, layout, feedback; NativeWind spike S1 outcome → ADR-0005 + verified-features list | Component tests pass; Windows rendering verified |
| **M5 Walking skeleton** | `app/` + `session` + `dashboard` + one T2 write (`gmail` against Mock) | Whole pipeline (§6.1) demonstrable: confirm → step-up → outcome-unknown, all on Mock |
| **M6 Windows native bridge** | `Pipeline.NativeBridge`; `Pipeline.GetStatus` real round-trip (spike S2) | Release build works without Metro |
| **M7 Read-only features** | `jobs`, `integrations`, `sheets` (read), `audit`, `diagnostics` | Five-state tests; runbook rehearsed on a seeded fault |
| **M8 T2 features** | `gmail`, `chat`, `zohoExpense`, `bigin`, `sheets` (write) | Idempotency, confirm, step-up, outcome-unknown tested per feature |
| **M9 T3 features** | `payments`, `approvals`, `admin`, integrations connect/disconnect | Pending-approval + SoD UX; backend contract verified end to end |
| **M10 iOS** | iOS native bridge, cloud build (spikes S3, S5) | Same round-trip on iOS; accessibility and platform checklists pass |
| **M11 Hardening** | i18n completeness, a11y audit, perf budgets, bundle-size trend, docs freshness review | All scaffold §14 to §17 checklists pass; onboarding dry run by a new developer succeeds |

**Spike policy:** each spike (S1 to S5) ends with an ADR (or "rejected" ADR) and a docs update. No dependency version is pinned before its spike concludes.

---

## 16. Amendments to the existing scaffold

These are deliberate refinements. Record them as ADRs when adopted.

| # | Scaffold says | Issue | Amendment |
|---|---|---|---|
| A-1 | Command list appears in `commandRegistry.ts` (§8.2) and the command table (§10.1) | Two lists will drift | `contracts/commands.ts` is the single source; registry and docs derive from it |
| A-2 | `bridge/idempotency.ts`, `bridge/correlation.ts` | `intent/` and `observability/` need IDs without importing `bridge/` (which they must not) | Move to `core/ids/` (ADR-0004) |
| A-3 | Write rules (idempotency, double-submit, step-up, approval, outcome-unknown) live in prose and per-hook code | Each feature could implement them slightly differently | Centralize in `core/intent` (pure) + `intent/` (React) |
| A-4 | `routes.ts` holds the route→permission map; §11.2 holds the human table | Retyped data drifts | Derive from `feature.manifest.ts`; test that §11.2 reference equals derived map |
| A-5 | `console.*` forbidden except `secureLogger.ts` | No positive pattern for tracking | Add `observability/` with typed events and breadcrumbs |
| A-6 | RBAC blueprint §9.11 names a `services/` test folder for an "IPC client" | Frontend scaffold uses `bridge/` | Treat `__tests__/bridge/` as the equivalent; update the RBAC blueprint wording to match |
| A-7 | Feature `README` / ownership not specified | Ownership gaps at scale | Required per-feature README + manifest `owner` + CODEOWNERS check |
| A-8 | Scaffold coverage targets cover `bridge/`, `security/`, `contracts/` | New critical layers | Add `core/` and `intent/` at ≥ 90% |
| A-9 | `features/integrations` provides `ReconnectBanner`, which other features' errors also need | Cross-feature reuse is forbidden (R-04) | Move `ReconnectBanner` to `security/` (or `intent/`) as a shared behavioral component; `integrations` consumes it |

---

## 17. Anti-patterns and review checklist

### 17.1 Anti-patterns (reject on sight)

| Anti-pattern | Why it hurts | Do instead |
|---|---|---|
| A screen calling the bridge or a native module | Breaks the one door (R-01) | Feature hook → feature `api/` |
| `isLoading`, `isSending`, `hasError` flags per screen | Illegal state combos | Intent phase / query state unions |
| Retrying a write on timeout "to be nice" | Duplicate side effects | `OutcomeUnknown`, user checks job status |
| Checking `role === "admin"` in UI | Role logic belongs to backend | `usePermission(permission)` from capabilities |
| Hex colors, magic spacing, inline style objects | Design drift | Tokens in Tailwind config |
| Dynamic class names (`` `bg-${color}-500` ``) | Tailwind can't generate them (N-07) | Fixed class sets chosen by condition |
| Showing `error.message` | Leaks provider/internal text | `AppError.code` → i18n key |
| Storing anything sensitive via AsyncStorage | Not protected storage | Keep it in host; UI holds status only |
| Cross-feature imports | Hidden coupling | Promote shared code to a lower layer |
| "Utility" dumping ground | Becomes a junk drawer | Place by §5.3 decision tree |
| Platform `if (Platform.OS …)` scattered in features | Divergence everywhere | `platform/` and suffixed files |
| Copying a type from the backend by hand | Drift | Contract snapshot + drift test |
| Big-bang PRs | Unreviewable, risky | ≤ 400 lines, one concern |

### 17.2 Reviewer checklist (mirrored in `PULL_REQUEST_TEMPLATE.md`)

**Architecture**
- [ ] Layer/boundary rules respected; no new cycles
- [ ] No new `bridge` import outside `features/*/api/`
- [ ] New logic is pure where possible (L-03)

**Behavior**
- [ ] Writes use `useMutationIntent`; no auto-retry; correct tier UX
- [ ] All five screen states present
- [ ] Backend denial still handled where UI gates exist

**Security & privacy**
- [ ] No secrets/tokens/payloads in code, logs, stores, or events
- [ ] Errors shown from codes + i18n, with correlation ID

**Quality**
- [ ] Tests added with rule IDs in names; regression test for bug fixes
- [ ] Tokens only; accessibility labels; keyboard path (Windows)
- [ ] No hard-coded strings; i18n keys present
- [ ] File/function limits respected; no dead code

**Documentation & tracking**
- [ ] README/manifest updated; `docs:build` run
- [ ] ADR linked if a boundary, library, or security behavior changed
- [ ] Ticket linked; Conventional Commit title; screenshots (Windows + iOS)

---

## 18. Templates

### 18.1 Feature README template

```markdown
# <Feature name>

**Owner:** @team-or-person   **Tier (max):** T0 | T1 | T2 | T3   **Status:** active | experimental

## Purpose
One or two sentences: what user problem this feature solves.

## Commands used
| Command | Permission | Tier | State-changing |
|---|---|---|---|

## Screens
| Screen | Route | Required permission |
|---|---|---|

## Behavior notes
- Confirm/step-up/approval behavior specific to this feature
- Known edge cases (e.g., outcome-unknown handling)

## Fixtures available
success · denial · step-up · approval · conflict · rate-limit · timeout

## Related
- Docs: docs/…   - ADRs: 000x   - Backend: <use case / blueprint section>
```

### 18.2 ADR template

```markdown
# ADR-000X: <Decision title>

- **Status:** Proposed | Accepted | Superseded by ADR-000Y
- **Date:** YYYY-MM-DD
- **Deciders:** @names
- **Related rules:** R-xx, L-xx

## Context
What forces, constraints, and evidence led here? (Link spikes, tickets.)

## Decision
The decision in one clear paragraph, imperative voice.

## Alternatives considered
| Option | Pros | Cons | Why not |
|---|---|---|---|

## Consequences
Positive, negative, and follow-up work. How will we know it was right?

## Verification
Which tests/lint rules/CI gates enforce this decision?
```

### 18.3 Pull request template

```markdown
## What and why
<One paragraph. Link ticket: PIPE-___>

## Type
- [ ] feat  - [ ] fix  - [ ] refactor  - [ ] docs  - [ ] chore  - [ ] test

## Scope
Feature/layer: ______   Tier touched: T0 / T1 / T2 / T3

## Evidence
- [ ] `npm run verify` green
- [ ] Screenshots: Windows ___  iOS ___
- [ ] Correlation/event sequence verified (if a write flow changed)

## Checklist
(copy §17.2 here)

## ADR / docs
- ADR: ______ (required if boundary/library/security behavior changed)
- Docs updated: ______
```

### 18.4 Bug report template (forces trackability)

```markdown
**Summary:**
**Platform / app version / build:**   (from Settings → About or support bundle)
**Correlation ID(s):**
**Error code shown (if any):**
**Steps to reproduce:**
**Expected / Actual:**
**Support bundle attached:** yes / no
**Suspected layer (optional):** UI · intent · bridge · host · provider
```

### 18.5 Runbook entry template

```markdown
### <Symptom in the user's words>
- **First check:** (event, code, screen)
- **Likely causes (ordered):**
- **How to confirm:** (command, breadcrumb, log field)
- **Fix / workaround:**
- **Prevention:** (test, lint, doc)
- **Related:** ADR / rule IDs
```

---

## Summary: what makes this codebase safe to scale

1. **One direction of dependency**, machine-enforced, with a tiny pure core underneath.
2. **One door to the backend** and **one definition per concept** (commands, permissions, errors, statuses), proven by drift tests.
3. **One pipeline for every write**, modeled as a pure state machine with named phases, so retries, step-up, approvals, and unknown outcomes behave identically in every feature.
4. **One identity per action** (correlation ID + idempotency key) visible from the click to the audit log.
5. **Features as mini-apps** with manifests, READMEs, fixtures, owners, and a generator, so growth is additive.
6. **Docs, ADRs, rule IDs, and traceability** treated as code: reviewed, generated where possible, and checked in CI.
7. **A walking skeleton first**, spikes that end in written decisions, and milestones with exit criteria rather than dates.
