#!/usr/bin/env bash
# task-complete.sh — Main agent cleanup after subagent merge
# Usage: task-complete.sh <task>
set -euo pipefail

TASK="${1:?task name required}"
BRANCH="feat/${TASK}"

# ── Resolve paths ──────────────────────────────────────────────────────────
MAIN_REPO="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel)}"
PROJECT_NAME=$(basename "$MAIN_REPO")
WORKTREE_DIR="$(dirname "$MAIN_REPO")/${PROJECT_NAME}-${TASK}"

echo "task-complete: task=${TASK}"
echo "task-complete: branch=${BRANCH}"
echo "task-complete: worktree=${WORKTREE_DIR}"

# ── Verify PR is merged ────────────────────────────────────────────────────
echo "task-complete: verifying merged PR..."
MERGED_PR=$(gh pr list --head "$BRANCH" --state merged --json number --limit 1 \
  --jq '.[0].number // empty' 2>/dev/null || echo "")

if [ -z "$MERGED_PR" ]; then
  echo "ERROR: No merged PR found for branch '${BRANCH}'." >&2
  echo "  Ensure the PR was merged before running task-complete." >&2
  exit 1
fi
echo "task-complete: confirmed PR #${MERGED_PR} merged"

# ── Delete remote branch ───────────────────────────────────────────────────
echo "task-complete: deleting remote branch..."
git push origin --delete "$BRANCH" 2>/dev/null || true

# ── Remove worktree ────────────────────────────────────────────────────────
echo "task-complete: removing worktree..."
git worktree remove "$WORKTREE_DIR" --force 2>/dev/null || true

# ── Delete local branch ────────────────────────────────────────────────────
echo "task-complete: deleting local branch..."
git branch -D "$BRANCH" 2>/dev/null || true

# ── Delete lifecycle tags (local + remote) ─────────────────────────────────
echo "task-complete: cleaning up lifecycle tags..."
git tag -d "wt/active/${TASK}" 2>/dev/null || true
git push origin --delete "wt/active/${TASK}" 2>/dev/null || true
git tag -d "wt/done/${TASK}" 2>/dev/null || true
git push origin --delete "wt/done/${TASK}" 2>/dev/null || true

# ── Prune and sync main ────────────────────────────────────────────────────
echo "task-complete: pruning worktrees and syncing main..."
git worktree prune
git fetch origin main
git update-ref refs/heads/main refs/remotes/origin/main

MAIN_SHA=$(git rev-parse --short main)
echo "Task ${TASK} complete. main at ${MAIN_SHA}."
