# Layer 2: Bridge

**Purpose:** The single door between frontend and the .NET host / native bridge.
**Allowed imports:** `contracts/`, `core/`, `utils/`, `config/`.
**Rules:** R-01, B-01, B-02, B-07. Only `features/*/api/*.ts` (and app bootstrap) may import this folder.
