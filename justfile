# Game Project Task Runner
# Usage: just <recipe> [args]

# Import engine-specific recipes if available
import? 'justfile.engine'

# Default: list available recipes
default:
    @just --list

# --- Setup ---

# Install dependencies, configure hooks, verify tooling
setup:
    #!/usr/bin/env bash
    set -euo pipefail
    uv sync
    git config core.hooksPath .githooks
    git lfs install
    echo "--- Python dependencies installed, git hooks configured, LFS active ---"
    # Warn if Unity is not reachable
    if [ -z "${UNITY_PATH:-}" ] && ! command -v Unity >/dev/null 2>&1; then
        echo "WARNING: UNITY_PATH is not set and Unity is not on PATH."
        echo "  Set UNITY_PATH to your Unity installation, e.g.:"
        echo "    export UNITY_PATH=\"/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity\""
    fi
    echo "--- Setup complete ---"

# One-command project initialization (interactive)
quick-start:
    #!/usr/bin/env bash
    set -euo pipefail
    echo "🎮 mcgoats-game-template Quick Start"
    echo ""
    echo "Available engines:"
    echo "  1) godot   — Godot 4.x (GDScript) [Full support]"
    echo "  2) unity   — Unity (C#) [Basic stubs]"
    echo "  3) unreal  — Unreal Engine (C++) [Empty stubs]"
    echo ""
    read -p "Select engine (godot/unity/unreal): " engine
    if [[ -z "$engine" ]]; then
        echo "No engine selected. Aborting."
        exit 1
    fi
    echo ""
    echo "Running setup..."
    bash tools/setup-engine.sh "$engine"
    echo ""
    echo "Installing Python dependencies..."
    uv sync
    echo ""
    echo "Configuring git hooks..."
    git config core.hooksPath .githooks
    chmod +x .githooks/* .claude/hooks/* .claude/*.sh 2>/dev/null || true
    echo ""
    echo "✅ Quick start complete!"
    echo ""
    echo "Next steps:"
    echo "  1. Set up GitHub branch protection and MERGE_TOKEN secret"
    echo "     Run: /dev:init-project for a guided walkthrough"
    echo "  2. Customize README.md and CLAUDE.md for your project"
    echo "  3. Start building: just worktree-create first-feature"

# --- Linting ---

# Lint Python scripts
python-lint:
    uv run ruff check scripts/tools/

# Format-check Python scripts
python-format-check:
    uv run ruff format --check scripts/tools/

# Auto-format Python scripts
python-format:
    uv run ruff format scripts/tools/

# Auto-format then lint (pre-push check)
check: python-lint validate-registry validate-docs validate-frontmatter

# Validate system manifests (file existence, ownership, dependencies)
validate-registry:
    uv run python scripts/tools/validate_registry.py

# Validate YAML frontmatter in loader-consumed markdown (commands + skills)
validate-frontmatter:
    uv run python scripts/tools/validate_frontmatter.py

# --- Testing ---

# Show test coverage report
test-coverage:
    uv run python scripts/tools/test_coverage_report.py

# CI coverage check: compare against baseline
test-coverage-ci:
    uv run python scripts/tools/test_coverage_report.py --ci

# Full preflight: lint + validate (run before pushing)
preflight: python-lint validate-registry validate-docs validate-frontmatter
    @echo "--- Preflight passed ---"

# --- Changelog ---

# Generate changelog from conventional commits
changelog:
    git-cliff -o CHANGELOG.md
    @echo "--- CHANGELOG.md updated ---"

# Preview upcoming changelog (unreleased commits)
changelog-preview:
    git-cliff --unreleased

# --- Release ---

# Create a tagged release from main
release version:
    #!/usr/bin/env bash
    set -euo pipefail
    echo "Releasing {{ version }}..."
    BRANCH=$(git branch --show-current)
    if [ "$BRANCH" != "main" ]; then
        echo "ERROR: Releases must be created from main (currently on $BRANCH)"
        exit 1
    fi
    if [ -n "$(git status --porcelain)" ]; then
        echo "ERROR: Working tree is dirty. Commit or stash changes first."
        exit 1
    fi
    git-cliff --tag "v{{ version }}" -o CHANGELOG.md
    git add CHANGELOG.md
    ALLOW_MASTER_COMMIT=1 git commit -m "chore: release {{ version }}"
    git tag -a "v{{ version }}" -m "Release {{ version }}"
    echo "Tagged v{{ version }}. Run 'ALLOW_MASTER_PUSH=1 git push && git push --tags' to trigger CI."

# --- Worktree Management ---

# Create an isolated worktree for a task (branch: feat/<task> from origin/main)
worktree-create task:
    #!/usr/bin/env bash
    set -euo pipefail
    BRANCH="feat/{{ task }}"
    # Use parent directory naming convention: <project>-<task>
    PROJECT_NAME=$(basename "$(pwd)")
    WORKTREE_DIR="../${PROJECT_NAME}-{{ task }}"
    echo "Pulling latest main from remote..."
    git fetch origin main
    git update-ref refs/heads/main refs/remotes/origin/main
    if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
        echo "Branch $BRANCH already exists. Checking out in worktree..."
        git worktree add "$WORKTREE_DIR" "$BRANCH"
    else
        git worktree add -b "$BRANCH" "$WORKTREE_DIR" origin/main
    fi
    # Tag as active (signals "in progress, DO NOT delete")
    TAG_COMMIT=$(git -C "$WORKTREE_DIR" rev-parse HEAD)
    git tag -f "wt/active/{{ task }}" "$TAG_COMMIT" 2>/dev/null || true
    git push origin "wt/active/{{ task }}" --force 2>/dev/null || true
    # Clean up any stale done tag from a previous run of this task
    git tag -d "wt/done/{{ task }}" 2>/dev/null || true
    git push origin --delete "wt/done/{{ task }}" 2>/dev/null || true
    echo "--- Worktree created at $WORKTREE_DIR (branch: $BRANCH) ---"
    echo "Tagged wt/active/{{ task }} (worktree protected from cleanup)"
    echo "cd $WORKTREE_DIR to start working"

# Remove a task's worktree and delete its local + remote branch
worktree-cleanup task:
    #!/usr/bin/env bash
    set -euo pipefail
    BRANCH="feat/{{ task }}"
    PROJECT_NAME=$(basename "$(pwd)")
    WORKTREE_DIR="../${PROJECT_NAME}-{{ task }}"
    # Safety check: abort if worktree is still marked active (unless FORCE is set)
    if [ -z "${FORCE:-}" ]; then
        ACTIVE_LOCAL=$(git tag -l "wt/active/{{ task }}")
        ACTIVE_REMOTE=$(git ls-remote --tags origin "refs/tags/wt/active/{{ task }}" 2>/dev/null | head -1 || true)
        if [ -n "$ACTIVE_LOCAL" ] || [ -n "$ACTIVE_REMOTE" ]; then
            echo "ERROR: Branch $BRANCH is still marked active (wt/active/{{ task }} tag exists)."
            echo "Run: just worktree-mark-done {{ task }}"
            echo "Or override with: FORCE=1 just worktree-cleanup {{ task }}"
            exit 1
        fi
    fi
    if [ -d "$WORKTREE_DIR" ]; then
        git worktree remove "$WORKTREE_DIR" --force 2>/dev/null || true
        echo "Removed worktree $WORKTREE_DIR"
    fi
    if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
        git branch -D "$BRANCH" 2>/dev/null || true
        echo "Deleted local branch $BRANCH"
    fi
    if git ls-remote --exit-code --heads origin "$BRANCH" >/dev/null 2>&1; then
        git push origin --delete "$BRANCH" 2>/dev/null || true
        echo "Deleted remote branch $BRANCH"
    fi
    # Clean up lifecycle tags
    git tag -d "wt/active/{{ task }}" 2>/dev/null || true
    git tag -d "wt/done/{{ task }}" 2>/dev/null || true
    git push origin --delete "wt/active/{{ task }}" 2>/dev/null || true
    git push origin --delete "wt/done/{{ task }}" 2>/dev/null || true
    git fetch origin main --quiet 2>/dev/null || true
    git update-ref refs/heads/main refs/remotes/origin/main 2>/dev/null || true
    echo "Local main -> $(git rev-parse --short origin/main 2>/dev/null || echo '?')"
    echo "--- Cleanup complete for {{ task }} ---"

# Prune orphaned worktrees and branches with gone remotes
worktree-cleanup-all:
    #!/usr/bin/env bash
    set -euo pipefail
    echo "Pruning orphaned worktrees..."
    git worktree prune
    echo "Fetching remote state..."
    git fetch origin --prune
    GONE_BRANCHES=$(git branch -vv | grep ': gone]' | awk '{print $1}' || true)
    if [ -n "$GONE_BRANCHES" ]; then
        echo "Deleting branches with gone remotes:"
        echo "$GONE_BRANCHES"
        echo "$GONE_BRANCHES" | xargs git branch -D 2>/dev/null || true
    else
        echo "No stale branches found."
    fi
    git update-ref refs/heads/main refs/remotes/origin/main 2>/dev/null || true
    echo "Local main -> $(git rev-parse --short origin/main 2>/dev/null || echo '?')"
    echo "--- Cleanup-all complete ---"

# Sync main, delete merged branches, prune worktrees (tag-aware)
worktree-sync:
    #!/usr/bin/env bash
    set -euo pipefail
    echo "Fetching latest main and tags..."
    git fetch origin main --tags --prune-tags
    git update-ref refs/heads/main refs/remotes/origin/main
    MAIN_SHA=$(git rev-parse --short origin/main)
    echo "Local main -> $MAIN_SHA"
    # Auto-cleanup worktrees/branches marked as done
    DONE_TASKS=$(git tag -l 'wt/done/*' | sed 's|wt/done/||' || true)
    DONE_COUNT=0
    if [ -n "$DONE_TASKS" ]; then
        echo ""
        echo "=== Auto-cleaning completed worktrees (wt/done/*) ==="
        for TASK in $DONE_TASKS; do
            BRANCH="feat/$TASK"
            PROJECT_NAME=$(basename "$(pwd)")
            WT_DIR="../${PROJECT_NAME}-${TASK}"
            if [ -d "$WT_DIR" ]; then
                git worktree remove "$WT_DIR" --force 2>/dev/null || true
                echo "  Removed worktree: $WT_DIR"
            fi
            if git show-ref --verify --quiet "refs/heads/$BRANCH"; then
                git branch -D "$BRANCH" 2>/dev/null || true
                echo "  Deleted local branch: $BRANCH"
            fi
            # Clean up lifecycle tags
            git tag -d "wt/active/$TASK" 2>/dev/null || true
            git tag -d "wt/done/$TASK" 2>/dev/null || true
            git push origin --delete "wt/active/$TASK" 2>/dev/null || true
            git push origin --delete "wt/done/$TASK" 2>/dev/null || true
            DONE_COUNT=$((DONE_COUNT + 1))
        done
    fi
    # Report active worktrees (do not touch)
    ACTIVE_TASKS=$(git tag -l 'wt/active/*' | sed 's|wt/active/||' || true)
    if [ -n "$ACTIVE_TASKS" ]; then
        echo ""
        echo "=== In-progress worktrees (wt/active/*) — not touched ==="
        for TASK in $ACTIVE_TASKS; do
            echo "  feat/$TASK"
        done
    fi
    # Find untagged branches (legacy)
    MERGED=$(git branch --merged origin/main | grep -vE '^\*|main' | sed 's/^[ \t]*//' || true)
    UNTAGGED=""
    if [ -n "$MERGED" ]; then
        for BRANCH in $MERGED; do
            TASK=$(echo "$BRANCH" | sed 's|^feat/||')
            ACTIVE_TAG=$(git tag -l "wt/active/$TASK")
            DONE_TAG=$(git tag -l "wt/done/$TASK")
            if [ -z "$ACTIVE_TAG" ] && [ -z "$DONE_TAG" ]; then
                UNTAGGED="${UNTAGGED}  $BRANCH\n"
                git branch -d "$BRANCH" 2>/dev/null || true
            fi
        done
    fi
    if [ -n "$UNTAGGED" ]; then
        echo ""
        echo "=== Untagged merged branches (legacy) — deleted ==="
        echo -e "$UNTAGGED"
    fi
    git worktree prune
    echo ""
    echo "=== Sync Summary ==="
    echo "  Completed (cleaned): $DONE_COUNT"
    echo "  Active (protected):  $(echo "$ACTIVE_TASKS" | grep -c . 2>/dev/null || echo 0)"
    echo "--- Sync complete ---"

