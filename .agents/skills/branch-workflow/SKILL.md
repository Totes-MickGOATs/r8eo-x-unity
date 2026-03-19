# Branch Workflow & Merge Queue Skill

Use this skill when creating feature branches, managing worktrees, pushing code, opening PRs, or handling the merge queue. Covers the full branch lifecycle from worktree creation through auto-merge.

## The Golden Rule

**Never commit or push directly to main.** Local hooks and GitHub branch protection will block you. All work goes through feature branches and pull requests.

## Step-by-Step: Feature or Fix

### 1. Create an Isolated Worktree

```bash
just worktree-create <task-name>
# Fetches origin/main, fast-forwards local main, then creates
# feat/<task-name> branch from the latest remote main.
# Worktree lives at ../<project>-<task-name>/
cd ../<project>-<task-name>
```

This **always pulls the latest main from remote** before branching, so your feature branch starts from the true current state of main — not a stale local copy.

If you're a Claude Code subagent, run `bash scripts/tools/safe-worktree-init.sh <task-name>` as your FIRST action — it fetches `origin/main`, creates the feature branch, and prints `WORKTREE_PATH=/absolute/path`. All subsequent work must be done in that worktree using absolute paths.

### 2. Develop on the Feature Branch

- Commit frequently. Each file change gets its own commit (or a grouped commit if changes are atomic).
- Commit message format: `type: short description` (e.g., `feat: add surface friction zone`, `fix: correct wheel radius`).
- Follow TDD: write test first, run it red, implement, run it green, commit.

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

> **No GitHub Actions** — PRs are merged directly with `gh pr merge --squash`. No CI pipeline, no `ready-to-merge` label, no auto-merge workflow.

### 4. Local Fast-Forward (Mid-Development Speed Optimization)

After a successful push (pre-push tests passed), fast-forward local main to the current branch:

```bash
just ff-main
```

**Why:** Gives the next subagent immediate access to your changes on local main mid-development.

**When to use:** Mid-development only — BEFORE the PR merges.

**After remote PR merges:** Use `git update-ref` to sync local main (or use `just lifecycle ship` which does this automatically):
```bash
git fetch origin main && git update-ref refs/heads/main origin/main
```

### 5. Cleanup

**Subagents:** `just lifecycle ship` handles everything through merge.

**Main agent after subagent merge:**
```bash
just task-complete <task-name>    # Verify merge, delete branch/worktree/tags, sync main
# OR
just worktree-cleanup <task-name> # Manual cleanup
just worktree-sync                # Batch: pull main, delete merged branches, prune
```

## Definition of Done

A task is **not done** until ALL of the following are true. Agents must self-monitor and self-correct until every item is satisfied.

| # | Criterion | How to Verify |
|---|-----------|---------------|
| 1 | PR is open with all commits pushed | `gh pr view` shows your PR |
| 2 | Local pre-commit hooks passed | Coverage ratchet + assert audit are FAIL gates |
| 3 | PR merged | `gh pr view --json state -q .state` returns `MERGED` |
| 4 | Local main updated | `git rev-parse --short main` matches merge commit |
| 5 | Knowledge synced | CLAUDE.md files, manifests updated if applicable |

### Owning Your Branch

**You are responsible for your branch from push to merge.** This means:

1. Ensure pre-commit gates pass (coverage ratchet, assert audit)
2. Ensure pre-push tests pass (if UNITY_PATH is set)
3. Merge with `gh pr merge --squash` or `just lifecycle ship`
4. Confirm merge: `gh pr view --json state -q .state`
5. Sync local main after merge

**Do not:**
- Leave your branch unmerged at the end of a task
- Skip local test gates (coverage ratchet is a hard FAIL)

### Self-Reflection on Failure

When a gate fails or merge fails:

