#!/usr/bin/env bash
# conflict-forecast.sh — Detect files touched by multiple active worktree branches
# Run before dispatching parallel streams or periodically during execution.
# Exit 0 if no conflicts, exit 1 if overlapping files found.
#
# Usage: bash scripts/tools/conflict-forecast.sh [--verbose]
set -euo pipefail

VERBOSE="${1:-}"
MAIN_REPO="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}"

if [[ -z "$MAIN_REPO" ]]; then
  echo "ERROR: Cannot determine main repo root" >&2
  exit 1
fi

# Collect all active feat/ branches from worktrees
branches=()
while IFS= read -r line; do
  if [[ "$line" == branch\ * ]]; then
    ref="${line#branch }"
    branch="${ref#refs/heads/}"
    if [[ "$branch" == feat/* ]]; then
      branches+=("$branch")
    fi
  fi
done < <(git -C "$MAIN_REPO" worktree list --porcelain 2>/dev/null)

if (( ${#branches[@]} < 2 )); then
  echo "Only ${#branches[@]} active branch(es) — no cross-stream conflicts possible."
  exit 0
fi

echo "Scanning ${#branches[@]} active branches for file overlaps..."
echo ""

# Generate file→branch mapping using a temp file approach (bash 3.x compatible)
tmpdir=$(mktemp -d)
trap 'rm -rf "$tmpdir"' EXIT

for branch in "${branches[@]}"; do
  # Get files changed on this branch vs main, tag each with the branch name
  git -C "$MAIN_REPO" diff --name-only "main...$branch" 2>/dev/null | while IFS= read -r file; do
    [[ -z "$file" ]] && continue
    echo "$branch" >> "$tmpdir/$(echo "$file" | sed 's|/|__|g')"
  done
done

# Report conflicts
conflicts=0
for encoded_file in "$tmpdir"/*; do
  [[ ! -f "$encoded_file" ]] && continue

  branch_count=$(wc -l < "$encoded_file" | tr -d ' ')
  if (( branch_count > 1 )); then
    # Decode filename
    file=$(basename "$encoded_file" | sed 's|__|/|g')
    branch_list=$(paste -sd ', ' "$encoded_file")
    echo "CONFLICT: $file"
    echo "  touched by: $branch_list"
    conflicts=$((conflicts + 1))
  elif [[ "$VERBOSE" == "--verbose" ]]; then
    file=$(basename "$encoded_file" | sed 's|__|/|g')
    branch_name=$(cat "$encoded_file")
    echo "  ok: $file ($branch_name)"
  fi
done

echo ""
if (( conflicts > 0 )); then
  echo "$conflicts file(s) touched by multiple branches — merge conflicts likely."
  exit 1
fi

echo "No cross-stream file conflicts detected."
exit 0
