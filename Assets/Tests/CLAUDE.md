# Assets/Tests/

Unity Test Framework test suites organized by test mode.

## Subdirectories

| Dir | Assembly | Purpose |
|-----|----------|---------|
| `EditMode/` | `R8EOX.Tests.EditMode` | Pure logic tests — no Play mode required |

## Conventions

- Test naming: `MethodName_Condition_ExpectedResult`
- Test class naming: `{ClassName}Tests.cs`
- TDD mandatory: RED → GREEN → COMMIT
- 100% coverage on physics formulas

## Relevant Skills

- **`unity-testing-patterns`** — TDD with Unity Test Framework
- **`unity-testing-debugging-qa`** — Testing, debugging, and quality assurance workflows
- **`clean-room-qa`** — Independent QA validation process