1. **Read the actual error** — don't guess.
2. **Identify root cause** — is it your code, a failing test, a merge conflict?
3. **Fix and push** — don't just retry. Address the underlying issue.
4. **Update memory if needed** — if you discovered a new gotcha, record it so future agents don't hit the same issue.

## Enforcement Layers

This project has **three layers** preventing direct main commits:

| Layer | Mechanism | Bypassable? |
|-------|-----------|-------------|
| **Claude Code PreToolUse hook** | Blocks `git commit` on main and `--no-verify` usage | No — runs before the command, cannot be skipped |
| **Git pre-commit hook** | `.githooks/pre-commit` — checks branch name | Yes — `--no-verify` skips it (but layer 1 blocks that) |
| **GitHub branch protection** | Server-side — requires PR + CI checks | No — push to main is rejected server-side |

The PreToolUse hook is the primary guard for agents. Even if an agent tries `--no-verify`, the hook blocks it before git ever runs.

## Agent Protocol

### Subagents (Self-Managed Worktree Pattern)

Subagents no longer use `isolation: "worktree"`. Instead, every subagent creates its own worktree as the first action:

```bash
# Step 1: Create worktree (ALWAYS first action)
just lifecycle init <task-name>
# OR: bash scripts/tools/safe-worktree-init.sh <task-name>
# Output: WORKTREE_PATH=/absolute/path/to/worktree

# Step 2: All subsequent work uses the worktree path
cd /absolute/path/to/worktree && git branch --show-current  # Verify on feat/<task>
```

**Why self-managed?** When Claude Code tears down an `isolation: "worktree"` subagent, the platform switches the main repo's HEAD to the subagent's feature branch. The `SubagentStop` hook fires before teardown completes, so recovery can't reliably catch it. Subagents creating their own worktrees avoids this entirely — the main repo stays on `main`.

**Critical rules for subagents:**
1. **Run `lifecycle init` or `safe-worktree-init.sh` as the FIRST action** — before any file reads, edits, or git commands
2. **Use absolute paths for all operations** — `CLAUDE_PROJECT_DIR` points to main repo, not worktree
3. **Prefix every Bash command** with `cd /worktree/path &&` — shell `cd` does not persist between calls
4. **Never edit files in `CLAUDE_PROJECT_DIR`** — only work in the worktree

Subagent lifecycle (two-command pattern):
1. `just lifecycle init <task>` → capture `WORKTREE_PATH`
2. Verify: `cd $WORKTREE_PATH && git branch --show-current` → shows `feat/<task>`
3. TDD develop: write test (RED), implement (GREEN), commit each file immediately
4. `just lifecycle ship` → push + create PR + squash-merge + sync local main
5. Report back: Return a summary of changes made, files affected, and any downstream implications

**Subagents must NOT exit until step 4 is complete.**

### Main Agent Responsibilities

The main agent (on main) should:

1. **Never edit files directly** — dispatch subagents for code changes
2. **Pull latest main** before AND after dispatching: `git fetch origin && git pull --ff-only origin main`
3. **Verify subagent work** — check the PR diff
4. **Cleanup after merge**: `just task-complete <task>` (verifies merge, removes worktree/branch/tags, syncs main)
5. **Coordinate sequential tasks** — for multi-task plans, follow the Sequential Coordination protocol in `swarm-development` skill: dispatch one subagent at a time, update in-flight memories between dispatches, clean up memories after merge

### Hard Rules for All Agents

- **NEVER** commit on the `main` branch
- **NEVER** use `--no-verify` on git commit
- **NEVER** leave a branch unmerged
- **NEVER** exit without confirming merge and updating local main
- **NEVER** use `isolation: "worktree"` — subagents call `safe-worktree-init.sh` themselves
- **ALWAYS** provide a task name to subagents so they can create their own worktree

## Gotchas & Common Mistakes

### "BLOCKED: Cannot commit on main branch"

The Claude Code PreToolUse hook detected a `git commit` while on the `main` branch. Switch to a feature branch.

