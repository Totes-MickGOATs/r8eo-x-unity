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

## Coverage Enforcement

Three layers enforce test coverage:
1. **Pre-commit:** Warns if modified scripts have no corresponding test file
2. **Pre-push:** Runs tests for changed files, blocks push on failure (bypass: `SKIP_TEST_CHECK=1`)
3. **CI:** Coverage baseline ratchet -- total test count can never decrease

**Module-based test gating:** When files in a module change, pre-push and `just ff-main` automatically
run ALL tests declared for that module plus transitive dependents. Test classes are resolved from
`tests.editmode` / `tests.playmode` in each manifest. See `scripts/tools/resolve_module_tests.py`.

Baseline file: `.coverage-baseline.json` (project root). Updated automatically when tests are added.
Coverage report: `uv run python scripts/tools/test_coverage_report.py` (categories loaded from manifests)

## Relevant Skills

- **`unity-testing-patterns`** — TDD with Unity Test Framework
- **`unity-testing-debugging-qa`** — Testing, debugging, and quality assurance workflows
- **`clean-room-qa`** — Independent QA validation process
