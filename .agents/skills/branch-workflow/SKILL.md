---
name: branch-workflow
description: Branch Workflow & Merge Queue Skill
---

# Branch Workflow & Merge Queue Skill

Use this skill when creating feature branches, managing worktrees, pushing code, opening PRs, or handling merges. Covers the full branch lifecycle from worktree creation through merge.

## The Golden Rule

**Never commit or push directly to main.** Local hooks will block you. All work goes through feature branches and pull requests.

## Step-by-Step: Feature or Fix

### 1. Create an Isolated Worktree

```bash
just worktree-create <task-name>
# Fetches origin/main, fast-forwards local main, then creates
# feat/<task-name> branch from the latest remote main.
# Worktree lives at ../<project>-<task-name>/
```

If you're a Claude Code subagent, run `bash scripts/tools/safe-worktree-init.sh <task-name>` as your FIRST action — it fetches `origin/main`, creates the feature branch, and prints `WORKTREE_PATH=/absolute/path`. All subsequent work must be done in that worktree using absolute paths.

### 2. Develop on the Feature Branch

- Commit frequently. Commit message format: `type: short description`.
- Follow TDD: write test first (RED), implement (GREEN), commit.

### 3. Push, Create PR, and Merge

For subagents, the one-command path is:

```bash
just lifecycle ship    # push + create PR + squash-merge + sync local main
```

Or manually:

```bash
git push -u origin feat/<task-name>
gh pr create --base main --title "feat: description" --body "..."
gh pr merge <number> --squash
git fetch origin main && git update-ref refs/heads/main origin/main
```

> **No GitHub Actions** — PRs are merged directly with `gh pr merge --squash`. No CI pipeline, no `ready-to-merge` label.

### 4. Local Fast-Forward (Mid-Development)

After a push, fast-forward local main to give next subagent immediate access:

```bash
just ff-main
```

After remote PR merges, sync with `just lifecycle ship` (automatic) or manually:
```bash
git fetch origin main && git update-ref refs/heads/main origin/main
```

### 5. Cleanup

**Subagents:** `just lifecycle ship` handles everything through merge.

**Main agent after subagent merge:**
```bash
just task-complete <task-name>    # Verify merge, delete branch/worktree/tags, sync main
# OR
just worktree-sync                # Batch: pull main, delete merged branches, prune
```

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

## Agent Protocol

### Subagents (Self-Managed Worktree Pattern)

Subagents no longer use `isolation: "worktree"`. Every subagent creates its own worktree as the first action:

```bash
# Step 1: Create worktree (ALWAYS first action)
just lifecycle init <task-name>
# OR: bash scripts/tools/safe-worktree-init.sh <task-name>
# Output: WORKTREE_PATH=/absolute/path/to/worktree
```

**Why self-managed?** When Claude Code tears down an `isolation: "worktree"` subagent, the platform switches the main repo's HEAD to the feature branch. Self-managed worktrees avoid this — the main repo stays on `main`.

**Critical rules for subagents:**
1. **Run `lifecycle init` or `safe-worktree-init.sh` as the FIRST action**
2. **Use absolute paths for all operations** — shell `cd` does not persist between calls
3. **Never edit files in `CLAUDE_PROJECT_DIR`** — only work in the worktree

Subagent lifecycle:
1. `just lifecycle init <task>` → capture `WORKTREE_PATH`
2. TDD develop: write test (RED), implement (GREEN), commit each file immediately
3. `just lifecycle ship` → push + create PR + squash-merge + sync local main
4. Report back with summary of changes, files affected, downstream implications

**Subagents must NOT exit until step 3 is complete.**

### Main Agent Responsibilities

1. **Never edit files directly** — dispatch subagents for code changes
2. **Verify subagent work** — check the PR diff
3. **Cleanup after merge**: `just task-complete <task>` (verifies merge, removes worktree/branch/tags, syncs main)
4. **Coordinate sequential tasks** — dispatch one subagent at a time; update in-flight memories between dispatches

### Hard Rules for All Agents

- **NEVER** commit on the `main` branch
- **NEVER** use `--no-verify` on git commit
- **NEVER** leave a branch unmerged
- **NEVER** use `isolation: "worktree"` — subagents call `safe-worktree-init.sh` themselves
- **ALWAYS** provide a task name to subagents

## Gotchas & Common Mistakes

### "BLOCKED: Cannot commit on main branch"

The Claude Code PreToolUse hook detected a `git commit` on the `main` branch. Switch to a feature branch.

### "BLOCKED: --no-verify is not allowed"

Never use `--no-verify`. Fix the root cause instead.

### Branching From Stale Main

Always use `just worktree-create` or `safe-worktree-init.sh` — they fetch before branching.

### Merge Conflicts

If `lifecycle ship` fails because the branch is behind main:

```bash
git fetch origin main
git rebase origin/main
git push --force-with-lease
gh pr merge <number> --squash
```

Or re-run `just lifecycle ship` — it auto-rebases if behind.

### Parallel PRs

Merge sequentially. Pre-rebase before shipping:

```bash
git fetch origin && git rebase origin/main
```

### Dirty Worktrees

```bash
just worktree-sync           # Batch cleanup: pull main, delete merged branches, prune
just worktree-cleanup <task> # Single cleanup
```

## Emergency Procedures

### PR Not Merging

```bash
gh pr view <number>                                    # Check PR state
gh pr view <number> --json mergeable -q .mergeable     # Check conflicts
git fetch origin main && git rebase origin/main        # Rebase if behind
git push --force-with-lease
gh pr merge <number> --squash
```

### Failed Rebase (Unrecoverable Conflicts)

```bash
git rebase --abort
git fetch origin main
git checkout -b feat/<task>-v2 origin/main
git cherry-pick <commit1> <commit2> ...
git push -u origin feat/<task>-v2
gh pr close <old-number>
gh pr create --base main
```

### Release / Hotfix Bypass

```bash
ALLOW_MASTER_COMMIT=1 git commit -m "chore: release 1.0.0"
ALLOW_MASTER_PUSH=1 git push origin main
```

Repo admins only.

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
