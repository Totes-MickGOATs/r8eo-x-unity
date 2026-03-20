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

# Sync from remote only if origin is reachable (best-effort — local-first workflow)
CURRENT_BRANCH=$(git branch --show-current 2>/dev/null || echo "")
HAS_ORIGIN=$(git remote | grep -q '^origin$' && echo "yes" || echo "no")
if [ "$HAS_ORIGIN" = "yes" ]; then
    git fetch origin --tags --prune-tags --quiet 2>/dev/null || true
    # Only update local main if remote is ahead (ff-only, never rewrite history)
    REMOTE_MAIN=$(git rev-parse refs/remotes/origin/main 2>/dev/null || echo "")
    LOCAL_MAIN=$(git rev-parse refs/heads/main 2>/dev/null || echo "")
    if [ -n "$REMOTE_MAIN" ] && [ -n "$LOCAL_MAIN" ] && [ "$REMOTE_MAIN" != "$LOCAL_MAIN" ]; then
        if git merge-base --is-ancestor "$LOCAL_MAIN" "$REMOTE_MAIN" 2>/dev/null; then
            git update-ref refs/heads/main "$REMOTE_MAIN" 2>/dev/null || true
            echo "session-start: local main fast-forwarded to origin/main"
        fi
    fi
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

# Report in-flight work from previous sessions
# Find the memory directory for this project (Claude Code encodes the path)
MEMORY_DIR=""
if [ -d "$HOME/.claude/projects" ]; then
    # Match directory containing this project's name
    PROJECT_BASE=$(basename "$CLAUDE_PROJECT_DIR")
    MEMORY_DIR=$(find "$HOME/.claude/projects" -maxdepth 1 -type d -name "*${PROJECT_BASE}*" -print -quit 2>/dev/null || true)
    if [ -n "$MEMORY_DIR" ]; then
        MEMORY_DIR="${MEMORY_DIR}/memory"
    fi
fi
INFLIGHT_FILES=$(ls "$MEMORY_DIR"/project_inflight_*.md 2>/dev/null || true)
if [ -n "$INFLIGHT_FILES" ]; then
    echo "session-start: IN-FLIGHT WORK detected from previous sessions:"
    for F in $INFLIGHT_FILES; do
        NAME=$(basename "$F" .md | sed 's/project_inflight_//')
        echo "  - $NAME (read $F for details)"
    done
    echo "session-start: Check these before starting new work on related systems."
fi

# Report local verification queue status
QUEUE_FILE="${CLAUDE_PROJECT_DIR}/Logs/automation/queue.json"
if [ -f "$QUEUE_FILE" ] && command -v python3 >/dev/null 2>&1; then
    python3 - "$QUEUE_FILE" <<'PYEOF'
import json, sys
try:
    with open(sys.argv[1]) as f:
        data = json.load(f)
    queue = data.get('queue', [])
    results = data.get('results', {})
    queued = [e for e in queue if e.get('status') == 'queued']
    passed = [b for b, r in results.items() if r.get('status') == 'passed']
    if queued or passed:
        print("session-start: verification queue —", end=" ")
        parts = []
        if queued:
            parts.append(f"{len(queued)} queued (run: just queue-run)")
        if passed:
            parts.append(f"{len(passed)} passed (promote: just queue-promote <branch>)")
        print(", ".join(parts))
except Exception:
    pass
PYEOF
fi

# Source engine-specific session start if it exists
ENGINE_HOOK="$CLAUDE_PROJECT_DIR/.claude/hooks/session-start-engine.sh"
if [ -f "$ENGINE_HOOK" ]; then
    source "$ENGINE_HOOK"
else
    echo "session-start: ready"
fi

exit 0
