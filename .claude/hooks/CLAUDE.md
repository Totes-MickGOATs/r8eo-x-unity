# hooks/

Claude Code event hooks. These run automatically at various lifecycle points.

## Files

| File | Hook Event | Role |
|------|------------|------|
| `lint-on-save.sh` | PostToolUse (Edit/Write) | Lint saved files, dispatch to engine-specific linter |
| `lint-session-stop.sh` | SessionEnd | Full lint sweep report at end of session |
| `pre-compact-context.sh` | PreCompact | Capture branch/PR state before context compaction; warn about context budget |
| `session-start.sh` | SessionStart | Configure git hooks, run engine-specific init |
| `stop-uncommitted-check.sh` | Stop | Detect worktree branch contamination (auto-recover) + warn about uncommitted code files |
| `subagent-post-stop.sh` | SubagentStop | Auto-commit stray changes, verify subagent pushed and created PR, remind about Definition of Done |
| `worktree-setup.sh` | WorktreeCreate | **DEPRECATED** — only fires for `isolation:"worktree"` which is no longer used. See `scripts/tools/safe-worktree-init.sh`. |

## Engine Hooks

After `setup-engine.sh` runs, engine-specific hooks are copied here:
- `lint-on-save-engine.sh` -- engine-specific file linting
- `pre-commit-engine.sh` -- engine-specific pre-commit checks
- `worktree-setup-engine.sh` -- **DEPRECATED** — Library junction logic moved to `scripts/tools/safe-worktree-init.sh`
- `session-start-engine.sh` -- sourced by `session-start.sh`; no-op stub present, override with engine impl
- `lint-session-stop-engine.sh` -- sourced by `lint-session-stop.sh`; no-op stub present, override with engine impl
