#!/usr/bin/env bash
# Mark a batch integration PR as ready, handle BEHIND state, enable auto-merge.
#
# Usage: scripts/finalize-batch.sh <pr-number>
set -euo pipefail

PR_NUMBER="${1:?Usage: finalize-batch.sh <pr-number>}"

echo "Finalizing batch PR #${PR_NUMBER}..."

# Get PR details
PR_JSON=$(gh pr view "$PR_NUMBER" --json headRefName,baseRefName,state,isDraft)
HEAD_REF=$(echo "$PR_JSON" | python3 -c "import sys,json; print(json.load(sys.stdin)['headRefName'])")
BASE_REF=$(echo "$PR_JSON" | python3 -c "import sys,json; print(json.load(sys.stdin)['baseRefName'])")
IS_DRAFT=$(echo "$PR_JSON" | python3 -c "import sys,json; print(json.load(sys.stdin)['isDraft'])")
STATE=$(echo "$PR_JSON" | python3 -c "import sys,json; print(json.load(sys.stdin)['state'])")

if [ "$STATE" != "OPEN" ]; then
  echo "ERROR: PR #${PR_NUMBER} is $STATE, not OPEN." >&2
  exit 1
fi

# Ensure branch is up to date with base
git fetch origin "$BASE_REF" --quiet
git fetch origin "$HEAD_REF" --quiet

if ! git merge-base --is-ancestor "origin/$BASE_REF" "origin/$HEAD_REF" 2>/dev/null; then
  echo "PR is behind $BASE_REF. Updating branch..."
  gh api "repos/{owner}/{repo}/pulls/${PR_NUMBER}/update-branch" \
    --method PUT \
    --field expected_head_sha="$(git rev-parse "origin/$HEAD_REF")" \
    2>/dev/null || {
      echo "WARNING: Could not auto-update branch. May need manual merge."
    }
  # Wait for branch update
  sleep 5
fi

# Mark as ready for review (if draft)
if [ "$IS_DRAFT" = "True" ]; then
  gh pr ready "$PR_NUMBER"
  echo "Marked PR #${PR_NUMBER} as ready for review."
fi

# Enable auto-merge with merge commit (not squash — preserve sub-PR history)
gh pr merge "$PR_NUMBER" --auto --merge
echo "Auto-merge (merge commit) enabled for PR #${PR_NUMBER}."

echo "--- Batch PR #${PR_NUMBER} finalized ---"
echo "Waiting for CI to pass before auto-merge completes."
echo "Monitor: gh pr view $PR_NUMBER --json state,mergeStateStatus"
