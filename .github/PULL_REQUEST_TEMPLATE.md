## What and Why
<!-- Link issue / ticket -->

## Architectural Invariants Checklist
- [ ] No direct connector access from UI/Host (must use `IUseCaseDispatcher`)
- [ ] Handler has `[RequiresPermission]` with a catalog permission
- [ ] No direct credential store access (uses `ICredentialBroker` + `AuthorizationTicket`)
- [ ] Concentric layer direction respected (no boundary violations)
- [ ] No cross-feature imports
- [ ] Rule IDs cited in test names and file headers
- [ ] Regression test added for any bug fix
- [ ] Automated verification passes (`./scaffold.sh --verify` or `dotnet test` + `npm run verify`)