# List all worktrees with branch name, tag status, and PR status
worktree-list:
    #!/usr/bin/env bash
    set -euo pipefail
    git fetch origin main --quiet 2>/dev/null || true
    git fetch origin --tags --prune-tags --quiet 2>/dev/null || true
    git update-ref refs/heads/main refs/remotes/origin/main 2>/dev/null || true
    MAIN_SHA=$(git rev-parse origin/main 2>/dev/null || git rev-parse main)
    printf "%-45s %-30s %-10s %-6s %s\n" "WORKTREE" "BRANCH" "TAG" "BEHIND" "PR STATUS"
    printf "%-45s %-30s %-10s %-6s %s\n" "--------" "------" "---" "------" "---------"
    git worktree list --porcelain | while IFS= read -r line; do
        case "$line" in
            "worktree "*)
                WT_PATH="${line#worktree }"
                ;;
            "branch "*)
                BRANCH="${line#branch refs/heads/}"
                BEHIND=$(git rev-list --count "$BRANCH..${MAIN_SHA}" 2>/dev/null || echo "?")
                # Determine tag status
                TAG_STATUS=""
                if [ "$BRANCH" != "main" ]; then
                    TASK=$(echo "$BRANCH" | sed 's|^feat/||')
                    if git tag -l "wt/active/$TASK" | grep -q .; then
                        TAG_STATUS="ACTIVE"
                    elif git tag -l "wt/done/$TASK" | grep -q .; then
                        TAG_STATUS="DONE"
                    else
                        TAG_STATUS="UNTAGGED"
                    fi
                fi
                PR_STATUS=""
                if [ "$BRANCH" != "main" ] && command -v gh >/dev/null 2>&1; then
                    PR_JSON=$(gh pr list --head "$BRANCH" --state all --json number,state --limit 1 2>/dev/null || echo "[]")
                    PR_NUM=$(echo "$PR_JSON" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['number'] if d else '')" 2>/dev/null || echo "")
                    if [ -n "$PR_NUM" ]; then
                        PR_STATE=$(echo "$PR_JSON" | python3 -c "import sys,json; print(json.load(sys.stdin)[0]['state'])" 2>/dev/null || echo "?")
                        PR_STATUS="#${PR_NUM} ${PR_STATE}"
                    else
                        PR_STATUS="NO PR"
                    fi
                fi
                printf "%-45s %-30s %-10s %-6s %s\n" "$WT_PATH" "$BRANCH" "$TAG_STATUS" "$BEHIND" "$PR_STATUS"
                ;;
        esac
    done

