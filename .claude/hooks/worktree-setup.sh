#!/bin/bash
# WorktreeCreate hook: auto-setup new worktrees
# Runs AFTER Claude Code creates a worktree. Fetches latest remote state
# and rebases the worktree branch onto origin/main so agents never start
# from a stale base.
# NOTE: Must always produce output — Claude Code treats no output as failure.
cd "$CLAUDE_PROJECT_DIR" || { echo "worktree-setup: skipped (no project dir)"; exit 0; }

# --- Fetch latest remote state and update local main ---
echo "worktree-setup: fetching latest remote state..."
git fetch origin --quiet 2>/dev/null || true
git update-ref refs/heads/main refs/remotes/origin/main 2>/dev/null || true
MAIN_SHA=$(git rev-parse --short refs/remotes/origin/main 2>/dev/null || echo "?")
echo "worktree-setup: local main -> $MAIN_SHA"

# --- Rebase worktree branch onto fresh origin/main ---
# The worktree may have been created from a stale local main. Rebase now
# so the agent starts from the true current state.
CURRENT_BRANCH=$(git branch --show-current 2>/dev/null || echo "")
if [ -n "$CURRENT_BRANCH" ] && [ "$CURRENT_BRANCH" != "main" ]; then
    echo "worktree-setup: rebasing $CURRENT_BRANCH onto origin/main..."
    if git rebase origin/main --quiet 2>/dev/null; then
        echo "worktree-setup: branch is up to date with origin/main"
    else
        # Abort rebase on conflict — agent will handle manually if needed
        git rebase --abort 2>/dev/null || true
        echo "worktree-setup: WARNING — rebase had conflicts, skipped (agent should rebase manually)"
    fi
fi

# Source engine-specific worktree setup if it exists
ENGINE_HOOK="$CLAUDE_PROJECT_DIR/.claude/hooks/worktree-setup-engine.sh"
if [ -f "$ENGINE_HOOK" ]; then
    source "$ENGINE_HOOK"
else
    echo "worktree-setup: no engine-specific setup configured"
fi

# Always produce output — Claude Code treats no output as failure
echo "worktree-setup: ready"
exit 0
