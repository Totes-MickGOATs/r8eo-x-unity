#!/usr/bin/env bash
# worktree-audit.sh — Detect ghost wt/active/* tags with no matching worktree
set -euo pipefail

MAIN_REPO="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}"
ghosts=0

echo "Auditing worktree tags..."
while IFS= read -r tag; do
  task="${tag#wt/active/}"
  branch="feat/${task}"
  if ! git -C "$MAIN_REPO" show-ref --verify --quiet "refs/heads/$branch" 2>/dev/null; then
    echo "GHOST: $tag (no local branch $branch)"
    echo "  fix: git tag -d $tag && git push origin --delete $tag"
    ghosts=$((ghosts + 1))
  fi
done < <(git -C "$MAIN_REPO" tag -l 'wt/active/*')

if (( ghosts == 0 )); then
  echo "No ghost tags found."
else
  echo ""
  echo "$ghosts ghost tag(s) found. Run the suggested commands to clean up."
fi
