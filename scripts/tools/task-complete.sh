#!/usr/bin/env bash
# task-complete.sh — Main agent cleanup after branch is promoted (local-first)
# Accepts either local promotion (git merge-base) or remote PR merge as proof of done.
# Usage: task-complete.sh <task>
set -euo pipefail

TASK="${1:?task name required}"
BRANCH="feat/${TASK}"

# ── Resolve paths ──────────────────────────────────────────────────────────
MAIN_REPO="${CLAUDE_PROJECT_DIR:-$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)}"
PROJECT_NAME=$(basename "$MAIN_REPO")
WORKTREE_DIR="$(dirname "$MAIN_REPO")/${PROJECT_NAME}-${TASK}"

echo "task-complete: task=${TASK}"
echo "task-complete: branch=${BRANCH}"
echo "task-complete: worktree=${WORKTREE_DIR}"

# ── Verify branch was promoted (local-first) OR merged via PR ─────────────
echo "task-complete: verifying branch is done..."

DONE_LOCAL=0
DONE_REMOTE=0

# Check 1: local promotion — branch tip is ancestor of local main
if git -C "$MAIN_REPO" merge-base --is-ancestor "$BRANCH" main 2>/dev/null; then
  DONE_LOCAL=1
  echo "task-complete: branch '${BRANCH}' is ancestor of local main (locally promoted)"
fi

# Check 2: remote PR merged (fallback — only if gh is available)
if [ $DONE_LOCAL -eq 0 ] && command -v gh >/dev/null 2>&1; then
  MERGED_PR=$(gh pr list --head "$BRANCH" --state merged --json number --limit 1 \
    --jq '.[0].number // empty' 2>/dev/null || echo "")
  if [ -n "$MERGED_PR" ]; then
    DONE_REMOTE=1
    echo "task-complete: confirmed PR #${MERGED_PR} merged remotely"
  fi
fi

if [ $DONE_LOCAL -eq 0 ] && [ $DONE_REMOTE -eq 0 ]; then
  echo "ERROR: Branch '${BRANCH}' is not done yet." >&2
  echo "  Option 1 (local-first): just queue-promote ${BRANCH}" >&2
  echo "  Option 2 (remote):      just lifecycle ship  (then re-run task-complete)" >&2
  exit 1
fi

# ── Delete remote branch (best-effort) ────────────────────────────────────
echo "task-complete: deleting remote branch (best-effort)..."
git -C "$MAIN_REPO" push origin --delete "$BRANCH" 2>/dev/null || true

# ── Remove worktree ────────────────────────────────────────────────────────
echo "task-complete: removing worktree..."
git -C "$MAIN_REPO" worktree remove "$WORKTREE_DIR" --force 2>/dev/null || true

# ── Delete local branch ────────────────────────────────────────────────────
echo "task-complete: deleting local branch..."
git -C "$MAIN_REPO" branch -D "$BRANCH" 2>/dev/null || true

# ── Delete lifecycle tags (local + remote, best-effort) ───────────────────
echo "task-complete: cleaning up lifecycle tags..."
git -C "$MAIN_REPO" tag -d "wt/active/${TASK}" 2>/dev/null || true
git -C "$MAIN_REPO" push origin --delete "wt/active/${TASK}" 2>/dev/null || true
git -C "$MAIN_REPO" tag -d "wt/done/${TASK}" 2>/dev/null || true
git -C "$MAIN_REPO" push origin --delete "wt/done/${TASK}" 2>/dev/null || true

# ── Prune worktrees; sync main from remote if ahead (best-effort) ─────────
echo "task-complete: pruning worktrees..."
git -C "$MAIN_REPO" worktree prune

HAS_ORIGIN=$(git -C "$MAIN_REPO" remote | grep -q '^origin$' && echo "yes" || echo "no")
if [ "$HAS_ORIGIN" = "yes" ]; then
  git -C "$MAIN_REPO" fetch origin main --quiet 2>/dev/null || true
  REMOTE_MAIN=$(git -C "$MAIN_REPO" rev-parse refs/remotes/origin/main 2>/dev/null || echo "")
  LOCAL_MAIN=$(git -C "$MAIN_REPO" rev-parse refs/heads/main 2>/dev/null || echo "")
  if [ -n "$REMOTE_MAIN" ] && [ "$REMOTE_MAIN" != "$LOCAL_MAIN" ]; then
    if git -C "$MAIN_REPO" merge-base --is-ancestor "$LOCAL_MAIN" "$REMOTE_MAIN" 2>/dev/null; then
      git -C "$MAIN_REPO" update-ref refs/heads/main "$REMOTE_MAIN" 2>/dev/null || true
      echo "task-complete: local main fast-forwarded to origin/main"
    fi
  fi
fi

MAIN_SHA=$(git -C "$MAIN_REPO" rev-parse --short main)
echo "Task ${TASK} complete. main at ${MAIN_SHA}."
