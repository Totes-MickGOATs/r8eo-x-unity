#!/bin/bash
# SessionStart hook: run on each new conversation session.
# Use this for auth switching, environment validation, or project-specific setup.
# NOTE: Must always produce output — Claude Code treats no output as failure.

cd "$CLAUDE_PROJECT_DIR" || { echo "session-start: skipped (no project dir)"; exit 0; }

# Verify git hooks are configured
HOOKS_PATH=$(git config core.hooksPath 2>/dev/null)
if [ "$HOOKS_PATH" != ".githooks" ]; then
    echo "session-start: configuring git hooks..."
    git config core.hooksPath .githooks
fi

# Sync tag state and keep main fresh
git fetch origin --tags --prune-tags --quiet 2>/dev/null || true
CURRENT_BRANCH=$(git branch --show-current 2>/dev/null || echo "")
if [ "$CURRENT_BRANCH" = "main" ]; then
    git pull --ff-only origin main 2>/dev/null || true
fi

# Auto-cleanup worktrees marked as done
DONE_TASKS=$(git tag -l 'wt/done/*' 2>/dev/null | sed 's|wt/done/||' || true)
if [ -n "$DONE_TASKS" ]; then
    PROJECT_NAME=$(basename "$CLAUDE_PROJECT_DIR")
    for TASK in $DONE_TASKS; do
        BRANCH="feat/$TASK"
        WT_DIR="$CLAUDE_PROJECT_DIR/../${PROJECT_NAME}-${TASK}"
        if [ -d "$WT_DIR" ]; then
            git worktree remove "$WT_DIR" --force 2>/dev/null || true
        fi
        if git show-ref --verify --quiet "refs/heads/$BRANCH" 2>/dev/null; then
            git branch -D "$BRANCH" 2>/dev/null || true
        fi
        git tag -d "wt/active/$TASK" 2>/dev/null || true
        git tag -d "wt/done/$TASK" 2>/dev/null || true
        git push origin --delete "wt/active/$TASK" 2>/dev/null || true
        git push origin --delete "wt/done/$TASK" 2>/dev/null || true
    done
    git worktree prune 2>/dev/null || true
fi

# Report stale active worktrees (older than 48 hours)
ACTIVE_TASKS=$(git tag -l 'wt/active/*' 2>/dev/null | sed 's|wt/active/||' || true)
if [ -n "$ACTIVE_TASKS" ]; then
    CUTOFF=$(date -d '48 hours ago' +%s 2>/dev/null || date -v-48H +%s 2>/dev/null || echo "0")
    for TASK in $ACTIVE_TASKS; do
        TAG_DATE=$(git log -1 --format='%ct' "wt/active/$TASK" 2>/dev/null || echo "0")
        if [ "$TAG_DATE" -gt 0 ] && [ "$CUTOFF" -gt 0 ] && [ "$TAG_DATE" -lt "$CUTOFF" ]; then
            AGE_HOURS=$(( ($(date +%s) - TAG_DATE) / 3600 ))
            echo "session-start: STALE worktree detected — feat/$TASK (active for ${AGE_HOURS}h)"
        fi
    done
fi

# Source engine-specific session start if it exists
ENGINE_HOOK="$CLAUDE_PROJECT_DIR/.claude/hooks/session-start-engine.sh"
if [ -f "$ENGINE_HOOK" ]; then
    source "$ENGINE_HOOK"
else
    echo "session-start: ready"
fi

exit 0
