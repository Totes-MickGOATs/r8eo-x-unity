#!/usr/bin/env bash
# unity-queue.sh — Local verification queue for serialized Unity compile/test jobs
# Serializes Unity compile+test jobs through a dedicated verifier worktree.
#
# Usage:
#   unity-queue.sh submit <branch>      — add branch to queue
#   unity-queue.sh run                  — process next queued item
#   unity-queue.sh run-all              — process all queued items
#   unity-queue.sh promote <branch>     — fast-forward local main if branch passed
#   unity-queue.sh status               — show queue state + verifier status
#   unity-queue.sh init-verifier        — create verifier worktree
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)"
PROJECT_NAME="$(basename "$REPO_ROOT")"
VERIFIER_DIR="$(dirname "$REPO_ROOT")/${PROJECT_NAME}-verifier"

QUEUE_DIR="${REPO_ROOT}/Logs/automation"
QUEUE_FILE="${QUEUE_DIR}/queue.json"
RESULTS_DIR="${QUEUE_DIR}/results"

UNITY_BIN="${UNITY_PATH:-}"

# ── Helpers ─────────────────────────────────────────────────────────────────

die() { echo "ERROR: $*" >&2; exit 1; }

require_python3() {
  command -v python3 >/dev/null 2>&1 || die "python3 is required but not found"
}

ensure_queue_dir() {
  mkdir -p "$QUEUE_DIR" "$RESULTS_DIR"
  if [ ! -f "$QUEUE_FILE" ]; then
    python3 -c "import json; print(json.dumps({'queue': [], 'results': {}}, indent=2))" > "$QUEUE_FILE"
  fi
}

queue_read() {
  require_python3
  ensure_queue_dir
  python3 -c "
import json, sys
with open('${QUEUE_FILE}') as f:
    data = json.load(f)
print(json.dumps(data))
"
}

queue_write() {
  local json_data="$1"
  require_python3
  ensure_queue_dir
  python3 -c "
import json, sys
data = json.loads(sys.argv[1])
with open('${QUEUE_FILE}', 'w') as f:
    json.dump(data, f, indent=2)
    f.write('\n')
" "$json_data"
}

queue_add_branch() {
  local branch="$1"
  require_python3
  ensure_queue_dir
  python3 -c "
import json, sys, time
branch = sys.argv[1]
with open('${QUEUE_FILE}') as f:
    data = json.load(f)
# Remove any existing entry for this branch first
data['queue'] = [e for e in data['queue'] if e.get('branch') != branch]
data['queue'].append({'branch': branch, 'submitted_at': int(time.time()), 'status': 'queued'})
with open('${QUEUE_FILE}', 'w') as f:
    json.dump(data, f, indent=2)
    f.write('\n')
print('queued: ' + branch)
" "$branch"
}

queue_next() {
  require_python3
  ensure_queue_dir
  python3 -c "
import json
with open('${QUEUE_FILE}') as f:
    data = json.load(f)
for entry in data['queue']:
    if entry.get('status') == 'queued':
        print(entry['branch'])
        break
"
}

queue_set_status() {
  local branch="$1"
  local status="$2"  # running|passed|failed
  require_python3
  python3 -c "
import json, sys, time
branch, status = sys.argv[1], sys.argv[2]
with open('${QUEUE_FILE}') as f:
    data = json.load(f)
for entry in data['queue']:
    if entry.get('branch') == branch:
        entry['status'] = status
        entry['updated_at'] = int(time.time())
        break
data['results'][branch] = {'status': status, 'updated_at': int(time.time())}
with open('${QUEUE_FILE}', 'w') as f:
    json.dump(data, f, indent=2)
    f.write('\n')
" "$branch" "$status"
}

queue_get_result() {
  local branch="$1"
  require_python3
  python3 -c "
import json, sys
branch = sys.argv[1]
with open('${QUEUE_FILE}') as f:
    data = json.load(f)
result = data.get('results', {}).get(branch, {})
print(result.get('status', 'unknown'))
" "$branch"
}

check_lockfile() {
  if [ -f "${REPO_ROOT}/Temp/UnityLockfile" ]; then
    echo "ERROR: Unity is currently running (Temp/UnityLockfile exists). Close the editor first." >&2
    echo "  If Unity crashed, remove: ${REPO_ROOT}/Temp/UnityLockfile" >&2
    exit 1
  fi
  if [ -f "${VERIFIER_DIR}/Temp/UnityLockfile" ] 2>/dev/null; then
    echo "ERROR: Unity is running in verifier worktree (${VERIFIER_DIR}/Temp/UnityLockfile exists)." >&2
    exit 1
  fi
}

# ── Commands ─────────────────────────────────────────────────────────────────