### "BLOCKED: --no-verify is not allowed"

Never use `--no-verify`. It's blocked by the PreToolUse hook. Fix the root cause (lint errors, parse errors, etc.) instead of bypassing it.

### "ERROR: Direct commits to main are blocked"

You're on the `main` branch. You need to be on a feature branch.

```bash
# If you haven't started work yet:
just worktree-create my-task

# If you already made changes on main (uncommitted):
git stash
just worktree-create my-task
cd ../<project>-my-task
git stash pop
```

### "ERROR: Direct push to main is blocked"

Push your feature branch instead:

```bash
git push -u origin feat/<task-name>
```

### Branching From Stale Main

If you manually create a branch with `git checkout -b` instead of `just worktree-create`, your branch may be based on a stale local main. Always fetch first:

```bash
git fetch origin main
git checkout -b feat/my-task origin/main
```

`just worktree-create` handles this automatically.

### Merge Conflicts

If `lifecycle ship` fails because the branch is behind main, rebase first:

```bash
git fetch origin main
git rebase origin/main
# Fix conflicts if any
git push --force-with-lease
gh pr merge <number> --squash
```

Or re-run `just lifecycle ship` — it auto-rebases if behind.

### Parallel PRs

When multiple PRs are in flight, merge them sequentially — `lifecycle ship` pushes and merges immediately without CI wait. Pre-rebase before shipping to avoid conflicts:

```bash
git fetch origin && git rebase origin/main
```

### Dirty Worktrees

If `just worktree-list` shows worktrees for merged PRs, clean them up:

```bash
just worktree-sync           # Batch cleanup: pull main, delete merged branches, prune
just worktree-cleanup <task> # Single cleanup
just worktree-cleanup-all    # Nuclear: prune all orphaned worktrees + gone branches
```

### Multiple PRs in Flight

The merge queue serializes merges — only one PR merges at a time. This prevents "it passed CI but breaks main" scenarios. PRs are processed in FIFO order by when the `ready-to-merge` label was applied (not creation date).

### Agent Worktrees vs Human Worktrees

| Source | Location | Branch Pattern | Cleanup |
|--------|----------|---------------|---------|
| `just worktree-create foo` | `../<project>-foo/` | `feat/foo` | `just worktree-cleanup foo` |
| `safe-worktree-init.sh foo` (subagent) | `../<project>-foo/` | `feat/foo` | `just worktree-cleanup foo` |

`just worktree-cleanup-all` prunes all orphaned worktrees.

## Emergency Procedures

### PR Not Merging

If `just lifecycle ship` or `gh pr merge` fails:

```bash
# Check PR state
gh pr view <number>

# Check for merge conflicts
gh pr view <number> --json mergeable -q .mergeable

# Rebase if behind main
git fetch origin main
git rebase origin/main
git push --force-with-lease
gh pr merge <number> --squash
```

### Failed Rebase (Unrecoverable Conflicts)

If rebase conflicts are too complex:

```bash
git rebase --abort
git reset --hard origin/feat/<task>   # Reset to remote state
# Create a new branch from fresh main and cherry-pick your commits
git fetch origin main
git checkout -b feat/<task>-v2 origin/main
git cherry-pick <commit1> <commit2> ...
git push -u origin feat/<task>-v2
# Close the old PR and create a new one
gh pr close <old-number>
gh pr create --base main
```

### Release / Hotfix Bypass

For releases and hotfixes that must commit directly to main:

```bash
ALLOW_MASTER_COMMIT=1 git commit -m "chore: release 1.0.0"
ALLOW_MASTER_PUSH=1 git push origin main
```

This is for repo admins only. The GitHub branch protection must allow admin bypass (enforce_admins off).

## Tag-Based Worktree Lifecycle

Every worktree has a lifecycle tracked by lightweight git tags pushed to origin:

