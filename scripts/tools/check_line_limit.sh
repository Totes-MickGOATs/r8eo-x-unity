#!/usr/bin/env bash
# check_line_limit.sh — Fail CI if any .cs file exceeds the 200-line hard limit
set -euo pipefail

LIMIT=200
SEARCH_DIRS=("Assets/Scripts" "Assets/Tests")

# Auto-generated files exempt from the limit
ALLOWLIST=(
  "R8EOXInputActions.cs"
)

violations=0

is_allowed() {
  local file="$1"
  local basename
  basename="$(basename "$file")"
  for allowed in "${ALLOWLIST[@]}"; do
    if [[ "$basename" == "$allowed" ]]; then
      return 0
    fi
  done
  return 1
}

for dir in "${SEARCH_DIRS[@]}"; do
  if [[ ! -d "$dir" ]]; then continue; fi
  while IFS= read -r -d '' file; do
    if is_allowed "$file"; then continue; fi
    lines=$(wc -l < "$file")
    if (( lines > LIMIT )); then
      echo "FAIL: $file ($lines lines, limit=$LIMIT)"
      violations=$((violations + 1))
    fi
  done < <(find "$dir" -name '*.cs' -print0)
done

if (( violations > 0 )); then
  echo ""
  echo "$violations file(s) exceed the $LIMIT-line limit."
  exit 1
fi

echo "All .cs files are within the $LIMIT-line limit."
