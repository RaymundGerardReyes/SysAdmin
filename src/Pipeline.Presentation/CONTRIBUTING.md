# Contributing to Pipeline.Presentation

1. Review `docs/00-start-here/onboarding.md` and `docs/06-reference/rules.md`.
2. Do not introduce cross-feature imports (`features/<a>` must never import from `features/<b>`).
3. Only `features/*/api/*.ts` may import `bridge/`.
4. Writes must use `useMutationIntent` and respect the 9-phase intent machine.
5. Every rule in `rules.md` cited in code must have an automated test named `[RuleID]`.
6. Run `npm run verify` locally before submitting a PR.
