#!/bin/bash
# SubagentStop hook: recover from worktree teardown contamination, then verify
# subagent pushed and created PR before declaring done.

cd "$CLAUDE_PROJECT_DIR" || exit 0

BRANCH=$(git branch --show-current 2>/dev/null)
[[ -z "$BRANCH" ]] && exit 0

# --- Worktree teardown contamination recovery ---
# When isolation:"worktree" tears down, the Claude Code platform can switch
# the main repo's HEAD to the subagent's feature branch. Detect and recover
# IMMEDIATELY so the main agent never sees stale branch state.
if [[ "$BRANCH" != "main" ]]; then
    MAIN_WORKTREE=$(git worktree list --porcelain 2>/dev/null | head -1 | sed 's/^worktree //')
    NORM_PROJECT=$(cd "$CLAUDE_PROJECT_DIR" 2>/dev/null && pwd -W 2>/dev/null || pwd)
    NORM_WORKTREE=$(cd "$MAIN_WORKTREE" 2>/dev/null && pwd -W 2>/dev/null || echo "$MAIN_WORKTREE")
    if [[ "$NORM_WORKTREE" == "$NORM_PROJECT" ]]; then
        echo ""
        echo "WORKTREE RECOVERY: Main repo was on '$BRANCH' — restoring to main..."
        git checkout -f main 2>/dev/null
        git restore --staged . 2>/dev/null
        git checkout -- . 2>/dev/null
        git clean -fd 2>/dev/null
        BRANCH=$(git branch --show-current 2>/dev/null)
        if [[ "$BRANCH" == "main" ]]; then
            echo "WORKTREE RECOVERY: Restored to main, clean state."
        else
            echo "WORKTREE RECOVERY: FAILED — still on '$BRANCH'. Manual fix needed."
        fi
        exit 0
    fi
fi

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

# CI monitoring reminder
echo "REMINDER: Watch CI through merge before exiting."
echo "  → gh run watch && gh pr view --json state -q .state"
echo "  → Then: git fetch origin main && git update-ref refs/heads/main origin/main"

# Never block — always exit 0
exit 0
