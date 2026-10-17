# PipelineSolution

> Enterprise Infrastructure Modular Monolith (.NET 10) & React Native (Windows + iOS) Presentation with Bypass-Resistant RBAC, Single Choke Point Dispatcher, Capability Tokens, Concentric Layers, and 100% Path Traceability.

---

## Architecture Overview

1. **Backend (.NET 10 LTS):**
   - **Modular Monolith:** Strict Clean Architecture (`Domain` $\leftarrow$ `Application` $\leftarrow$ `Infrastructure` / `Host` / `LocalApi` / `Integrations.*`).
   - **Single Choke Point (`IUseCaseDispatcher`):** Every UI/IPC operation passes through a fixed behavior pipeline: `Authentication -> Validation -> Authorization -> RateLimit -> Idempotency -> Audit -> Handler`.
   - **Bypass-Resistant RBAC (`S01`–`S11`):** Enforces 10-step authorization algorithm producing deterministic `ReasonCode` and `PathId` (`AUTHZ_P01`..`AUTHZ_P11`, `AUTHZ_PX`).
   - **Capability Tickets (`AuthorizationTicket`):** Cryptographic HMAC capability tokens required by `ICredentialBroker` before granting provider tokens. No direct access to `ICredentialStore` or `DpapiCredentialStore`.
   - **Startup & Architecture Guard:** `UseCaseRegistryValidator` and `NetArchTest.Rules` ensure zero unprotected operations or architectural bypasses.

2. **Frontend (`src/Pipeline.Presentation/`):**
   - **React Native (Windows + iOS) + Expo Modules + NativeWind v4 + TypeScript:** Governed by Engineering Laws `L-01`–`L-12`.
   - **Concentric Layer Direction:** `Layer 7 app` $\rightarrow$ `Layer 6 features/*` $\rightarrow$ `Layer 5 intent, platform` $\rightarrow$ `Layer 4 security, observability` $\rightarrow$ `Layer 3 ui, state` $\rightarrow$ `Layer 2 bridge` $\rightarrow$ `Layer 1 core, config, utils, i18n, hooks` $\rightarrow$ `Layer 0 contracts`.
   - **Single Bridge Door (`bridge/`):** Exclusively called by `features/*/api/*.ts`. Never accepts arbitrary URLs or headers.
   - **Pure Intent State Machine (`core/intent/`):** 9-phase deterministic state machine. Never blindly retries writes on timeout (`OutcomeUnknown` phase).
   - **Contract Drift Verification:** `contracts-snapshots/` mirrors backend catalogs.

---

## Quick Start & Verification

### Prerequisites
- **.NET SDK:** `10.0.301` or later
- **Node.js:** `v22.20.0` or `>=22.13.0 <25.0.0`
- **npm:** `11.7.0` or `>=10.9.0`

### Scaffolding & Verification Engine (`scaffold.sh`)

`scaffold.sh` is a modular, idempotent scaffolding and verification engine powered by `scripts/`:

```bash
# 1. Idempotently scaffold or update solution, projects, and layers
./scaffold.sh --scaffold

# 2. Force re-generation of all manifests and solution templates
./scaffold.sh --scaffold --force

# 3. Audit existing files and project inventory (dry run)
./scaffold.sh --dry-run

# 4. Check host toolchain prerequisites (.NET 10, Node 22, npm)
./scaffold.sh --check-prereqs

# 5. Execute full end-to-end verification (build with -warnaserror + all tests)
./scaffold.sh --verify

# 6. Execute test battery across .NET and Presentation
./scaffold.sh --test

# 7. Clean build and test artifacts
./scaffold.sh --clean
```

### Manual Verification Commands

```bash
# Backend Verification (.NET 10)
dotnet restore PipelineSolution.slnx
dotnet build PipelineSolution.slnx -warnaserror
dotnet test PipelineSolution.slnx --filter "Category=Unit|Category=Path|Category=Architecture|Category=Security"

# Frontend Verification (Node 22 / TypeScript / Jest)
cd src/Pipeline.Presentation
npm install
npm run verify
```
