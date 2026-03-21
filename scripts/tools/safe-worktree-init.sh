#!/usr/bin/env bash
# safe-worktree-init.sh — Subagent worktree bootstrap
# Creates a feature branch + worktree from local main (falls back to origin/main),
# and verifies the result. Replaces isolation:"worktree" for subagent dispatch.
#
# Usage: bash scripts/tools/safe-worktree-init.sh <task-name>
#   task-name: kebab-case identifier (e.g., fix-wheel-radius)
#
# Output:
#   WORKTREE_PATH=/absolute/path/to/worktree  (machine-readable, last line on success)
#
# Exit codes:
#   0 — success (or re-entry: worktree already exists)
#   1 — error (see stderr)

set -euo pipefail

TASK="${1:-}"
if [ -z "$TASK" ]; then
    echo "ERROR: task name required" >&2
    echo "Usage: bash scripts/tools/safe-worktree-init.sh <task-name>" >&2
    exit 1
fi

BRANCH="feat/${TASK}"

# Resolve main repo root
MAIN_REPO="${CLAUDE_PROJECT_DIR:-}"
if [ -z "$MAIN_REPO" ]; then
    MAIN_REPO="$(git rev-parse --show-toplevel 2>/dev/null)"
fi
if [ -z "$MAIN_REPO" ]; then
    echo "ERROR: Cannot determine main repo root (CLAUDE_PROJECT_DIR not set and not in a git repo)" >&2
    exit 1
fi

PROJECT_NAME="$(basename "$MAIN_REPO")"
WORKTREE_DIR="$(dirname "$MAIN_REPO")/${PROJECT_NAME}-${TASK}"

echo "safe-worktree-init: task=$TASK branch=$BRANCH"
echo "safe-worktree-init: main repo=$MAIN_REPO"
echo "safe-worktree-init: worktree dir=$WORKTREE_DIR"

# Re-entry: if worktree already exists, print path and exit
if git -C "$MAIN_REPO" worktree list --porcelain 2>/dev/null | grep -q "^worktree $WORKTREE_DIR$"; then
    echo "safe-worktree-init: worktree already exists (re-entry)"
    WORKTREE_ABS="$(cd "$WORKTREE_DIR" 2>/dev/null && pwd)"
    echo "WORKTREE_PATH=${WORKTREE_ABS}"
    exit 0
fi

# Fetch latest remote state (best-effort; never clobbers local main)
echo "safe-worktree-init: fetching origin/main (best-effort)..."
git -C "$MAIN_REPO" fetch origin main --quiet 2>/dev/null || true

# Branch base: always prefer local main (reflects queue-promote advances).
# Fall back to origin/main only if local main ref does not exist yet.
if git -C "$MAIN_REPO" show-ref --verify --quiet "refs/heads/main" 2>/dev/null; then
    BASE_REF="refs/heads/main"
    MAIN_SHA=$(git -C "$MAIN_REPO" rev-parse --short refs/heads/main 2>/dev/null || echo "?")
    echo "safe-worktree-init: branching from local main -> $MAIN_SHA"
else
    BASE_REF="origin/main"
    MAIN_SHA=$(git -C "$MAIN_REPO" rev-parse --short refs/remotes/origin/main 2>/dev/null || echo "?")
    echo "safe-worktree-init: branching from origin/main -> $MAIN_SHA (no local main found)"
fi

# Create worktree + feature branch from BASE_REF
if git -C "$MAIN_REPO" show-ref --verify --quiet "refs/heads/$BRANCH" 2>/dev/null; then
    echo "safe-worktree-init: branch $BRANCH already exists, checking out in new worktree..."
    git -C "$MAIN_REPO" worktree add "$WORKTREE_DIR" "$BRANCH"
else
    echo "safe-worktree-init: creating branch $BRANCH from $BASE_REF..."
    git -C "$MAIN_REPO" worktree add -b "$BRANCH" "$WORKTREE_DIR" "$BASE_REF"
fi

