#!/usr/bin/env bash
# check_line_limit.sh — Enforce 150-line limit repo-wide.
# Replaces the old 200-line C#-only check. Delegates to audit_line_limit.sh
# which uses the exception ledger at .line-limit-exceptions.json.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec bash "$SCRIPT_DIR/audit_line_limit.sh" --strict "$@"
