#!/bin/bash
# Claude Code PreToolUse hook: block Edit/Write tool calls on repo files when on main branch.
# Prevents agents from editing files directly in the main working tree.
# It runs BEFORE the tool executes.

# Get the file path from the tool input (works for both Edit and Write)
FILE_PATH=$(jq -r ".tool_input.file_path // .tool_input.path // empty")

# If no file path, nothing to check
if [ -z "$FILE_PATH" ]; then
    exit 0
fi

# Check current branch (main working tree)
BRANCH=$(git -C "$CLAUDE_PROJECT_DIR" branch --show-current 2>/dev/null)

if [ "$BRANCH" != "main" ]; then
    exit 0
fi

# Check if the file is inside the project directory
case "$FILE_PATH" in
    "$CLAUDE_PROJECT_DIR"*)
        echo "BLOCKED: Cannot edit repo files directly on main branch." >&2
        echo "" >&2
        echo "  File attempted: $FILE_PATH" >&2
        echo "" >&2
        echo "ALL code changes must go through feature branches in worktrees." >&2
        echo "" >&2
        echo "If you are a subagent, run safe-worktree-init.sh as your FIRST action:" >&2
        echo "  bash scripts/tools/safe-worktree-init.sh <task-name>" >&2
        echo "" >&2
        echo "Then work exclusively in the worktree using absolute paths." >&2
        echo "" >&2
        echo "Full guide: .agents/skills/branch-workflow/SKILL.md" >&2
        exit 1
        ;;
    *)
        # File is outside the project directory — allow (e.g., /tmp files)
        exit 0
        ;;
esac