# Mark a task as done (transition wt/active -> wt/done after PR merges)
worktree-mark-done task:
    #!/usr/bin/env bash
    set -euo pipefail
    BRANCH="feat/{{ task }}"
    # Verify the PR is merged
    MERGED_JSON=$(gh pr list --head "$BRANCH" --state merged --json number --limit 1 2>/dev/null || echo "[]")
    MERGED_NUM=$(echo "$MERGED_JSON" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['number'] if d else '')" 2>/dev/null || echo "")
    if [ -z "$MERGED_NUM" ]; then
        echo "ERROR: No merged PR found for branch $BRANCH."
        echo "The PR must be merged before marking as done."
        echo "Check: gh pr list --head $BRANCH --state all"
        exit 1
    fi
    echo "PR #$MERGED_NUM is merged. Transitioning tags..."
    # Delete active tag (local + remote, ignore errors if already gone)
    git tag -d "wt/active/{{ task }}" 2>/dev/null || true
    git push origin --delete "wt/active/{{ task }}" 2>/dev/null || true
    # Create done tag on current HEAD
    git tag -f "wt/done/{{ task }}" HEAD 2>/dev/null || true
    git push origin "wt/done/{{ task }}" --force 2>/dev/null || true
    echo "--- Tagged wt/done/{{ task }} — safe to clean up ---"
    echo "Run: just worktree-cleanup {{ task }}"

