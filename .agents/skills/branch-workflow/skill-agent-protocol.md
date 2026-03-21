# Agent Protocol

> Part of the `branch-workflow` skill. See [SKILL.md](SKILL.md) for the overview.

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

