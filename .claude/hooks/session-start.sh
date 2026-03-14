#!/bin/bash
# SessionStart hook: run on each new conversation session.
# Use this for auth switching, environment validation, or project-specific setup.
# NOTE: Must always produce output — Claude Code treats no output as failure.

cd "$CLAUDE_PROJECT_DIR" || { echo "session-start: skipped (no project dir)"; exit 0; }

# Keep main branch fresh (only when on main)
if [ "$(git branch --show-current 2>/dev/null)" = "main" ]; then
    git fetch origin main --quiet 2>/dev/null || true
    git pull --ff-only origin main 2>/dev/null || true
fi

# Verify git hooks are configured
HOOKS_PATH=$(git config core.hooksPath 2>/dev/null)
if [ "$HOOKS_PATH" != ".githooks" ]; then
    echo "session-start: configuring git hooks..."
    git config core.hooksPath .githooks
fi

# Source engine-specific session start if it exists
ENGINE_HOOK="$CLAUDE_PROJECT_DIR/.claude/hooks/session-start-engine.sh"
if [ -f "$ENGINE_HOOK" ]; then
    source "$ENGINE_HOOK"
else
    echo "session-start: ready"
fi

exit 0