# Mark a task as abandoned (safe to delete even without merged PR)
worktree-mark-abandoned task:
    #!/usr/bin/env bash
    set -euo pipefail
    echo "WARNING: Marking {{ task }} as abandoned. This allows cleanup to delete the branch"
    echo "even though the PR was not merged. Any unmerged work will be lost."
    # Delete active tag
    git tag -d "wt/active/{{ task }}" 2>/dev/null || true
    git push origin --delete "wt/active/{{ task }}" 2>/dev/null || true
    # Create done tag
    git tag -f "wt/done/{{ task }}" HEAD 2>/dev/null || true
    git push origin "wt/done/{{ task }}" --force 2>/dev/null || true
    echo "--- Tagged wt/done/{{ task }} — safe to clean up ---"
    echo "Run: just worktree-cleanup {{ task }}"

# Detect ghost worktree tags with no matching branch
worktree-audit:
    bash scripts/tools/worktree-audit.sh

# --- Agent Lifecycle ---

# Subagent lifecycle: init or ship
lifecycle *args:
    bash scripts/tools/subagent-lifecycle.sh {{args}}

# Main agent: verify merge, clean up everything
task-complete task:
    bash scripts/tools/task-complete.sh {{task}}

# Assert audit on all test files
assert-audit:
    uv run python scripts/tools/assert_audit.py --all

