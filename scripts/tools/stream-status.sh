#!/usr/bin/env bash
# stream-status.sh — Dashboard showing status of all active worktree branches
# Shows commit count, push status, and PR state for each active stream.
#
# Usage: bash scripts/tools/stream-status.sh
set -euo pipefail

MAIN_REPO="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}"
if [[ -z "$MAIN_REPO" ]]; then
  echo "ERROR: Cannot determine main repo root" >&2
  exit 1
fi

# Check for gh CLI
has_gh=false
if command -v gh &>/dev/null; then
  has_gh=true
fi

echo "=== Stream Status Dashboard ==="
echo ""
printf "%-35s %-12s %-8s %-8s %s\n" "BRANCH" "STATUS" "COMMITS" "FILES" "PR"
printf "%-35s %-12s %-8s %-8s %s\n" "------" "------" "-------" "-----" "--"

# Parse porcelain format: groups of (worktree, HEAD, branch) lines
wt_path=""
wt_branch=""
total=0
pushed_count=0

while IFS= read -r line; do
  if [[ "$line" == worktree\ * ]]; then
    wt_path="${line#worktree }"
  elif [[ "$line" == branch\ * ]]; then
    ref="${line#branch }"
    wt_branch="${ref#refs/heads/}"
  elif [[ -z "$line" ]]; then
    # End of entry — process this worktree
    if [[ -z "$wt_branch" || "$wt_branch" == "main" || "$wt_branch" == "master" ]]; then
      wt_path=""
      wt_branch=""
      continue
    fi

    total=$((total + 1))

    # Count commits ahead of main
    commit_count=$(git -C "$wt_path" rev-list --count "main..$wt_branch" 2>/dev/null || echo "?")

    # Count changed files
    file_count=$(git -C "$wt_path" diff --name-only "main...$wt_branch" 2>/dev/null | wc -l | tr -d ' ')

    # Check if pushed to remote
    remote_ref="refs/remotes/origin/$wt_branch"
    if git -C "$MAIN_REPO" show-ref --verify --quiet "$remote_ref" 2>/dev/null; then
      local_sha=$(git -C "$wt_path" rev-parse "$wt_branch" 2>/dev/null || echo "")
      remote_sha=$(git -C "$MAIN_REPO" rev-parse "$remote_ref" 2>/dev/null || echo "")
      if [[ "$local_sha" == "$remote_sha" ]]; then
        status="pushed"
        pushed_count=$((pushed_count + 1))
      else
        status="ahead"
      fi
    else
      if [[ "$commit_count" != "?" ]] && (( commit_count > 0 )); then
        status="local"
      else
        status="empty"
      fi
    fi

    # Check PR status via gh
    pr_info="-"
    if $has_gh && [[ "$status" == "pushed" || "$status" == "ahead" ]]; then
      pr_json=$(gh pr list --head "$wt_branch" --json number,state --limit 1 2>/dev/null || echo "[]")
      pr_num=$(echo "$pr_json" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['number'] if d else '')" 2>/dev/null || echo "")
      if [[ -n "$pr_num" ]]; then
        pr_state=$(echo "$pr_json" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['state'] if d else '')" 2>/dev/null || echo "?")
        pr_info="#${pr_num} (${pr_state})"
      fi
    fi

    printf "%-35s %-12s %-8s %-8s %s\n" "$wt_branch" "$status" "$commit_count" "$file_count" "$pr_info"

    wt_path=""
    wt_branch=""
  fi
done < <(git -C "$MAIN_REPO" worktree list --porcelain 2>/dev/null; echo "")

echo ""
echo "$total active stream(s), $pushed_count pushed to remote"
