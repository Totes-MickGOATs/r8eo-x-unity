#!/usr/bin/env bash
# Poll a PR until it merges, with tiered backoff.
#
# Usage: scripts/wait-for-merge.sh <pr-number> [--timeout <minutes>]
set -euo pipefail

PR_NUMBER="${1:?Usage: wait-for-merge.sh <pr-number> [--timeout <minutes>]}"
shift

TIMEOUT_MINUTES=15
while [[ $# -gt 0 ]]; do
  case "$1" in
    --timeout) TIMEOUT_MINUTES="$2"; shift 2 ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

TIMEOUT_SECONDS=$((TIMEOUT_MINUTES * 60))
ELAPSED=0

# Tiered backoff: 10s for first 2min, 30s for next 5min, 60s after that
backoff_interval() {
  local elapsed=$1
  if [ "$elapsed" -lt 120 ]; then
    echo 10
  elif [ "$elapsed" -lt 420 ]; then
    echo 30
  else
    echo 60
  fi
}

echo "Waiting for PR #${PR_NUMBER} to merge (timeout: ${TIMEOUT_MINUTES}m)..."

while [ "$ELAPSED" -lt "$TIMEOUT_SECONDS" ]; do
  STATE=$(gh pr view "$PR_NUMBER" --json state -q '.state' 2>/dev/null || echo "UNKNOWN")

  case "$STATE" in
    MERGED)
      echo "PR #${PR_NUMBER} merged after ~${ELAPSED}s."
      exit 0
      ;;
    CLOSED)
      echo "ERROR: PR #${PR_NUMBER} was closed without merging." >&2
      exit 1
      ;;
    UNKNOWN)
      echo "WARNING: Could not fetch PR state. Retrying..."
      ;;
    *)
      # Still open — check merge status for diagnostics
      MERGE_STATUS=$(gh pr view "$PR_NUMBER" --json mergeStateStatus -q '.mergeStateStatus' 2>/dev/null || echo "?")
      if [ "$((ELAPSED % 60))" -eq 0 ] || [ "$ELAPSED" -eq 0 ]; then
        echo "  PR #${PR_NUMBER}: state=$STATE mergeStatus=$MERGE_STATUS elapsed=${ELAPSED}s"
      fi
      ;;
  esac

  INTERVAL=$(backoff_interval "$ELAPSED")
  sleep "$INTERVAL"
  ELAPSED=$((ELAPSED + INTERVAL))
done

# Timeout — diagnose
echo ""
echo "TIMEOUT: PR #${PR_NUMBER} did not merge within ${TIMEOUT_MINUTES} minutes." >&2
echo ""
echo "Diagnostics:" >&2
gh pr view "$PR_NUMBER" --json state,mergeStateStatus,statusCheckRollup 2>/dev/null || true
echo "" >&2
echo "Common causes:" >&2
echo "  - CI checks still running or failed" >&2
echo "  - Branch is behind base (needs rebase/update)" >&2
echo "  - Required reviews missing" >&2
echo "  - Merge conflicts" >&2
exit 1
