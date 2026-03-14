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

- `.agents/skills/unity-testing-patterns/SKILL.md` — TDD with Unity Test Framework
