#!/bin/bash
# Audit which skills have been referenced in recent git history.
# Usage: bash tools/audit-skill-usage.sh [days]
#   days — lookback window (default: 30)

set -euo pipefail

DAYS="${1:-30}"
SINCE="$(date -d "$DAYS days ago" +%Y-%m-%d 2>/dev/null || date -v-"${DAYS}"d +%Y-%m-%d 2>/dev/null || echo "")"

if [ -z "$SINCE" ]; then
    echo "ERROR: Could not compute date $DAYS days ago. Falling back to --all."
    SINCE=""
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_DIR"

SKILLS_DIR=".agents/skills"

echo "=== Skill Usage Audit ==="
echo "Lookback: ${DAYS} days (since ${SINCE:-all history})"
echo ""

# Collect all skill names
SKILL_NAMES=()
if [ -d "$SKILLS_DIR" ]; then
    for skill_dir in "$SKILLS_DIR"/*/; do
        if [ -f "${skill_dir}SKILL.md" ]; then
            name="$(basename "$skill_dir")"
            SKILL_NAMES+=("$name")
        fi
    done
fi

if [ ${#SKILL_NAMES[@]} -eq 0 ]; then
    echo "No skills found in $SKILLS_DIR."
    exit 0
fi

echo "Total skills found: ${#SKILL_NAMES[@]}"
echo ""

# Build git log args
GIT_LOG_ARGS=(log --all --oneline)
if [ -n "$SINCE" ]; then
    GIT_LOG_ARGS+=(--since="$SINCE")
fi

# Get commit messages from the period
COMMIT_LOG=$(git "${GIT_LOG_ARGS[@]}" 2>/dev/null || echo "")

# Also scan PR bodies via gh if available
PR_BODIES=""
if command -v gh &>/dev/null; then
    PR_BODIES=$(gh pr list --state all --limit 50 --json title,body \
        -q '.[].title + " " + .body' 2>/dev/null || echo "")
fi

# Combine all searchable text
SEARCHABLE="${COMMIT_LOG}
${PR_BODIES}"

# Check each skill
USED=()
UNUSED=()

for skill in "${SKILL_NAMES[@]}"; do
    if echo "$SEARCHABLE" | grep -qi "$skill"; then
        USED+=("$skill")
    else
        UNUSED+=("$skill")
    fi
done

# Report
echo "--- Referenced in last ${DAYS} days (${#USED[@]}) ---"
if [ ${#USED[@]} -gt 0 ]; then
    for s in "${USED[@]}"; do
        echo "  [USED]   $s"
    done
else
    echo "  (none)"
fi

echo ""
echo "--- Not referenced (${#UNUSED[@]}) ---"
if [ ${#UNUSED[@]} -gt 0 ]; then
    for s in "${UNUSED[@]}"; do
        echo "  [UNUSED] $s"
    done
else
    echo "  (none — all skills were referenced!)"
fi

echo ""
echo "=== End Audit ==="

exit 0
