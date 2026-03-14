#!/bin/bash
# SubagentStop hook: verify subagent pushed and created PR before declaring done.
# This catches the "I'm done" claim without follow-through.

cd "$CLAUDE_PROJECT_DIR" || exit 0

BRANCH=$(git branch --show-current 2>/dev/null)

# Only check on feature branches (not main)
[[ "$BRANCH" == "main" ]] && exit 0
[[ -z "$BRANCH" ]] && exit 0

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

# Auto-transition tags if PR is merged
if command -v gh &>/dev/null; then
    MERGED_JSON=$(gh pr list --head "$BRANCH" --state merged --json number --limit 1 2>/dev/null || echo "[]")
    MERGED_NUM=$(echo "$MERGED_JSON" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['number'] if d else '')" 2>/dev/null || echo "")
    if [ -n "$MERGED_NUM" ]; then
        # PR is merged — transition active->done
        git tag -d "wt/active/$TASK" 2>/dev/null || true
        git push origin --delete "wt/active/$TASK" 2>/dev/null || true
        git tag -f "wt/done/$TASK" HEAD 2>/dev/null || true
        git push origin "wt/done/$TASK" --force 2>/dev/null || true
        echo "Auto-transitioned: wt/active/$TASK -> wt/done/$TASK (PR #$MERGED_NUM merged)"
    fi
fi

if [ -n "$WARNINGS" ]; then
    echo ""
    echo "╔══════════════════════════════════════╗"
    echo "║   SUBAGENT QUALITY GATE WARNING      ║"
    echo "╚══════════════════════════════════════╝"
    echo ""
    echo -e "$WARNINGS"
    echo "Definition of Done: PR open + CI green + ready-to-merge label applied."
fi

# Clean-loop reminder
echo ""
echo "REMINDER: Run /dev:clean-loop before declaring done."
echo "  → Captures lessons learned, updates docs, checks memory, verifies clean state."

# Never block — always exit 0
exit 0
