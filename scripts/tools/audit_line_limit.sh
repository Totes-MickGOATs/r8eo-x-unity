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
EXTENSIONS="*.cs *.md *.json *.yml *.yaml *.txt *.asmdef *.ruleset *.uxml *.uss"

# Patterns to exclude (binary/generated/cache)
EXCLUDES="Library/ Logs/ Temp/ obj/ .meta Packages/packages-lock.json"

is_excluded() {
  local file="$1"
  for pat in $EXCLUDES; do
    case "$file" in
      *"$pat"*) return 0 ;;
    esac
  done
  return 1
}

# Build exception set as newline-delimited list via Python
EXCEPTION_LIST=""
if [[ -f "$EXCEPTIONS_FILE" ]]; then
  EXCEPTION_LIST=$(python3 -c "
import json, sys
try:
    data = json.load(open('$EXCEPTIONS_FILE'))
    for e in data.get('exceptions', []):
        print(e['file'])
except Exception:
    pass
" 2>/dev/null || true)
fi

is_excepted() {
  local file="$1"
  echo "$EXCEPTION_LIST" | grep -qxF "$file" 2>/dev/null
}

# Collect all governed tracked files
violations=""
excepted=""
violation_count=0
excepted_count=0

for ext in $EXTENSIONS; do
  while IFS= read -r file; do
    [[ -f "$file" ]] || continue
    is_excluded "$file" && continue
    lines=$(wc -l < "$file" | tr -d ' ')
    if (( lines > LIMIT )); then
      if is_excepted "$file"; then
        excepted="${excepted}${lines} ${file}"$'\n'
        excepted_count=$((excepted_count + 1))
      else
        violations="${violations}${lines} ${file}"$'\n'
        violation_count=$((violation_count + 1))
      fi
    fi
  done < <(git ls-files -- "$ext" 2>/dev/null || true)
done

# Sort and report
echo "=== Repo-wide line audit (limit=${LIMIT}) ==="
echo ""

if (( violation_count > 0 )); then
  echo "VIOLATIONS (${violation_count} files above ${LIMIT} lines, NOT in exception ledger):"
  printf '%s' "$violations" | sort -rn | while IFS= read -r entry; do
    [[ -z "$entry" ]] && continue
    count="${entry%% *}"
    path="${entry#* }"
    printf "  %-6s  %s\n" "$count" "$path"
  done
  echo ""
fi

if (( excepted_count > 0 )); then
  echo "EXCEPTED (${excepted_count} files above ${LIMIT} lines, tracked in ledger):"
  printf '%s' "$excepted" | sort -rn | while IFS= read -r entry; do
    [[ -z "$entry" ]] && continue
    count="${entry%% *}"
    path="${entry#* }"
    printf "  %-6s  %s\n" "$count" "$path"
  done
  echo ""
fi

total=$(( violation_count + excepted_count ))
echo "Summary: ${violation_count} unexcepted violation(s), ${excepted_count} excepted, ${total} total above ${LIMIT} lines."

if (( STRICT == 1 && violation_count > 0 )); then
  echo ""
  echo "BLOCKED (--strict): ${violation_count} file(s) exceed ${LIMIT} lines and are not in ${EXCEPTIONS_FILE}."
  echo "Add them to ${EXCEPTIONS_FILE} with owner/reason/removal_batch/expiry, or reduce them below ${LIMIT} lines."
  exit 1
fi

exit 0
