# Layer 6: Features

**Purpose:** Feature-sliced modules. Each feature is a self-contained vertical slice with its own manifest, screens, components, hooks, api, and fixtures.
**Allowed imports:** `ui/`, `security/`, `state/`, `intent/`, `observability/`, `contracts/`, `core/`, `hooks/`, `utils/`, `config/`, `platform/`; **`bridge/` only inside `api/*.ts`**.
**Rules:** L-01, R-04 (No cross-feature imports!). Features never import from other features.
