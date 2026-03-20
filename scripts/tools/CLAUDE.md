# scripts/tools/

Build utilities, asset generation, and validation scripts. Python scripts run via `uv run python <script>`.

## Python Scripts

| File | Role |
|------|------|
| `validate_claude_md.py` | Check CLAUDE.md freshness against directory git dates |
| `validate_registry.py` | Validate system manifests (JSON + .tres) against disk; checks `tests` field ownership and file existence |
| `resolve_module_tests.py` | Resolve which test classes to run given a list of changed files (module-based test gating) |
| `test_coverage_report.py` | Track per-category test coverage with baseline comparison; `--check-modules` for per-module ratchet |
| `assert_audit.py` | Verify every `[Test]`/`[UnityTest]` method body contains at least one assertion; `--all` scans all test files |
| `pre_commit_checks.py` | Unified pre-commit entry point: runs coverage ratchet + assert audit in one process, loading manifests once; reads staged `.cs` paths from stdin |
| `lint_csharp_policy.py` | Policy linter for C# runtime assemblies: blocks Debug.Log*, FindObject/Resources.Load, GUID refs in asmdefs, string-based layer/tag/scene lookups, and manifest orphans; `--staged`, `--changed-against <ref>`, `--all` modes |

## Shell Scripts

| File | Role |
|------|------|
| `get_changed_files.sh` | Normalize changed file selection for lint tools: `--staged`, `--changed <ref>`, `--all`, `--cs` filter |
| `safe-worktree-init.sh` | Create a single feature branch + worktree from origin/main |
| `safe-worktree-init-batch.sh` | Batch create multiple worktrees with a single git fetch |
| `subagent-lifecycle.sh` | Consolidated subagent commands: `init <task>` (delegates to safe-worktree-init.sh), `submit` (queue current branch), and `ship` (push+PR+merge+sync remote fallback) |
| `task-complete.sh` | Main agent one-command cleanup: accepts local promotion (merge-base check) or remote PR merge; deletes branch, worktree, tags, syncs main |
| `unity-queue.sh` | Local verification queue — serializes Unity compile/test jobs through a dedicated verifier worktree; commands: `submit`, `run`, `run-all`, `promote`, `status`, `init-verifier` |
| `stream-status.sh` | Dashboard: commit count, push status, and PR state for all active streams |
| `conflict-forecast.sh` | Detect files touched by multiple active worktree branches |
| `syntax-check-csharp.sh` | Lightweight pre-commit C# checks: balanced braces, namespace, line limit |
| `check_line_limit.sh` | CI enforcement: fail if any .cs file exceeds 200 lines |
| `worktree-audit.sh` | Detect ghost `wt/active/*` tags with no matching local branch |

## Notes

- Python scripts use `uv` package manager (`.venv/` at project root)
- Run: `uv run python scripts/tools/<script>.py`
- Shell scripts: `bash scripts/tools/<script>.sh` or via `just` recipe

## Adding New Scripts

1. Write the script in this directory
2. Add it to this CLAUDE.md file listing
3. If it should run in CI, add it to `.github/workflows/ci.yml`
4. If it has a just recipe, add to `justfile` or `justfile.engine`