# --- Local Verification Queue ---
queue-submit branch:
    bash scripts/tools/unity-queue.sh submit {{branch}}
queue-run:
    bash scripts/tools/unity-queue.sh run
queue-run-all:
    bash scripts/tools/unity-queue.sh run-all
queue-promote branch:
    bash scripts/tools/unity-queue.sh promote {{branch}}
queue-status:
    bash scripts/tools/unity-queue.sh status
queue-init:
    bash scripts/tools/unity-queue.sh init-verifier

# --- Local Fast-Forward ---

# Fast-forward local main to current branch after successful push (tested commits only)
ff-main:
    #!/usr/bin/env bash
    set -euo pipefail
    BRANCH=$(git branch --show-current)
    # Safety: must be on a feature branch
    if [[ "$BRANCH" == "main" || -z "$BRANCH" ]]; then
        echo "ERROR: Must be on a feature branch, not '$BRANCH'" >&2
        exit 1
    fi
    # Safety: feature branch must be pushed to origin
    REMOTE_REF=$(git ls-remote --heads origin "$BRANCH" 2>/dev/null | head -1)
    if [[ -z "$REMOTE_REF" ]]; then
        echo "ERROR: Branch '$BRANCH' not pushed to origin. Push first (tests must pass)." >&2
        exit 1
    fi
    # Safety: main must be ancestor of HEAD (fast-forward only)
    if ! git merge-base --is-ancestor main HEAD 2>/dev/null; then
        echo "ERROR: main is not an ancestor of HEAD. Rebase onto main first." >&2
        exit 1
    fi
    # Fast-forward local main ref to current HEAD
    OLD_SHA=$(git rev-parse --short main)
    git update-ref refs/heads/main HEAD
    NEW_SHA=$(git rev-parse --short main)
    echo "ff-main: local main fast-forwarded $OLD_SHA -> $NEW_SHA"
    echo "  Branch: $BRANCH"
    echo "  Note: Remote main unchanged — PR still required for official merge."

# --- Fast Iteration ---

# Push current branch and create PR in one command
pr title="":
    #!/usr/bin/env bash
    set -euo pipefail
    BRANCH=$(git branch --show-current)
    if [ "$BRANCH" = "main" ]; then
        echo "ERROR: Cannot create PR from main. Use a feature branch."
        exit 1
    fi
    echo "Rebasing onto latest main..."
    git fetch origin main --quiet
    if ! git rebase origin/main; then
        echo "ERROR: Rebase failed. Resolve conflicts, then retry."
        git rebase --abort 2>/dev/null || true
        exit 1
    fi
    echo "Pushing $BRANCH..."
    git push --force-with-lease -u origin "$BRANCH"
    EXISTING=$(gh pr list --head "$BRANCH" --state open --json number --limit 1 2>/dev/null || echo "[]")
    EXISTING_NUM=$(echo "$EXISTING" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['number'] if d else '')" 2>/dev/null || echo "")
    if [ -n "$EXISTING_NUM" ]; then
        PR_URL=$(gh pr view "$EXISTING_NUM" --json url -q '.url')
        echo "--- PR #${EXISTING_NUM} already exists (updated): $PR_URL ---"
        exit 0
    fi
    PR_TITLE="{{ title }}"
    if [ -z "$PR_TITLE" ]; then
        PR_TITLE=$(echo "$BRANCH" | sed 's|^feat/||' | tr '-' ' ' | tr '_' ' ')
    fi
    echo "Creating PR..."
    PR_BODY="## Summary\nFeature branch\n\n## Test plan\n- [ ] CI passes\n- [ ] Tests written and passing"
    gh pr create --base main --title "$PR_TITLE" --body "$(echo -e "$PR_BODY")"
    NEW_PR_NUM=$(gh pr list --head "$BRANCH" --state open --json number -q '.[0].number' 2>/dev/null || echo "")
    if [ -n "$NEW_PR_NUM" ]; then
        gh pr merge "$NEW_PR_NUM" --auto --squash 2>/dev/null || true
        echo "Auto-merge enabled for PR #${NEW_PR_NUM}"
    fi
    echo "--- PR created. Auto-merge enabled. ---"