# Tag worktree as active
TAG_COMMIT=$(git -C "$WORKTREE_DIR" rev-parse HEAD 2>/dev/null || echo "")
if [ -n "$TAG_COMMIT" ]; then
    git -C "$MAIN_REPO" tag -f "wt/active/${TASK}" "$TAG_COMMIT" 2>/dev/null || true
    git -C "$MAIN_REPO" push origin "wt/active/${TASK}" --force --quiet 2>/dev/null || true
    # Clean up any stale done tag
    git -C "$MAIN_REPO" tag -d "wt/done/${TASK}" 2>/dev/null || true
    git -C "$MAIN_REPO" push origin --delete "wt/done/${TASK}" --quiet 2>/dev/null || true
    echo "safe-worktree-init: tagged wt/active/${TASK}"
fi

# Link Unity Library cache (junction on Windows, symlink on Unix)
MAIN_LIBRARY="$MAIN_REPO/Library"
WORKTREE_LIBRARY="$WORKTREE_DIR/Library"
if [ -d "$MAIN_LIBRARY" ] && [ ! -d "$WORKTREE_LIBRARY" ]; then
    echo "safe-worktree-init: linking Library cache..."
    OSTYPE_CHECK="$(uname -s 2>/dev/null || echo 'Windows')"
    if [[ "$OSTYPE_CHECK" == "MINGW"* ]] || [[ "$OSTYPE_CHECK" == "MSYS"* ]] || [[ -n "${WINDIR:-}" ]] || [[ "${OS:-}" == "Windows_NT" ]]; then
        # Windows: directory junction
        cmd //c "mklink /J \"$(cygpath -w "$WORKTREE_LIBRARY")\" \"$(cygpath -w "$MAIN_LIBRARY")\"" 2>/dev/null || \
        cmd.exe /c "mklink /J \"$(cygpath -w "$WORKTREE_LIBRARY" 2>/dev/null || echo "$WORKTREE_LIBRARY")\" \"$(cygpath -w "$MAIN_LIBRARY" 2>/dev/null || echo "$MAIN_LIBRARY")\"" 2>/dev/null || true
    else
        # Unix: symlink
        ln -s "$MAIN_LIBRARY" "$WORKTREE_LIBRARY" 2>/dev/null || true
    fi

    if [ -d "$WORKTREE_LIBRARY" ]; then
        echo "safe-worktree-init: Library cache linked successfully"
    else
        echo "safe-worktree-init: WARNING — Library link failed; Unity will do a full reimport"
    fi
else
    echo "safe-worktree-init: Library already linked or main Library not found, skipping"
fi

# Link .venv if present in main repo
MAIN_VENV="$MAIN_REPO/.venv"
WORKTREE_VENV="$WORKTREE_DIR/.venv"
if [ -d "$MAIN_VENV" ] && [ ! -d "$WORKTREE_VENV" ]; then
    echo "safe-worktree-init: linking .venv..."
    OSTYPE_CHECK="$(uname -s 2>/dev/null || echo 'Windows')"
    if [[ "$OSTYPE_CHECK" == "MINGW"* ]] || [[ "$OSTYPE_CHECK" == "MSYS"* ]] || [[ -n "${WINDIR:-}" ]] || [[ "${OS:-}" == "Windows_NT" ]]; then
        cmd //c "mklink /J \"$(cygpath -w "$WORKTREE_VENV")\" \"$(cygpath -w "$MAIN_VENV")\"" 2>/dev/null || true
    else
        ln -s "$MAIN_VENV" "$WORKTREE_VENV" 2>/dev/null || true
    fi
fi

# Verify: worktree exists, branch is correct
ACTUAL_BRANCH=$(git -C "$WORKTREE_DIR" branch --show-current 2>/dev/null || echo "")
if [ "$ACTUAL_BRANCH" != "$BRANCH" ]; then
    echo "ERROR: Verification failed — worktree branch is '$ACTUAL_BRANCH', expected '$BRANCH'" >&2
    exit 1
fi

WORKTREE_ABS="$(cd "$WORKTREE_DIR" && pwd)"
echo "safe-worktree-init: verified — worktree on branch $ACTUAL_BRANCH"
echo "safe-worktree-init: ready"
echo ""
echo "WORKTREE_PATH=${WORKTREE_ABS}"