| Tag | Meaning | Created when |
|-----|---------|-------------|
| `wt/active/<task>` | In progress — DO NOT delete | `just worktree-create <task>` |
| `wt/done/<task>` | Completed — safe to delete | `just worktree-mark-done <task>` (manual only — run after confirming PR merge) |

### How It Works

1. **`just worktree-create foo`** creates `wt/active/foo` tag and pushes it to origin
2. Agent develops, pushes, creates PR, CI passes, PR merges
3. **`just worktree-mark-done foo`** verifies PR merged, transitions `wt/active/foo` to `wt/done/foo` — this is the ONLY way to transition tags; it must be run manually after confirming PR merge
4. **`just worktree-cleanup foo`** or **`just worktree-sync`** deletes the worktree, branches, and tags

### Cleanup Safety

- `worktree-cleanup` **refuses** to delete a worktree with a `wt/active/*` tag
- Override with `FORCE=1 just worktree-cleanup <task>` for emergencies
- `worktree-sync` auto-cleans `wt/done/*` worktrees, skips `wt/active/*`
- `just task-complete <task>` handles all cleanup atomically after merge

### Abandoning a Worktree

If a PR was closed without merging (e.g., approach abandoned):

```bash
just worktree-mark-abandoned <task>   # Marks safe to delete without merged PR
just worktree-cleanup <task>          # Now succeeds
```

### Automatic Tag Transitions

- **Session start hook** cleans up `wt/done/*` worktrees automatically
- **Session start hook** warns about `wt/active/*` tags older than 48 hours
- **Tags are NOT auto-transitioned by hooks.** `just worktree-mark-done <task>` is the ONLY way to transition `wt/active` → `wt/done`, and must be run manually after confirming PR merge.

## Worktree Recipes Reference

| Recipe | Purpose |
|--------|---------|
| `just worktree-create <task>` | Create `feat/<task>` branch + worktree from `origin/main` + tag as active |
| `just worktree-mark-done <task>` | Transition `wt/active` to `wt/done` (requires merged PR) |
| `just worktree-mark-abandoned <task>` | Mark worktree as safe to delete (no merged PR required) |
| `just worktree-cleanup <task>` | Remove worktree + branches + tags (blocked if active, use FORCE=1 to override) |
| `just worktree-cleanup-all` | Prune orphaned worktrees + delete branches with gone remotes |
| `just worktree-list` | Show all worktrees with branch name, tag status, PR status, commits-behind-main |
| `just worktree-sync` | Pull main, auto-clean done worktrees, report active ones |

## Lifecycle Command Reference

| Command | Purpose |
|---------|---------|
| `just lifecycle init <task>` | Subagent: create worktree (delegates to safe-worktree-init.sh) |
| `just lifecycle ship` | Subagent: push + create PR + squash-merge + sync local main |
| `just task-complete <task>` | Main agent: verify merge, clean up worktree/branch/tags, sync main |
| `just assert-audit` | Run assert_audit.py on all test files |
| `just pr [title]` | Push + create PR (manual alternative to lifecycle ship) |
| `gh pr merge <number> --squash` | Merge a PR directly (no CI required) |
| `gh pr view --json state -q .state` | Check PR merge state |

## Infrastructure Files

| File | Role |
|------|------|
| `.githooks/pre-commit` | Main branch commit guard + lint + coverage ratchet + assert audit |
| `.githooks/pre-push` | Main branch push guard + module-gated test check (advisory if UNITY_PATH unset) |
| `scripts/tools/subagent-lifecycle.sh` | `init <task>` and `ship` — consolidated subagent lifecycle |
| `scripts/tools/task-complete.sh` | Main agent one-command post-merge cleanup |
| `scripts/tools/assert_audit.py` | Static assertion verifier for test methods |
| `scripts/tools/test_coverage_report.py` | Coverage baseline + `--check-modules` per-module ratchet |

> GitHub Actions workflows are disabled (`.github/workflows/*.yml.disabled`). All enforcement is local.
