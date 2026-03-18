#!/usr/bin/env bash
# safe-worktree-init-batch.sh — Create multiple worktrees in one shot
# Fetches origin/main once, then creates N worktrees sequentially.
# Saves ~N-1 redundant git fetch calls compared to calling safe-worktree-init.sh N times.
#
# Usage: bash scripts/tools/safe-worktree-init-batch.sh task1 task2 task3 ...
# Output: one WORKTREE_PATH=... line per task (machine-readable)
#
# Exit codes:
#   0 — all worktrees created (or already existed)
#   1 — one or more failures (partial success possible)
set -euo pipefail

if (( $# == 0 )); then
  echo "Usage: bash scripts/tools/safe-worktree-init-batch.sh task1 task2 ..." >&2
  exit 1
fi

MAIN_REPO="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}"
if [[ -z "$MAIN_REPO" ]]; then
  echo "ERROR: Cannot determine main repo root" >&2
  exit 1
fi

PROJECT_NAME="$(basename "$MAIN_REPO")"
PARENT_DIR="$(dirname "$MAIN_REPO")"

# Single fetch for all worktrees
echo "batch-init: fetching origin/main (once)..."
git -C "$MAIN_REPO" fetch origin main --quiet 2>/dev/null || true
git -C "$MAIN_REPO" update-ref refs/heads/main refs/remotes/origin/main 2>/dev/null || true
MAIN_SHA=$(git -C "$MAIN_REPO" rev-parse --short refs/remotes/origin/main 2>/dev/null || echo "?")
echo "batch-init: origin/main -> $MAIN_SHA"
echo ""

failures=0
created=0

for task in "$@"; do
  branch="feat/${task}"
  worktree_dir="${PARENT_DIR}/${PROJECT_NAME}-${task}"

  # Re-entry check
  if git -C "$MAIN_REPO" worktree list --porcelain 2>/dev/null | grep -q "^worktree ${worktree_dir}$"; then
    worktree_abs="$(cd "$worktree_dir" 2>/dev/null && pwd)"
    echo "batch-init: [skip] $task — already exists"
    echo "WORKTREE_PATH=${worktree_abs}"
    continue
  fi

  # Create worktree
  if git -C "$MAIN_REPO" show-ref --verify --quiet "refs/heads/$branch" 2>/dev/null; then
    if ! git -C "$MAIN_REPO" worktree add "$worktree_dir" "$branch" 2>/dev/null; then
      echo "batch-init: [FAIL] $task — git worktree add failed" >&2
      failures=$((failures + 1))
      continue
    fi
  else
    if ! git -C "$MAIN_REPO" worktree add -b "$branch" "$worktree_dir" origin/main 2>/dev/null; then
      echo "batch-init: [FAIL] $task — git worktree add -b failed" >&2
      failures=$((failures + 1))
      continue
    fi
  fi

  # Tag worktree as active
  tag_commit=$(git -C "$worktree_dir" rev-parse HEAD 2>/dev/null || echo "")
  if [[ -n "$tag_commit" ]]; then
    git -C "$MAIN_REPO" tag -f "wt/active/${task}" "$tag_commit" 2>/dev/null || true
    git -C "$MAIN_REPO" push origin "wt/active/${task}" --force --quiet 2>/dev/null || true
    git -C "$MAIN_REPO" tag -d "wt/done/${task}" 2>/dev/null || true
    git -C "$MAIN_REPO" push origin --delete "wt/done/${task}" --quiet 2>/dev/null || true
  fi

  # Link Library cache
  main_library="$MAIN_REPO/Library"
  worktree_library="$worktree_dir/Library"
  if [[ -d "$main_library" ]] && [[ ! -d "$worktree_library" ]]; then
    ln -s "$main_library" "$worktree_library" 2>/dev/null || true
  fi

  # Link .venv
  main_venv="$MAIN_REPO/.venv"
  worktree_venv="$worktree_dir/.venv"
  if [[ -d "$main_venv" ]] && [[ ! -d "$worktree_venv" ]]; then
    ln -s "$main_venv" "$worktree_venv" 2>/dev/null || true
  fi

  # Verify
  actual_branch=$(git -C "$worktree_dir" branch --show-current 2>/dev/null || echo "")
  if [[ "$actual_branch" != "$branch" ]]; then
    echo "batch-init: [FAIL] $task — branch mismatch: '$actual_branch' != '$branch'" >&2
    failures=$((failures + 1))
    continue
  fi

  worktree_abs="$(cd "$worktree_dir" && pwd)"
  echo "batch-init: [ok] $task"
  echo "WORKTREE_PATH=${worktree_abs}"
  created=$((created + 1))
done

echo ""
echo "batch-init: $created created, $failures failed, $# total"

if (( failures > 0 )); then
  exit 1
fi
exit 0
