# scripts/tools/

Build utilities, asset generation, and validation scripts. Python scripts run via `uv run python <script>`.

## Python Scripts

| File | Role |
|------|------|
| `validate_claude_md.py` | Check CLAUDE.md freshness against directory git dates |
| `validate_registry.py` | Validate system manifests (JSON + .tres) against disk; checks `tests` field ownership and file existence |
| `resolve_module_tests.py` | Resolve which test classes to run given a list of changed files (module-based test gating) |
| `test_coverage_report.py` | Track per-category test coverage with baseline comparison |

## Shell Scripts

| File | Role |
|------|------|
| `safe-worktree-init.sh` | Initialise a safe worktree for a task (called by subagents) |
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
