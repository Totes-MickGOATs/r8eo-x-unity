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

# Check local verification queue status for this branch
QUEUE_FILE="${CLAUDE_PROJECT_DIR}/Logs/automation/queue.json"
QUEUE_STATUS="not submitted"
if [ -f "$QUEUE_FILE" ] && command -v python3 >/dev/null 2>&1; then
    QUEUE_STATUS=$(python3 -c "
import json, sys
branch = sys.argv[1]
try:
    with open(sys.argv[2]) as f:
        data = json.load(f)
    result = data.get('results', {}).get(branch, {}).get('status', '')
    queue = [e for e in data.get('queue', []) if e.get('branch') == branch]
    if result:
        print(result)
    elif queue:
        print(queue[0].get('status', 'queued'))
    else:
        print('not submitted')
except Exception:
    print('unknown')
" "$BRANCH" "$QUEUE_FILE" 2>/dev/null || echo "unknown")
fi

if [ "$QUEUE_STATUS" = "passed" ]; then
    # Branch passed — check if main was promoted
    if git merge-base --is-ancestor "$BRANCH" main 2>/dev/null; then
        echo "queue: branch '${BRANCH}' passed and is already in local main"
    else
        WARNINGS="${WARNINGS}QUEUE PASSED — PROMOTE PENDING: Branch '${BRANCH}' passed verification but local main not yet updated.\n"
        WARNINGS="${WARNINGS}  Run: just queue-promote ${BRANCH}\n\n"
    fi
elif [ "$QUEUE_STATUS" = "failed" ]; then
    WARNINGS="${WARNINGS}QUEUE FAILED: Branch '${BRANCH}' failed verification.\n"
    WARNINGS="${WARNINGS}  Check: ${QUEUE_FILE}\n"
    WARNINGS="${WARNINGS}  Results: ${CLAUDE_PROJECT_DIR}/Logs/automation/results/\n\n"
elif [ "$QUEUE_STATUS" = "running" ]; then
    echo "queue: verification currently running for '${BRANCH}'"
elif [ "$QUEUE_STATUS" = "not submitted" ] || [ "$QUEUE_STATUS" = "unknown" ]; then
    WARNINGS="${WARNINGS}NOT SUBMITTED TO QUEUE: Branch '${BRANCH}' has not been submitted for verification.\n"
    WARNINGS="${WARNINGS}  Run: just queue-submit ${BRANCH}\n"
    WARNINGS="${WARNINGS}  Then: just queue-run\n\n"
fi

if [ -n "$WARNINGS" ]; then
    echo ""
    echo "╔══════════════════════════════════════╗"
    echo "║   SUBAGENT EXIT — ACTION REQUIRED    ║"
    echo "╚══════════════════════════════════════╝"
    echo ""
    echo -e "$WARNINGS"
    echo "Definition of Done: branch committed + queue passed + local main promoted."
fi

# Clean-loop reminder
echo ""
echo "REMINDER: Run /dev:clean-loop before declaring done."
echo "  → Captures lessons learned, updates docs, checks memory, verifies clean state."

echo "REMINDER: Verify queue status before exiting."
echo "  → just queue-status"
echo "  → just queue-promote ${BRANCH}   (if passed)"

# Never block — always exit 0
exit 0