cmd_submit() {
  local branch="${1:?Usage: unity-queue.sh submit <branch>}"
  require_python3
  ensure_queue_dir
  # Validate branch exists
  if ! git -C "$REPO_ROOT" show-ref --verify --quiet "refs/heads/${branch}" 2>/dev/null; then
    die "Branch '${branch}' does not exist locally."
  fi
  queue_add_branch "$branch"
  echo "unity-queue submit: '${branch}' added to queue"
  echo "  Queue file: ${QUEUE_FILE}"
  echo "  Run: bash scripts/tools/unity-queue.sh run"
}

cmd_init_verifier() {
  if [ -d "$VERIFIER_DIR" ]; then
    echo "unity-queue init-verifier: verifier already exists at ${VERIFIER_DIR}"
    echo "  To recreate: rm -rf '${VERIFIER_DIR}' then re-run"
    return 0
  fi

  echo "unity-queue init-verifier: creating verifier worktree at ${VERIFIER_DIR}..."
  # Create detached worktree (no branch checkout — will be switched per job)
  git -C "$REPO_ROOT" worktree add --detach "$VERIFIER_DIR" HEAD
  # Verifier gets its OWN Library — do NOT symlink to main
  mkdir -p "${VERIFIER_DIR}/Library"
  echo "unity-queue init-verifier: done"
  echo "  Verifier: ${VERIFIER_DIR}"
  echo "  Library:  ${VERIFIER_DIR}/Library (own copy — not symlinked)"
  echo "  NOTE: First Unity run will be slow (Library import). Subsequent runs are fast."
}

cmd_run() {
  require_python3
  ensure_queue_dir

  local branch
  branch="$(queue_next)"
  if [ -z "$branch" ]; then
    echo "unity-queue run: queue is empty — nothing to process"
    return 0
  fi

  echo "unity-queue run: processing '${branch}'..."

  # Ensure verifier exists
  if [ ! -d "$VERIFIER_DIR" ]; then
    echo "unity-queue run: verifier not found — running init-verifier first..."
    cmd_init_verifier
  fi

  # Check for Unity lockfile
  check_lockfile

  # Mark as running
  queue_set_status "$branch" "running"

  # Checkout branch in verifier
  echo "unity-queue run: checking out '${branch}' in verifier..."
  git -C "$VERIFIER_DIR" checkout "$branch" 2>/dev/null || {
    echo "ERROR: Could not checkout '${branch}' in verifier" >&2
    queue_set_status "$branch" "failed"
    exit 1
  }

  local result_file="${RESULTS_DIR}/${branch//\//_}.log"
  local exit_code=0

  # ── Phase 1: Compile check ──────────────────────────────────────────────
  if [ -n "$UNITY_BIN" ]; then
    echo "unity-queue run: compile check..."
    "$UNITY_BIN" \
      -batchmode \
      -nographics \
      -accept-apiupdate \
      -projectPath "$VERIFIER_DIR" \
      -quit \
      -logFile "${result_file}.compile" \
      2>&1 || exit_code=$?

    if [ $exit_code -ne 0 ]; then
      echo "unity-queue run: COMPILE FAILED for '${branch}'"
      echo "  Log: ${result_file}.compile"
      queue_set_status "$branch" "failed"
      cat "${result_file}.compile" >> "$result_file" 2>/dev/null || true
      return 1
    fi
    echo "unity-queue run: compile OK"
  else
    echo "unity-queue run: SKIP compile check (UNITY_PATH not set)"
  fi

  # ── Phase 2: Targeted EditMode tests ────────────────────────────────────
  if [ -n "$UNITY_BIN" ]; then
    echo "unity-queue run: resolving affected test modules..."
    local changed_cs
    changed_cs=$(git -C "$VERIFIER_DIR" diff --name-only main..HEAD 2>/dev/null | grep '\.cs$' || true)

    if [ -n "$changed_cs" ]; then
      local test_filter
      test_filter=$(echo "$changed_cs" | uv run python "${REPO_ROOT}/scripts/tools/resolve_module_tests.py" \
        --format unity-filter --editmode-only 2>/dev/null || true)

      if [ -n "$test_filter" ]; then
        echo "unity-queue run: running targeted tests (filter: ${test_filter})..."
        "$UNITY_BIN" \
          -batchmode \
          -nographics \
          -accept-apiupdate \
          -runTests \
          -testPlatform EditMode \
          -projectPath "$VERIFIER_DIR" \
          -testFilter "$test_filter" \
          -testResults "${RESULTS_DIR}/${branch//\//_}-editmode.xml" \
          -logFile "${result_file}.tests" \
          2>&1 || exit_code=$?

        if [ $exit_code -ne 0 ]; then
          echo "unity-queue run: TESTS FAILED for '${branch}'"
          echo "  Log: ${result_file}.tests"
          echo "  Results: ${RESULTS_DIR}/${branch//\//_}-editmode.xml"
          queue_set_status "$branch" "failed"
          cat "${result_file}.tests" >> "$result_file" 2>/dev/null || true
          return 1
        fi
        echo "unity-queue run: tests PASSED"
      else
        echo "unity-queue run: no affected test modules — skipping test run"
      fi
    else
      echo "unity-queue run: no changed .cs files — skipping test run"
    fi
  else
    echo "unity-queue run: SKIP tests (UNITY_PATH not set)"
  fi

  # ── Mark passed ──────────────────────────────────────────────────────────
  echo "unity-queue run: '${branch}' PASSED"
  queue_set_status "$branch" "passed"
  echo "  Promote with: bash scripts/tools/unity-queue.sh promote ${branch}"
}

