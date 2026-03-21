# Step-by-Step: Feature or Fix

> Part of the `branch-workflow` skill. See [SKILL.md](SKILL.md) for the overview.

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

