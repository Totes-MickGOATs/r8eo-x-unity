#!/bin/bash
# Subagent exit handler — performs safety recovery, auto-commit of stray changes,
# push verification, and reminds agent to complete Definition of Done.
# NOT a CI quality gate. Enforcement of TDD and clean-loop criteria is agent-discipline only.

cd "$CLAUDE_PROJECT_DIR" || exit 0

BRANCH=$(git branch --show-current 2>/dev/null)
[[ -z "$BRANCH" ]] && exit 0

# Only check on feature branches (not main) — if we're on main, nothing to gate
[[ "$BRANCH" == "main" ]] && exit 0

# Auto-commit any uncommitted tracked files to prevent lost work
DIRTY_TRACKED=$(git diff --name-only 2>/dev/null)
STAGED=$(git diff --cached --name-only 2>/dev/null)
if [ -n "$DIRTY_TRACKED" ] || [ -n "$STAGED" ]; then
    git add -u 2>/dev/null || true
    git commit -m "chore: stage uncommitted work" 2>/dev/null || true
fi

# Extract task name from branch (feat/<task> -> <task>)
TASK=$(echo "$BRANCH" | sed 's|^feat/||')

WARNINGS=""

# Check for uncommitted changes (untracked files may remain)
DIRTY=$(git status --porcelain 2>/dev/null | head -5)
if [ -n "$DIRTY" ]; then
    DIRTY_COUNT=$(git status --porcelain 2>/dev/null | wc -l)
    WARNINGS="${WARNINGS}UNCOMMITTED CHANGES: ${DIRTY_COUNT} file(s) not committed:\n"
    WARNINGS="${WARNINGS}$(echo "$DIRTY" | sed 's/^/  /')\n\n"
fi

# Check if branch has been pushed
REMOTE_REF=$(git ls-remote --heads origin "$BRANCH" 2>/dev/null | head -1)
if [ -z "$REMOTE_REF" ]; then
    WARNINGS="${WARNINGS}BRANCH NOT PUSHED: '$BRANCH' has not been pushed to origin.\n"
    WARNINGS="${WARNINGS}  Run: git push -u origin $BRANCH\n\n"
fi

# Sync local main to origin/main if the PR is already merged
# (post-merge sync only — use 'just ff-main' mid-development before the PR)
if [ -n "$REMOTE_REF" ]; then
    MERGED_CHECK=$(gh pr list --head "$BRANCH" --state merged --json number --limit 1 2>/dev/null || echo "[]")
    MERGED_CHECK_NUM=$(echo "$MERGED_CHECK" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['number'] if d else '')" 2>/dev/null || echo "")
    if [ -n "$MERGED_CHECK_NUM" ]; then
        git fetch origin main --quiet 2>/dev/null || true
        git update-ref refs/heads/main origin/main 2>/dev/null || true
        echo "post-merge: local main synced to origin/main (PR #$MERGED_CHECK_NUM merged)"
    fi
fi

# Check if PR exists for this branch
if command -v gh &>/dev/null && [ -n "$REMOTE_REF" ]; then
    PR_JSON=$(gh pr list --head "$BRANCH" --state open --json number,title --limit 1 2>/dev/null || echo "[]")
    PR_NUM=$(echo "$PR_JSON" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['number'] if d else '')" 2>/dev/null || echo "")
    if [ -z "$PR_NUM" ]; then
        # Check if merged already
        MERGED_JSON=$(gh pr list --head "$BRANCH" --state merged --json number --limit 1 2>/dev/null || echo "[]")
        MERGED_NUM=$(echo "$MERGED_JSON" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['number'] if d else '')" 2>/dev/null || echo "")
        if [ -z "$MERGED_NUM" ]; then
            WARNINGS="${WARNINGS}NO PR CREATED: Branch '$BRANCH' is pushed but has no pull request.\n"
            WARNINGS="${WARNINGS}  Run: gh pr create --base main\n\n"
        fi
    fi
fi

if [ -n "$WARNINGS" ]; then
    echo ""
    echo "╔══════════════════════════════════════╗"
    echo "║   SUBAGENT EXIT — ACTION REQUIRED    ║"
    echo "╚══════════════════════════════════════╝"
    echo ""
    echo -e "$WARNINGS"
    echo "Definition of Done: PR open + CI green + ready-to-merge label applied."
fi

# Clean-loop reminder
echo ""
echo "REMINDER: Run /dev:clean-loop before declaring done."
echo "  → Captures lessons learned, updates docs, checks memory, verifies clean state."

# CI monitoring reminder
echo "REMINDER: Watch CI through merge before exiting."
echo "  → gh run watch && gh pr view --json state -q .state"
echo "  → Then: git fetch origin main && git update-ref refs/heads/main origin/main"

# Never block — always exit 0
exit 0
