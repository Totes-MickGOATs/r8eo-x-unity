#!/usr/bin/env bash
# Remove a worktree and its local branch. Safety-checks for open PRs.
#
# Usage: scripts/cleanup-worktree.sh <branch-name> [--force]
set -euo pipefail

BRANCH="${1:?Usage: cleanup-worktree.sh <branch-name> [--force]}"
FORCE="${2:-}"

# Safety check: abort if there's an open PR for this branch
if [ "$FORCE" != "--force" ] && command -v gh >/dev/null 2>&1; then
  OPEN_PR=$(gh pr list --head "$BRANCH" --state open --json number -q '.[0].number' 2>/dev/null || echo "")
  if [ -n "$OPEN_PR" ]; then
    echo "ERROR: Branch $BRANCH has an open PR (#${OPEN_PR})." >&2
    echo "Close or merge the PR first, or use --force to override." >&2
    exit 1
  fi
fi

# Find and remove worktree
WORKTREE_PATH=$(git worktree list --porcelain | grep -B1 "branch refs/heads/$BRANCH" | head -1 | sed 's/^worktree //' || true)

if [ -n "$WORKTREE_PATH" ] && [ -d "$WORKTREE_PATH" ]; then
  git worktree remove "$WORKTREE_PATH" --force 2>/dev/null || true
  echo "Removed worktree: $WORKTREE_PATH"
else
  echo "No worktree found for branch $BRANCH"
fi

# Delete local branch
if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
  git branch -D "$BRANCH" 2>/dev/null || true
  echo "Deleted local branch: $BRANCH"
fi

# Prune stale worktree entries
git worktree prune

echo "--- Cleanup complete for $BRANCH ---"
