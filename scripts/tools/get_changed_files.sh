#!/usr/bin/env bash
# get_changed_files.sh — Normalize changed file selection for lint tools
# Usage:
#   get_changed_files.sh --staged           # staged files (pre-commit)
#   get_changed_files.sh --changed <ref>    # files changed vs ref (e.g., origin/main)
#   get_changed_files.sh --all              # all tracked files
#   get_changed_files.sh --cs              # filter to .cs only (add after any mode flag)
#
# Outputs one file path per line.
set -euo pipefail

mode="staged"
filter=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --staged) mode="staged"; shift ;;
    --changed) mode="changed"; ref="${2:?--changed requires a ref}"; shift 2 ;;
    --all) mode="all"; shift ;;
    --cs) filter="cs"; shift ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

case "$mode" in
  staged)
    git diff --cached --name-only --diff-filter=ACM 2>/dev/null || true ;;
  changed)
    git diff --name-only --diff-filter=ACM "${ref}" HEAD 2>/dev/null || true ;;
  all)
    git ls-files 2>/dev/null || true ;;
esac | if [ "$filter" = "cs" ]; then grep '\.cs$'; else cat; fi
