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

### 3. Push and Create a PR

```bash
git push -u origin feat/<task-name>
gh pr create --base main --title "feat: description" --body "..."
```

Or use the one-liner:

```bash
just pr "feat: description"    # push + create PR in one command
```

### 4. Local Fast-Forward (Mid-Development Speed Optimization)

After a successful push (pre-push tests passed), fast-forward local main to the current branch:

```bash
just ff-main
```

**Why:** Gives the next subagent immediate access to your changes on local main, without waiting for the remote PR/CI/merge cycle. The remote PR flow remains the source of truth.

**When to use:** Mid-development, BEFORE the PR merges — to make your in-progress changes visible to the next sequential task.

**Safety:** The recipe enforces:
- Must be on a feature branch (not main)
- Branch must be pushed to origin (tests passed)
- Main must be ancestor of HEAD (fast-forward only, no force)

**After remote PR merges:** Use `git update-ref` to sync local main to the squash commit — do NOT use `just ff-main` post-merge:
```bash
git fetch origin main && git update-ref refs/heads/main origin/main
```
This replaces the individual commits with the single squash commit from the PR merge. `just ff-main` is for mid-development only; post-merge sync requires fetching from origin.

### 5. Wait for CI

Required checks must pass before the PR can merge:
- **Lint & Preflight** — format checks, lint, registry validation

If CI fails, fix the issue in the feature branch, push again. CI re-runs automatically.

### 6. Auto-Merge (Event-Driven)

CI automatically adds the `ready-to-merge` label when checks pass. The **auto-merge workflow** (`auto-merge.yml`) triggers immediately on label addition:
- If the PR is behind main, it rebases automatically, CI re-runs, label re-added on pass, merge re-triggered
- If CI is green and up-to-date, it squash-merges and deletes the remote branch
- If CI failed, it removes the label and comments on the PR

**No manual labeling needed in the normal flow.** CI handles it. If you need to manually re-add the label (e.g., after fixing a CI failure):

```bash
gh pr edit <number> --add-label "ready-to-merge"
```

**Safety net:** When new commits are pushed to a PR, the `pr-guard.yml` workflow immediately strips the `ready-to-merge` label before CI starts, ensuring the PR must pass CI again.

### 7. Cleanup

```bash
just worktree-cleanup <task-name>   # Remove worktree + local/remote branch
# OR
just worktree-sync                  # Batch: pull main, delete merged branches, prune
```

## Definition of Done

A task is **not done** until ALL of the following are true. Agents must self-monitor and self-correct until every item is satisfied.

| # | Criterion | How to Verify |
|---|-----------|---------------|
| 1 | PR is open with all commits pushed | `gh pr view` shows your PR |
| 2 | CI is green | `gh run watch` completed successfully |
| 3 | `ready-to-merge` label applied | `gh pr view` shows the label |
| 4 | PR merged | `gh pr view --json state -q .state` returns `MERGED` |
| 5 | Local main updated | `git fetch origin main && git update-ref refs/heads/main origin/main` |
| 6 | No other agent's PR broken by your changes | Merge queue handles this — if it removes your label, you fix the conflict/failure |
| 7 | Knowledge synced | CLAUDE.md files, manifests updated if applicable |

### Owning Your CI

**You are responsible for your branch's CI from push to merge.** This means:

1. After every push, check CI status: `gh run list --branch feat/<task> --limit 3`
2. If CI fails, read the failure output: `gh run view <run-id> --log-failed`
3. Diagnose and fix the issue in your feature branch
4. Push the fix and wait for CI to go green
5. Only apply `ready-to-merge` after CI is green

**Do not:**
- Walk away from a red CI
- Assume "it works locally so CI must be wrong"
- Apply `ready-to-merge` before CI is green
- Leave your branch unmerged at the end of a task

### Self-Reflection on Failure

When CI fails or the merge queue rejects your PR:

1. **Read the actual error** — don't guess. Use `gh run view --log-failed`.
2. **Identify root cause** — is it your code, a flaky test, or a conflict with another PR?
3. **Fix and push** — don't just retry. Address the underlying issue.
4. **Update memory if needed** — if you discovered a new gotcha or pattern, record it so future agents don't hit the same issue.
5. **Re-apply the label** only after CI is green again.

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
bash scripts/tools/safe-worktree-init.sh <task-name>
# Output: WORKTREE_PATH=/absolute/path/to/worktree

