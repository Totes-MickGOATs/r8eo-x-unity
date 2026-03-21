---
name: branch-workflow
description: Branch Workflow & Merge Queue Skill
---


# Branch Workflow & Merge Queue Skill

Use this skill when creating feature branches, managing worktrees, pushing code, opening PRs, or handling merges. Covers the full branch lifecycle from worktree creation through merge.

## The Golden Rule

**Never commit or push directly to main.** Local hooks will block you. All work goes through feature branches and pull requests.

## Definition of Done

| # | Criterion | How to Verify |
|---|-----------|---------------|
| 1 | PR is open with all commits pushed | `gh pr view` shows your PR |
| 2 | Local pre-commit hooks passed | Coverage ratchet + assert audit are FAIL gates |
| 3 | PR merged | `gh pr view --json state -q .state` returns `MERGED` |
| 4 | Local main updated | `git rev-parse --short main` matches merge commit |
| 5 | Knowledge synced | CLAUDE.md files, manifests updated if applicable |

### Owning Your Branch

You are responsible for your branch from push to merge:

1. Ensure pre-commit gates pass (coverage ratchet, assert audit)
2. Ensure pre-push tests pass (if `UNITY_PATH` is set)
3. Merge with `gh pr merge --squash` or `just lifecycle ship`
4. Confirm merge: `gh pr view --json state -q .state`
5. Sync local main after merge

**Do not** leave your branch unmerged at the end of a task.

### Self-Reflection on Failure

When a gate fails or merge fails:

1. **Read the actual error** — don't guess.
2. **Identify root cause** — is it your code, a failing test, a merge conflict?
3. **Fix and push** — address the underlying issue, don't retry blindly.
4. **Update memory if needed** — record new gotchas for future agents.

## Enforcement Layers

| Layer | Mechanism | Bypassable? |
|-------|-----------|-------------|
| **Claude Code PreToolUse hook** | Blocks `git commit` on main and `--no-verify` usage | No — runs before the command |
| **Git pre-commit hook** | `.githooks/pre-commit` — branch guard + coverage + assert audit + line limit | Yes — `--no-verify` (but layer 1 blocks that) |

> GitHub branch protection is optional — server-side enforcement depends on repo settings.

## Tag-Based Worktree Lifecycle

| Tag | Meaning | Created when |
|-----|---------|-------------|
| `wt/active/<task>` | In progress — DO NOT delete | `just worktree-create <task>` |
| `wt/done/<task>` | Completed — safe to delete | `just worktree-mark-done <task>` |

- `worktree-cleanup` refuses to delete a worktree with a `wt/active/*` tag
- Override: `FORCE=1 just worktree-cleanup <task>`
- `just task-complete <task>` handles all cleanup atomically after merge

### Abandoning a Worktree

```bash
just worktree-mark-abandoned <task>   # Mark safe to delete without merged PR
just worktree-cleanup <task>
```

## Lifecycle Command Reference

| Command | Purpose |
|---------|---------|
| `just lifecycle init <task>` | Subagent: create worktree (delegates to safe-worktree-init.sh) |
| `just lifecycle ship` | Subagent: push + create PR + squash-merge + sync local main |
| `just task-complete <task>` | Main agent: verify merge, clean up worktree/branch/tags, sync main |
| `just worktree-create <task>` | Create `feat/<task>` branch + worktree from `origin/main` |
| `just worktree-sync` | Pull main, auto-clean done worktrees, report active ones |
| `just worktree-list` | Show all worktrees with branch, tag status, PR status |
| `gh pr merge <number> --squash` | Merge a PR directly |
| `gh pr view --json state -q .state` | Check PR merge state |

## Infrastructure Files

| File | Role |
|------|------|
| `.githooks/pre-commit` | Branch guard + lint + coverage ratchet + assert audit + line limit |
| `.githooks/pre-push` | Branch push guard + module-gated test check |
| `scripts/tools/subagent-lifecycle.sh` | `init <task>` and `ship` — consolidated subagent lifecycle |
| `scripts/tools/task-complete.sh` | Main agent one-command post-merge cleanup |
| `scripts/tools/assert_audit.py` | Static assertion verifier for test methods |
| `scripts/tools/test_coverage_report.py` | Coverage baseline + per-module ratchet |


## Topic Pages

- [Step-by-Step: Feature or Fix](skill-step-by-step-feature-or-fix.md)
- [Agent Protocol](skill-agent-protocol.md)

