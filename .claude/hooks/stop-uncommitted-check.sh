#!/bin/bash
# Stop hook: warn about uncommitted changes and detect worktree branch contamination.
# Catches "forgot to commit" AND "subagent worktree teardown switched main repo off main".

cd "$CLAUDE_PROJECT_DIR" || exit 0

BRANCH=$(git branch --show-current 2>/dev/null)
[[ -z "$BRANCH" ]] && exit 0

# --- Safety net: detect worktree branch contamination ---
# If the main repo is NOT on main, a subagent's worktree teardown likely switched us.
# Auto-recover: checkout main, discard contamination, warn loudly.
if [[ "$BRANCH" != "main" ]]; then
    # Check if this is the primary working directory (not a worktree itself)
    MAIN_WORKTREE=$(git worktree list --porcelain 2>/dev/null | head -1 | sed 's/^worktree //')
    if [[ "$MAIN_WORKTREE" == "$CLAUDE_PROJECT_DIR" || "$MAIN_WORKTREE" == "$(pwd)" ]]; then
        echo ""
        echo "WARNING: Main repo is on '$BRANCH' instead of 'main'!"
        echo "  This is likely worktree teardown contamination from a subagent."
        echo "  Auto-recovering: checkout main, discard foreign changes..."
        git checkout -f main 2>/dev/null
        git restore --staged . 2>/dev/null
        git checkout -- . 2>/dev/null
        git clean -fd 2>/dev/null
        BRANCH=$(git branch --show-current 2>/dev/null)
        if [[ "$BRANCH" == "main" ]]; then
            echo "  Recovered: now on main, clean state."
        else
            echo "  ERROR: Auto-recovery failed. Still on '$BRANCH'. Manual fix needed:"
            echo "    git checkout -f main && git clean -fd && git checkout -- ."
        fi
        exit 0
    fi
fi

[[ "$BRANCH" == "main" ]] && exit 0

# --- Uncommitted changes check (only on feature branches) ---
# Only warn if there are uncommitted code files (ignore untracked noise)
# Engine-specific extensions can be added here
DIRTY_CODE=$(git diff --name-only HEAD 2>/dev/null | grep -E '\.(gd|py|cs|cpp|h|tscn|tres)$' | head -5)
if [ -n "$DIRTY_CODE" ]; then
    COUNT=$(git diff --name-only HEAD 2>/dev/null | grep -cE '\.(gd|py|cs|cpp|h|tscn|tres)$')
    echo ""
    echo "NOTE: $COUNT uncommitted code file(s) on $BRANCH:"
    echo "$DIRTY_CODE" | sed 's/^/  /'
    [ "$COUNT" -gt 5 ] && echo "  ... ($((COUNT - 5)) more)"
fi

exit 0
