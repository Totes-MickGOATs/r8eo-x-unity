#!/usr/bin/env bash
# audit_line_limit.sh — Repo-wide 150-line audit via git ls-files
# Report-only by default (exit 0). Use --strict to exit 1 on violations outside the exception ledger.
#
# Usage:
#   bash scripts/tools/audit_line_limit.sh              # report-only, exits 0
#   bash scripts/tools/audit_line_limit.sh --strict     # exits 1 if non-excepted files violate limit
set -euo pipefail

LIMIT=150
EXCEPTIONS_FILE=".line-limit-exceptions.json"
STRICT=0
if [[ "${1:-}" == "--strict" ]]; then STRICT=1; fi

# Governed extensions (text files with meaningful line counts)
EXTENSIONS=("*.cs" "*.md" "*.json" "*.yml" "*.yaml" "*.txt" "*.asmdef" "*.ruleset" "*.uxml" "*.uss")

# Patterns to exclude (binary/generated/cache)
EXCLUDES=(
  "Library/"
  "Logs/"
  "Temp/"
  "obj/"
  "*.meta"
  "Packages/packages-lock.json"
)

is_excluded() {
  local file="$1"
  for pat in "${EXCLUDES[@]}"; do
    case "$file" in
      *"$pat"*) return 0 ;;
    esac
  done
  return 1
}

# Build exception set from ledger
declare -A EXCEPTION_MAP
if [[ -f "$EXCEPTIONS_FILE" ]]; then
  while IFS= read -r entry_file; do
    EXCEPTION_MAP["$entry_file"]=1
  done < <(python3 -c "
import json, sys
data = json.load(open('$EXCEPTIONS_FILE'))
for e in data.get('exceptions', []):
    print(e['file'])
" 2>/dev/null || true)
fi

# Collect all governed tracked files
violations=()
excepted=()

for ext in "${EXTENSIONS[@]}"; do
  while IFS= read -r file; do
    [[ -f "$file" ]] || continue
    is_excluded "$file" && continue
    lines=$(wc -l < "$file")
    if (( lines > LIMIT )); then
      if [[ -n "${EXCEPTION_MAP[$file]+_}" ]]; then
        excepted+=("$lines $file")
      else
        violations+=("$lines $file")
      fi
    fi
  done < <(git ls-files -- "$ext" 2>/dev/null || true)
done

# Sort and report
echo "=== Repo-wide line audit (limit=${LIMIT}) ==="
echo ""

if (( ${#violations[@]} > 0 )); then
  echo "VIOLATIONS (${#violations[@]} files above ${LIMIT} lines, NOT in exception ledger):"
  printf '%s\n' "${violations[@]}" | sort -rn | while read -r count path; do
    printf "  %-6s  %s\n" "$count" "$path"
  done
  echo ""
fi

if (( ${#excepted[@]} > 0 )); then
  echo "EXCEPTED (${#excepted[@]} files above ${LIMIT} lines, tracked in ledger):"
  printf '%s\n' "${excepted[@]}" | sort -rn | while read -r count path; do
    printf "  %-6s  %s\n" "$count" "$path"
  done
  echo ""
fi

total=$(( ${#violations[@]} + ${#excepted[@]} ))
echo "Summary: ${#violations[@]} unexcepted violation(s), ${#excepted[@]} excepted, ${total} total above ${LIMIT} lines."

if (( STRICT == 1 && ${#violations[@]} > 0 )); then
  echo ""
  echo "BLOCKED (--strict): ${#violations[@]} file(s) exceed ${LIMIT} lines and are not in ${EXCEPTIONS_FILE}."
  echo "Add them to ${EXCEPTIONS_FILE} with owner/reason/removal_batch/expiry, or reduce them below ${LIMIT} lines."
  exit 1
fi

exit 0
