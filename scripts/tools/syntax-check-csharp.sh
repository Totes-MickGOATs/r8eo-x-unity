#!/usr/bin/env bash
# syntax-check-csharp.sh — Lightweight pre-commit syntax checks for C# files
# Not a compiler — catches ~60% of typo-level errors before CI.
# Runs on staged .cs files by default, or all .cs files with --all.
#
# Usage:
#   bash scripts/tools/syntax-check-csharp.sh          # staged files only
#   bash scripts/tools/syntax-check-csharp.sh --all     # all .cs files
#   bash scripts/tools/syntax-check-csharp.sh file.cs   # specific file
set -euo pipefail

errors=0
warnings=0

check_file() {
  local file="$1"
  local basename
  basename="$(basename "$file")"

  if [[ ! -f "$file" ]]; then
    return
  fi

  local lines
  lines=$(wc -l < "$file")

  # 1. Balanced braces
  local open_braces close_braces
  open_braces=$(grep -o '{' "$file" | wc -l | tr -d ' ')
  close_braces=$(grep -o '}' "$file" | wc -l | tr -d ' ')
  if (( open_braces != close_braces )); then
    echo "ERROR: $file — unbalanced braces (open=$open_braces close=$close_braces)"
    errors=$((errors + 1))
  fi

  # 2. Balanced parentheses
  local open_parens close_parens
  open_parens=$(grep -o '(' "$file" | wc -l | tr -d ' ')
  close_parens=$(grep -o ')' "$file" | wc -l | tr -d ' ')
  if (( open_parens != close_parens )); then
    echo "ERROR: $file — unbalanced parentheses (open=$open_parens close=$close_parens)"
    errors=$((errors + 1))
  fi

  # 3. Namespace declaration present (skip .asmdef and auto-generated)
  if [[ "$basename" != *.asmdef ]] && [[ "$basename" != "AssemblyInfo.cs" ]]; then
    if ! grep -q "^namespace " "$file" && ! grep -q "^    namespace " "$file"; then
      # Check if it's a top-level statement file or has using directives (likely needs namespace)
      if grep -q "class \|struct \|interface \|enum " "$file"; then
        echo "WARN:  $file — no namespace declaration found"
        warnings=$((warnings + 1))
      fi
    fi
  fi

  # 4. Duplicate class/struct/interface names across file set (deferred — needs context)

  # 5. File length check (200-line limit)
  if (( lines > 200 )); then
    # Allowlist auto-generated files
    case "$basename" in
      R8EOXInputActions.cs) ;;
      *)
        echo "WARN:  $file — $lines lines (exceeds 200-line limit)"
        warnings=$((warnings + 1))
        ;;
    esac
  fi

  # 6. Common typos: doubled semicolons, missing using
  if grep -nP ';;(?!\s*//)' "$file" | head -3 | grep -q .; then
    echo "WARN:  $file — doubled semicolons found"
    warnings=$((warnings + 1))
  fi
}

# Determine which files to check
files=()
if [[ "${1:-}" == "--all" ]]; then
  while IFS= read -r -d '' f; do
    files+=("$f")
  done < <(find Assets/Scripts Assets/Tests -name '*.cs' -print0 2>/dev/null)
elif [[ -n "${1:-}" ]] && [[ -f "${1:-}" ]]; then
  files=("$1")
else
  # Staged .cs files
  while IFS= read -r f; do
    [[ "$f" == *.cs ]] && files+=("$f")
  done < <(git diff --cached --name-only --diff-filter=ACM 2>/dev/null || true)
fi

if (( ${#files[@]} == 0 )); then
  echo "No .cs files to check."
  exit 0
fi

echo "Checking ${#files[@]} C# file(s)..."
for f in "${files[@]}"; do
  check_file "$f"
done

echo ""
echo "${#files[@]} file(s) checked: $errors error(s), $warnings warning(s)"

if (( errors > 0 )); then
  exit 1
fi
exit 0