cmd_run_all() {
  require_python3
  ensure_queue_dir

  local processed=0
  while true; do
    local branch
    branch="$(queue_next)"
    [ -z "$branch" ] && break
    cmd_run
    processed=$((processed + 1))
  done

  if [ $processed -eq 0 ]; then
    echo "unity-queue run-all: queue was empty — nothing processed"
  else
    echo "unity-queue run-all: processed ${processed} item(s)"
  fi
}

cmd_promote() {
  local branch="${1:?Usage: unity-queue.sh promote <branch>}"
  require_python3
  ensure_queue_dir

  local result
  result="$(queue_get_result "$branch")"

  if [ "$result" != "passed" ]; then
    echo "ERROR: Branch '${branch}' has not passed verification (status: ${result})" >&2
    echo "  Run: bash scripts/tools/unity-queue.sh run" >&2
    exit 1
  fi

  # Verify branch is ancestor of main or can fast-forward
  if ! git -C "$REPO_ROOT" merge-base --is-ancestor main "$branch" 2>/dev/null; then
    echo "ERROR: main is not an ancestor of '${branch}'. Rebase first." >&2
    exit 1
  fi

  local old_sha
  old_sha=$(git -C "$REPO_ROOT" rev-parse --short main)
  git -C "$REPO_ROOT" update-ref refs/heads/main "refs/heads/${branch}"
  local new_sha
  new_sha=$(git -C "$REPO_ROOT" rev-parse --short main)

  echo "unity-queue promote: local main fast-forwarded ${old_sha} -> ${new_sha}"
  echo "  Branch: ${branch}"
  echo "  Note: Remote main unchanged — use 'just lifecycle ship' for remote fallback."
}

cmd_status() {
  require_python3
  ensure_queue_dir

  echo "=== Unity Verification Queue ==="
  echo "  Queue file: ${QUEUE_FILE}"
  echo "  Verifier:   ${VERIFIER_DIR} $([ -d "$VERIFIER_DIR" ] && echo '(exists)' || echo '(not created)')"
  echo "  UNITY_PATH: ${UNITY_BIN:-'(not set — compile/test steps will be skipped)'}"
  echo ""

  python3 -c "
import json, time
with open('${QUEUE_FILE}') as f:
    data = json.load(f)

queue = data.get('queue', [])
if not queue:
    print('Queue: empty')
else:
    print(f'Queue: {len(queue)} item(s)')
    for entry in queue:
        age = int(time.time()) - entry.get('submitted_at', 0)
        age_str = f'{age // 3600}h {(age % 3600) // 60}m' if age > 60 else f'{age}s'
        print(f\"  [{entry['status']:8s}] {entry['branch']} (submitted {age_str} ago)\")

results = data.get('results', {})
passed = [b for b, r in results.items() if r.get('status') == 'passed']
failed = [b for b, r in results.items() if r.get('status') == 'failed']
if passed:
    print(f'')
    print(f'Passed ({len(passed)}):')
    for b in passed:
        print(f'  + {b}')
if failed:
    print(f'')
    print(f'Failed ({len(failed)}):')
    for b in failed:
        print(f'  x {b}')
"
}

# ── Dispatch ─────────────────────────────────────────────────────────────────

cmd="${1:-}"
shift || true

case "$cmd" in
  submit)        cmd_submit "$@" ;;
  run)           cmd_run ;;
  run-all)       cmd_run_all ;;
  promote)       cmd_promote "$@" ;;
  status)        cmd_status ;;
  init-verifier) cmd_init_verifier ;;
  *)
    echo "Usage: unity-queue.sh <command> [args]" >&2
    echo "" >&2
    echo "Commands:" >&2
    echo "  submit <branch>      Add branch to verification queue" >&2
    echo "  run                  Process next queued item" >&2
    echo "  run-all              Process all queued items" >&2
    echo "  promote <branch>     Fast-forward local main (branch must have passed)" >&2
    echo "  status               Show queue state and verifier status" >&2
    echo "  init-verifier        Create verifier worktree" >&2
    exit 1
    ;;
esac
