#!/bin/bash
# Claude Code PreToolUse hook: block git commit and branch-switch on main branch.
# This hook cannot be bypassed by --no-verify or any git flag.
# It runs BEFORE the command executes.

CMD=$(jq -r ".tool_input.command // empty")

# Block --no-verify (never allowed in this project)
if echo "$CMD" | grep -qE '\-\-no-verify'; then
    echo "BLOCKED: --no-verify is not allowed in this project." >&2
    echo "Fix the pre-commit hook issue instead of bypassing it." >&2
    exit 1
fi

# Detect branch-switch commands:
#   git switch <branch>               — always a branch switch
#   git checkout <branch>             — branch switch when no ' -- ' separator
# We do NOT flag: git checkout -- file, git checkout . (file restore patterns)
IS_BRANCH_SWITCH=0
if echo "$CMD" | grep -qE '\bgit\b.*\bswitch\b'; then
    IS_BRANCH_SWITCH=1
fi
if echo "$CMD" | grep -qE '\bgit\b.*\bcheckout\b' && \
   ! echo "$CMD" | grep -qF ' -- ' && \
   ! echo "$CMD" | grep -qE '\bcheckout\s+\.'; then
    IS_BRANCH_SWITCH=1
fi

# Detect commit commands
IS_COMMIT=0
if echo "$CMD" | grep -qE '\bgit\b.*\bcommit\b'; then
    IS_COMMIT=1
fi

# Nothing to check — allow
if [ "$IS_BRANCH_SWITCH" = "0" ] && [ "$IS_COMMIT" = "0" ]; then
    exit 0
fi

# Check current branch (main working tree)
BRANCH=$(git -C "$CLAUDE_PROJECT_DIR" branch --show-current 2>/dev/null)

# Block branch switch in main working tree
if [ "$IS_BRANCH_SWITCH" = "1" ] && [ "$BRANCH" = "main" ]; then
    echo "BLOCKED: Cannot switch branches in the main working tree." >&2
    echo "" >&2
    echo "  Command attempted: $CMD" >&2
    echo "" >&2
    echo "The main working tree must stay on 'main' at all times." >&2
    echo "Use worktrees for all feature work:" >&2
    echo "  bash scripts/tools/safe-worktree-init.sh <task-name>" >&2
    echo "" >&2
    echo "Full guide: .agents/skills/branch-workflow/SKILL.md" >&2
    exit 1
fi

# Block commit on main branch
if [ "$IS_COMMIT" = "1" ] && [ "$BRANCH" = "main" ]; then
    # Allow if ALLOW_MASTER_COMMIT is set in the command
    if echo "$CMD" | grep -qE 'ALLOW_MASTER_COMMIT=1'; then
        exit 0
    fi
    echo "BLOCKED: Cannot commit on main branch." >&2
    echo "" >&2
    echo "ALL code changes must go through feature branches + PRs." >&2
    echo "" >&2
    echo "If you are a subagent, you should be in a worktree on a feat/ branch." >&2
    echo "Run safe-worktree-init.sh as your FIRST action, then work only in the worktree." >&2
    echo "  bash scripts/tools/safe-worktree-init.sh <task-name>" >&2
    echo "" >&2
    echo "If you are the main agent or on main by mistake:" >&2
    echo "  1. Do NOT edit files on main — dispatch a subagent instead" >&2
    echo "  2. Subagent runs: bash scripts/tools/safe-worktree-init.sh <task-name>" >&2
    echo "  3. Or manually: just worktree-create <task-name>" >&2
    echo "" >&2
    echo "Full guide: .agents/skills/branch-workflow/SKILL.md" >&2
    exit 1
fi

exit 0
