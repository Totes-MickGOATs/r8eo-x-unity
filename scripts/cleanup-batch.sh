#!/usr/bin/env bash
# Remove an integration branch after the batch PR has merged.
#
# Usage: scripts/cleanup-batch.sh <batch-name>
set -euo pipefail

BATCH_NAME="${1:?Usage: cleanup-batch.sh <batch-name>}"
BRANCH="integrate/${BATCH_NAME}"

# Verify the batch PR is merged
MERGED_PR=$(gh pr list --head "$BRANCH" --state merged --json number -q '.[0].number' 2>/dev/null || echo "")
if [ -z "$MERGED_PR" ]; then
  echo "ERROR: No merged PR found for branch $BRANCH." >&2
  echo "The batch PR must be merged before cleanup." >&2
  exit 1
fi

echo "Batch PR #${MERGED_PR} is merged. Cleaning up..."

# Delete local branch
if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
  git branch -D "$BRANCH" 2>/dev/null || true
  echo "Deleted local branch: $BRANCH"
fi

# Delete remote branch (if not already deleted by GitHub)
if git ls-remote --exit-code --heads origin "$BRANCH" >/dev/null 2>&1; then
  git push origin --delete "$BRANCH" 2>/dev/null || true
  echo "Deleted remote branch: $BRANCH"
else
  echo "Remote branch already deleted (GitHub auto-delete)."
fi

# Prune
git fetch origin --prune --quiet

echo "--- Batch cleanup complete for $BATCH_NAME ---"