# Watch CI status for current branch
watch-ci:
    #!/usr/bin/env bash
    set -euo pipefail
    BRANCH=$(git branch --show-current)
    echo "Finding latest CI run for $BRANCH..."
    RUN_ID=""
    for i in 1 2 3; do
        RUN_ID=$(gh run list --branch "$BRANCH" --limit 1 --json databaseId,status -q '.[0].databaseId' 2>/dev/null || echo "")
        if [ -n "$RUN_ID" ]; then break; fi
        echo "Waiting for CI run to appear..."
        sleep 3
    done
    if [ -z "$RUN_ID" ]; then
        echo "No CI runs found for branch $BRANCH. Push first?"
        exit 1
    fi
    echo "Watching run $RUN_ID..."
    gh run watch "$RUN_ID" --exit-status && echo "--- CI PASSED ---" || echo "--- CI FAILED --- Run: just ci-log"

# Show failed CI log for current branch
ci-log:
    #!/usr/bin/env bash
    set -euo pipefail
    BRANCH=$(git branch --show-current)
    RUN_ID=$(gh run list --branch "$BRANCH" --limit 1 --json databaseId -q '.[0].databaseId' 2>/dev/null || echo "")
    if [ -z "$RUN_ID" ]; then
        echo "No CI runs found for branch $BRANCH."
        exit 1
    fi
    echo "=== Failed logs for run $RUN_ID ==="
    gh run view "$RUN_ID" --log-failed

# One-command: rebase, push, create PR, watch CI
ship title="":
    #!/usr/bin/env bash
    set -euo pipefail
    echo "=== SHIP IT ==="
    just pr "{{ title }}"
    echo ""
    just watch-ci
    echo ""
    BRANCH=$(git branch --show-current)
    echo "Waiting for PR to merge..."
    for i in $(seq 1 20); do
        STATE=$(gh pr view --json state -q '.state' 2>/dev/null || echo "UNKNOWN")
        if [ "$STATE" = "MERGED" ]; then
            echo "--- PR MERGED ---"
            exit 0
        elif [ "$STATE" = "CLOSED" ]; then
            echo "--- PR was CLOSED (not merged) ---"
            exit 1
        fi
        sleep 15
    done
    echo "--- PR still open after 5 minutes. Check merge queue status. ---"

# --- C# Lint ---

# Fast C# lint: syntax check + policy lint on changed files (no Unity)
lint-fast:
	#!/usr/bin/env bash
	set -euo pipefail
	STAGED_CS=$(git diff --cached --name-only --diff-filter=ACM -- '*.cs' 2>/dev/null || true)
	ALL_CHANGED_CS=$(bash scripts/tools/get_changed_files.sh --changed origin/main --cs 2>/dev/null || true)
	# Use staged for pre-commit context, changed-against-main for broader checks
	CS_FILES="${STAGED_CS:-$ALL_CHANGED_CS}"
	if [ -z "$CS_FILES" ]; then
	    echo "lint-fast: no .cs changes detected — skipping"
	    exit 0
	fi
	echo "lint-fast: checking $(echo "$CS_FILES" | wc -l | tr -d ' ') C# file(s)..."
	# 1. Portable syntax check
	echo "=== Syntax check ==="
	echo "$CS_FILES" | while IFS= read -r f; do
	    [ -f "$f" ] && bash scripts/tools/syntax-check-csharp.sh "$f"
	done
	# 2. dotnet format --verify-no-changes (if dotnet is installed)
	if command -v dotnet >/dev/null 2>&1; then
	    echo "=== dotnet format check ==="
	    echo "$CS_FILES" | tr '\n' ' ' | xargs dotnet format r8eo-x-unity.sln --verify-no-changes --include || {
	        echo "BLOCKED: dotnet format violations. Run: dotnet format r8eo-x-unity.sln --include <files>"
	        exit 1
	    }
	fi
	# 3. Registry validation
	echo "=== Registry validation ==="
	uv run python scripts/tools/validate_registry.py
	# 4. Policy lint on staged/changed files
	echo "=== Policy lint ==="
	if echo "$CS_FILES" | grep -q '.'; then
	    uv run python scripts/tools/lint_csharp_policy.py --staged || {
	        echo "BLOCKED: Policy violations found. See output above."
	        exit 1
	    }
	fi
	echo "lint-fast: all checks passed"

