# Contributing to PipelineSolution

## Engineering Laws & Invariants
Every pull request must uphold the architectural invariants in `AGENTS.md`:
1. **Never bypass `IUseCaseDispatcher`:** No direct access from UI/Host to internal connectors.
2. **Every handler requires permission:** `[RequiresPermission("...")]` matching `PermissionCatalog`.
3. **No direct credential store calls:** Handlers only access credentials through `ICredentialBroker` using an `AuthorizationTicket`.
4. **Concentric layer imports in frontend:** Enforced by `npm run check:boundaries`.
5. **Traceability:** Test methods and code headers must cite rule IDs (`[R-xx]`, `[L-xx]`, `[B-xx]`, `AUTHZ_P*`, `IDEM_P*`).
6. **No weakening tests:** Never delete, skip, or modify existing tests to make a PR green.
