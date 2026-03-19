#!/usr/bin/env bash
# subagent-lifecycle.sh — Consolidates subagent workflow into 2 commands
# Usage:
#   subagent-lifecycle.sh init <task>   — create worktree (delegates to safe-worktree-init.sh)
#   subagent-lifecycle.sh ship          — push, create PR, merge, sync main
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cmd="${1:-}"

case "$cmd" in
  init)
    task="${2:?Usage: subagent-lifecycle.sh init <task>}"
    exec bash "${SCRIPT_DIR}/safe-worktree-init.sh" "$task"
    ;;

  ship)
    # ── Derive task from current branch ────────────────────────────────────
    BRANCH=$(git branch --show-current)
    if [ -z "$BRANCH" ] || [ "$BRANCH" = "main" ] || [ "$BRANCH" = "master" ]; then
      echo "ERROR: Must be on a feature branch, not '${BRANCH:-detached HEAD}'" >&2
      exit 1
    fi

    # Strip feat/ prefix to get the task name
    TASK="${BRANCH#feat/}"

    echo "subagent-lifecycle ship: task=${TASK} branch=${BRANCH}"

    # ── Fetch and rebase if behind ──────────────────────────────────────────
    echo "subagent-lifecycle ship: fetching origin/main..."
    git fetch origin main --quiet

    if ! git merge-base --is-ancestor origin/main HEAD 2>/dev/null; then
      echo "subagent-lifecycle ship: rebasing onto origin/main..."
      if ! git rebase origin/main; then
        echo "ERROR: Rebase onto origin/main failed. Resolve conflicts first." >&2
        git rebase --abort 2>/dev/null || true
        exit 1
      fi
      echo "subagent-lifecycle ship: rebase successful"
    fi

    # ── Push (pre-push hook runs automatically) ─────────────────────────────
    echo "subagent-lifecycle ship: pushing feat/${TASK}..."
    git push --force-with-lease -u origin "feat/${TASK}"

    # ── Create PR if none exists ────────────────────────────────────────────
    EXISTING_PR=$(gh pr list --head "feat/${TASK}" --state open --json number --limit 1 \
      --jq '.[0].number // empty' 2>/dev/null || echo "")

    if [ -n "$EXISTING_PR" ]; then
      PR_NUMBER="$EXISTING_PR"
      echo "subagent-lifecycle ship: using existing PR #${PR_NUMBER}"
    else
      echo "subagent-lifecycle ship: creating PR..."
      PR_NUMBER=$(gh pr create \
        --base main \
        --title "feat: ${TASK}" \
        --body "Automated PR from subagent lifecycle" \
        --json number --jq '.number')
      echo "subagent-lifecycle ship: created PR #${PR_NUMBER}"
    fi

    # ── Merge PR ────────────────────────────────────────────────────────────
    echo "subagent-lifecycle ship: merging PR #${PR_NUMBER}..."
    if ! gh pr merge "$PR_NUMBER" --squash; then
      echo "ERROR: PR merge failed. Check: gh pr view ${PR_NUMBER}" >&2
      exit 1
    fi

    # ── Sync local main ─────────────────────────────────────────────────────
    echo "subagent-lifecycle ship: syncing local main..."
    git fetch origin main
    git update-ref refs/heads/main refs/remotes/origin/main

    MAIN_SHA=$(git rev-parse --short main)
    echo "PR #${PR_NUMBER} merged. Local main at ${MAIN_SHA}."
    ;;

  *)
    echo "Usage: subagent-lifecycle.sh <init|ship> [args...]" >&2
    echo "" >&2
    echo "  init <task>   Create worktree for task (delegates to safe-worktree-init.sh)" >&2
    echo "  ship          Push, create PR, merge, sync local main" >&2
    exit 1
    ;;
esac
