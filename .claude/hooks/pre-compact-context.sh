#!/usr/bin/env bash
# PreCompact hook: capture key context before conversation compaction.
cd "$CLAUDE_PROJECT_DIR" || exit 0

BRANCH=$(git branch --show-current 2>/dev/null || echo "unknown")
DIRTY=$(git status --porcelain 2>/dev/null | wc -l | tr -d ' ')
COMMIT_COUNT="0"
if [ "$BRANCH" != "main" ] && [ "$BRANCH" != "unknown" ]; then
  COMMIT_COUNT=$(git rev-list --count origin/main..HEAD 2>/dev/null || echo "?")
fi

echo "=== CONTEXT SNAPSHOT ==="
echo "Branch: $BRANCH ($COMMIT_COUNT commits ahead of main)"
if [ "$DIRTY" -gt 0 ]; then
  echo "Uncommitted: $DIRTY file(s)"
else
  echo "Working tree: clean"
fi

# PR status (one line)
if command -v gh &>/dev/null && [ "$BRANCH" != "main" ] && [ "$BRANCH" != "unknown" ]; then
  PR_INFO=$(gh pr list --head "$BRANCH" --state all --json number,state --limit 1 -q '.[0] | "#\(.number) \(.state)"' 2>/dev/null || true)
  [ -n "$PR_INFO" ] && echo "PR: $PR_INFO"
fi

echo ""
echo "Context compacting — commit progress if needed, or start fresh session for complex remaining work."
echo "=== END ==="
exit 0