# Step 2: All subsequent work uses the worktree path
cd /absolute/path/to/worktree && git branch --show-current  # Verify on feat/<task>
```

**Why self-managed?** When Claude Code tears down an `isolation: "worktree"` subagent, the platform switches the main repo's HEAD to the subagent's feature branch. The `SubagentStop` hook fires before teardown completes, so recovery can't reliably catch it. Subagents creating their own worktrees avoids this entirely — the main repo stays on `main`.

**Critical rules for subagents:**
1. **Run `safe-worktree-init.sh` as the FIRST action** — before any file reads, edits, or git commands
2. **Use absolute paths for all operations** — `CLAUDE_PROJECT_DIR` points to main repo, not worktree
3. **Prefix every Bash command** with `cd /worktree/path &&` — shell `cd` does not persist between calls
4. **Never edit files in `CLAUDE_PROJECT_DIR`** — only work in the worktree

Subagent lifecycle:
1. Run `bash scripts/tools/safe-worktree-init.sh <task>` → capture `WORKTREE_PATH`
2. Verify: `cd $WORKTREE_PATH && git branch --show-current` → shows `feat/<task>`
3. Develop: Write code, run tests, commit (all in worktree)
4. Push: `cd $WORKTREE_PATH && git push -u origin feat/<task>`
5. Create PR: `gh pr create --base main`
6. Enable auto-merge: `gh pr merge --auto --squash`
7. Watch CI: `gh run watch` — wait for CI to complete, fix if it fails
8. Confirm merge: Poll `gh pr view --json state -q .state` until it returns `MERGED`
9. Update local main: `git -C $WORKTREE_PATH fetch origin main && git -C $WORKTREE_PATH update-ref refs/heads/main origin/main`
10. Report back: Return a summary of changes made, files affected, and any downstream implications

**Subagents must NOT exit until step 9 is complete.**

### Main Agent Responsibilities

The main agent (on main) should:

1. **Never edit files directly** — dispatch subagents for code changes
2. **Pull latest main** before AND after dispatching: `git fetch origin && git pull --ff-only origin main`
3. **Verify subagent work** — check the PR diff, not just CI status
4. **Cleanup after merge**: `just worktree-cleanup <task>` or `just worktree-sync`
5. **Coordinate sequential tasks** — for multi-task plans, follow the Sequential Coordination protocol in `swarm-development` skill: dispatch one subagent at a time, update in-flight memories between dispatches, clean up memories after merge

### Hard Rules for All Agents

- **NEVER** commit on the `main` branch
- **NEVER** use `--no-verify` on git commit
- **NEVER** leave a branch unmerged or CI failing
- **NEVER** exit without watching CI through merge and updating local main
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

### CI Fails After Rebase

The merge queue auto-rebases PRs that are behind main. If CI fails after rebase, the `ready-to-merge` label is removed and a comment is posted. Fix the issue, push, and re-add the label.

### Merge Conflicts

If the merge queue can't rebase your PR (conflict), it removes the label and comments. Resolve manually:

```bash
git fetch origin main
git rebase origin/main
# Fix conflicts
git push --force-with-lease
gh pr edit <number> --add-label "ready-to-merge"
```

### Parallel PR Rebase Cascade

When multiple PRs are in flight and one merges, the remaining PRs are now behind main. The merge queue handles this by auto-rebasing, but each rebase triggers a new CI run. With N parallel PRs, expect O(N^2) CI runs total.

**Mitigation strategies:**
- Limit parallel PRs to 2-3 when possible
- Sequence PRs that touch the same files (especially singletons like config files or CI workflows)
- Pre-rebase before pushing: `git fetch origin && git rebase origin/main`

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

### Stuck Merge Queue

If a PR has the `ready-to-merge` label but hasn't merged after 10+ minutes:

```bash
# Check if CI is still running
gh run list --branch <branch> --limit 3

# Check PR state
gh pr view <number>

# If CI passed but merge didn't trigger, re-apply label
gh pr edit <number> --remove-label "ready-to-merge"
gh pr edit <number> --add-label "ready-to-merge"

# If the auto-merge workflow itself failed, check its runs
gh run list --workflow auto-merge.yml --limit 5
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

### CI Outage (GitHub Actions Down)

If GitHub Actions is experiencing an outage:

1. Check status: https://www.githubstatus.com/
2. Do NOT force-merge PRs without CI — wait for the outage to resolve
3. Continue developing and pushing — CI will run when Actions recovers
4. If urgent, run tests locally to verify, but still wait for CI before merging

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
- GitHub CI workflows also check tags before deleting branches

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

## CI Command Reference

| Command | Purpose |
|---------|---------|
| `just watch-ci` | Watch latest CI run for current branch until complete |
| `just ci-log` | Show failed CI log for current branch |
| `just ship [title]` | Push + create PR + watch CI (all-in-one) |
| `just pr [title]` | Push + create PR |
| `gh run list --branch <branch>` | List CI runs for a branch |
| `gh run view <run-id> --log-failed` | View failed CI output |
| `gh run watch <run-id>` | Watch a running CI job |

## Infrastructure Files

| File | Role |
|------|------|
| `.githooks/pre-commit` | Main branch commit guard (+ lint, format checks) |
| `.githooks/pre-push` | Main branch push guard |
| `.github/workflows/auto-merge.yml` | Event-driven merge queue — serialized squash-merge |
| `.github/workflows/ci.yml` | CI pipeline — lint + tests + auto-label |
| `.github/workflows/pr-guard.yml` | Strip ready-to-merge label on new push |
