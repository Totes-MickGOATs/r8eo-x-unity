#!/usr/bin/env bash
# Create an integration branch + draft PR for batching multiple sub-PRs.
#
# Usage: scripts/create-integration-branch.sh <batch-name> [--title "PR title"]
#
# Output: JSON with branch name, PR number, and PR URL.
set -euo pipefail

BATCH_NAME="${1:?Usage: create-integration-branch.sh <batch-name> [--title \"title\"]}"
shift

PR_TITLE=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --title) PR_TITLE="$2"; shift 2 ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

BRANCH="integrate/${BATCH_NAME}"

if [ -z "$PR_TITLE" ]; then
  PR_TITLE="integrate: ${BATCH_NAME}"
fi

# Ensure we're up to date
git fetch origin main --quiet

# Create integration branch from main
if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
  echo "Branch $BRANCH already exists locally." >&2
  git checkout "$BRANCH"
else
  git checkout -b "$BRANCH" origin/main
fi

# Push branch
git push -u origin "$BRANCH"

# Create draft PR
PR_URL=$(gh pr create \
  --base main \
  --head "$BRANCH" \
  --title "$PR_TITLE" \
  --draft \
  --body "$(cat <<'EOF'
## Batch Integration PR

This PR collects multiple sub-PRs that target `integrate/` branch.
**Do not squash-merge** — use merge commit to preserve sub-PR history.

### Sub-PRs
_Sub-PRs will be listed here as they are opened._

---
_Created by `scripts/create-integration-branch.sh`_
EOF
)")

PR_NUMBER=$(gh pr view "$BRANCH" --json number -q '.number')

# Return to previous branch
git checkout - 2>/dev/null || true

# Output JSON
cat <<RESULT
{
  "branch": "$BRANCH",
  "pr_number": $PR_NUMBER,
  "pr_url": "$PR_URL"
}
RESULT