# Full-repo C# static lint (all files, no Unity)
lint-csharp:
	#!/usr/bin/env bash
	set -euo pipefail
	echo "=== Syntax check (all) ==="
	bash scripts/tools/syntax-check-csharp.sh --all
	if command -v dotnet >/dev/null 2>&1; then
	    echo "=== dotnet format check (full solution) ==="
	    dotnet format r8eo-x-unity.sln --verify-no-changes || {
	        echo "Run: dotnet format r8eo-x-unity.sln"
	        exit 1
	    }
	else
	    echo "dotnet not installed — skipping dotnet format check"
	fi
	echo "lint-csharp: done"

# Repo-policy lint: Debug.Log, FindObject, GUID, orphan manifest checks
lint-policy:
	uv run python scripts/tools/lint_csharp_policy.py --all

# Advisory asset/scene lint via Unity batchmode (non-blocking — exit 0 on findings)
lint-assets:
	#!/usr/bin/env bash
	set -euo pipefail
	if [ -z "${UNITY_PATH:-}" ]; then
	    echo "lint-assets: UNITY_PATH not set — skipping asset lint"
	    echo "  Set UNITY_PATH to your Unity installation to enable asset lint."
	    exit 0
	fi
	if [ ! -f "$UNITY_PATH" ]; then
	    echo "lint-assets: Unity binary not found at UNITY_PATH=$UNITY_PATH — skipping"
	    exit 0
	fi
	mkdir -p Logs
	echo "lint-assets: running Unity asset lint..."
	"$UNITY_PATH" \
	    -batchmode \
	    -nographics \
	    -quit \
	    -projectPath "$(pwd)" \
	    -executeMethod R8EOX.Editor.AssetLintRunner.RunFromCommandLine \
	    -logFile Logs/unity_lint.log \
	    2>&1 | tail -20 || true
	echo "lint-assets: report written to Logs/asset_lint_report.json"
	echo "lint-assets: done (exit 0 — findings are advisory)"
	exit 0

# Deep lint: full C# lint + registry + assert audit + advisory asset lint
lint-deep: lint-csharp validate-registry lint-policy lint-assets
	#!/usr/bin/env bash
	set -euo pipefail
	echo "=== Assert audit ==="
	uv run python scripts/tools/assert_audit.py --all
	echo "lint-deep: all checks complete"

# Check CLAUDE.md freshness
validate-docs:
    uv run python scripts/tools/validate_claude_md.py

# Check CLAUDE.md freshness (CI mode)
validate-docs-ci:
    uv run python scripts/tools/validate_claude_md.py --ci --threshold 30

# Audit skill usage over recent git history
audit-skills days="30":
    bash tools/audit-skill-usage.sh {{days}}

# Check for template upstream drift
check-template-sync:
    #!/usr/bin/env bash
    set -euo pipefail
    if ! git remote | grep -q template; then
        echo "No 'template' remote configured."
        echo "Add it: git remote add template https://github.com/Totes-MickGOATs/mcgoats-game-template.git"
        exit 0
    fi
    git fetch template main --quiet 2>/dev/null || { echo "Could not fetch template remote."; exit 0; }
    BEHIND=$(git rev-list --count HEAD..template/main 2>/dev/null || echo "0")
    if [ "$BEHIND" -gt 0 ]; then
        echo "Template has $BEHIND new commit(s). Consider merging:"
        echo "  git fetch template && git merge template/main --no-ff"
        git log --oneline HEAD..template/main | head -10
    else
        echo "Up to date with template."
    fi

