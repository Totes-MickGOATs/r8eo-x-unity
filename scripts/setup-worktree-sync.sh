#!/usr/bin/env bash
# Write the xenon-base-branch marker file so the post-commit hook
# knows which branch to sync against.
#
# Usage: scripts/setup-worktree-sync.sh [base-branch]
#   base-branch defaults to the upstream tracking branch or "main".
set -euo pipefail

BASE="${1:-}"

if [ -z "$BASE" ]; then
  # Try to detect from upstream tracking
  BASE=$(git rev-parse --abbrev-ref '@{upstream}' 2>/dev/null | sed 's|^origin/||' || true)
fi

if [ -z "$BASE" ]; then
  BASE="main"
fi

GIT_DIR=$(git rev-parse --git-dir)
echo "$BASE" > "$GIT_DIR/.xenon-base-branch"
echo "Worktree sync configured: base branch = $BASE"
echo "The post-commit hook will now auto-merge origin/$BASE after each commit."
