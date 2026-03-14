# ask-first/

Mandatory pre-implementation workflow for every dev task. Three phases: Interrogate -> Test-First -> Implement.

## Files

| File | Role |
|------|------|
| `SKILL.md` | Full workflow guide — Phase 1 (planning), Phase 2 (black-box test writing), Phase 3 (TDD implementation) |

## Key Concepts

- **Phase 1:** Ask yourself 10 questions before touching code. Rate confidence 1-5. If < 3, ask the user.
- **Phase 2:** Dispatch a separate agent to write tests with ZERO implementation knowledge. Minimum: 1 positive + 1 negative per method, integration per cross-class call, E2E per feature.
- **Phase 3:** Standard TDD — make each test green, commit, verify no regressions.

## Related Skills

- **`clean-room-qa`** — Black-box testing methodology (foundation for Phase 2)
- **`reverse-engineering`** — Debugging methodology (useful during Phase 1)
- **`unity-testing-patterns`** — UTF code examples, assertions, mocking
- **`unity-e2e-testing`** — PlayMode testing, InputTestFixture
- **`unity-testing-debugging-qa`** — Master QA reference
