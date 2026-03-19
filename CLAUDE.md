# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.

## BEFORE YOU WRITE ANY CODE — Read This First

> **MANDATORY PREREQUISITE:** You must be on a feature branch before writing, editing, or creating any file.
>
> **If you are a subagent:** Run `bash scripts/tools/safe-worktree-init.sh <task>` as your FIRST action. It prints `WORKTREE_PATH=/absolute/path`. Work exclusively in that worktree using absolute paths (prefix every `cd` and file operation with the worktree path). Never edit files in `$CLAUDE_PROJECT_DIR` (the main repo).
>
> **If you are the main agent:**
> 1. **Do NOT edit any files yet.** You are on `main` and commits will be blocked.
> 2. **Dispatch a subagent WITHOUT `isolation: "worktree"`** — the subagent calls `safe-worktree-init.sh` itself.
> 3. The subagent handles: worktree setup → code changes → commit → push → PR → CI green → label `ready-to-merge`.
> 4. You verify the PR merged successfully, then run `just worktree-cleanup <task>`.
>
> **Why this matters:** Three enforcement layers block main branch commits (PreToolUse hook, git pre-commit hook, GitHub branch protection). If you edit files on main, you'll waste work that can't be committed.
>
> **Full workflow guide:** `.agents/skills/branch-workflow/SKILL.md`

---

## Session Start — Self-Reflect Before Acting

> **MANDATORY:** Before writing code:
> 1. Read relevant memories from `MEMORY.md` and the local `CLAUDE.md` for the directory you're modifying.
> 2. Follow the Ask-First workflow (`.agents/skills/ask-first/SKILL.md`) — Interrogate -> Test-First -> Implement.
> 3. State your plan briefly before starting.

> **Why this matters:** Agents that skip reflection repeat mistakes that previous agents already solved. The 2 minutes spent reflecting saves 20 minutes of rework.

---

## First-Time Setup

If this is a fresh clone or a new project created from the template, see **`SETUP.md`** for:
- Required tool installation (git, gh, uv, just)
- Engine selection and configuration (`tools/setup-engine.sh`)
- GitHub repository protection rules and secrets
- Git hooks setup
- Or run `/dev:init-project` for an interactive guided walkthrough.

---

## Project Overview

<!-- ENGINE-SPECIFIC: Project description added by setup-engine.sh -->

**Game:** R8EO-X — A realistic RC Racing Simulator.

Current state: _Update `.ai/knowledge/status/project-status.md` with phase tracking._

<!-- ENGINE-SPECIFIC: Engine version, executable path, launch commands added by setup-engine.sh -->

## Git — Branch Workflow (STOP AND READ)

> **MANDATORY:** All changes go through feature branches + PRs. Direct commits/pushes to main are **blocked** by local hooks AND GitHub branch protection. You WILL get errors if you try.
>
> **Before writing ANY code**, read the workflow below. Full details + gotchas: `.agents/skills/branch-workflow/SKILL.md`

### Quick Reference

```
just lifecycle init <task>           # 1. Create feature branch + worktree (subagent first action)
# ... TDD develop, commit, test ...  # 2. Work on feat/<task> branch
just lifecycle ship                  # 3. Push, create PR, merge, sync main (all-in-one)
just task-complete <task>            # 4. Main agent cleanup after subagent merge
```

### Branch Workflow

1. **Create a worktree:** `just lifecycle init <task-name>` or `just worktree-create <task-name>` (creates `feat/<task>` from `origin/main`)
2. **Develop:** TDD cycle — write failing test, implement, commit. Commit message format: `type: short description`
3. **Ship (subagent):** `just lifecycle ship` — pushes, creates PR, merges, syncs local main
4. **Fast-forward local main (mid-dev only):** `just ff-main` (gives next task immediate access to your changes mid-development — NOT for post-merge sync)
5. **Cleanup (main agent):** `just task-complete <task>` — verifies merged PR, removes worktree, branch, tags
6. **Batch cleanup:** `just worktree-cleanup <task>` or `just worktree-sync`

> **No GitHub Actions** — all enforcement is local via hooks. PRs are created for audit trail and merged with `gh pr merge --squash`.

### Agent Protocol (Self-Managed Worktree Pattern)

> **HARD RULES FOR AGENTS:**
> - **NEVER commit on the `main` branch.** A PreToolUse hook will block you. Work on your feature branch.
> - **NEVER use `--no-verify`** on git commit. The hook will block this too. Fix the underlying issue instead.
> - **NEVER use `isolation: "worktree"`** — subagents call `safe-worktree-init.sh` themselves instead.
> - **ALWAYS provide task name** to subagents so they can call `bash scripts/tools/safe-worktree-init.sh <task>` as first action (or `just lifecycle init <task>`).
> - **ALWAYS set the `model` parameter** on every Agent tool call: `haiku` for Explore, `opus` for Plan, `sonnet` for general-purpose. Never omit it.
> - **NEVER leave your branch unmerged.** You own your branch from creation to merge. See Definition of Done below.

Subagent lifecycle (two commands):
1. `just lifecycle init <task>` → captures WORKTREE_PATH, all work uses absolute paths in that worktree
2. TDD develop in worktree → commit each file immediately
3. `just lifecycle ship` → push + create PR + squash-merge + sync local main
4. Main agent runs `just task-complete <task>` to clean up

### Sequential Task Coordination

When the main agent decomposes work into multiple tasks:
1. Dispatch ONE subagent at a time — each performs one atomic, non-breaking task
2. After each subagent reports back, update in-flight memories (`project_inflight_<task>.md`) with affected systems/files
3. Provide downstream subagents with in-flight context so they can peek at relevant branches
4. After each PR merges: remove in-flight memories, update permanent memories
5. Full protocol: `.agents/skills/swarm-development/SKILL.md` → Sequential Coordination Mode

### Keeping Branches Fresh

Freshness is enforced automatically by hooks — manual steps are belt-and-suspenders:

- **SessionStart hook** fetches all remote refs and updates local `main` to match `origin/main` (every session, every agent).
- **safe-worktree-init.sh** fetches `origin/main` and creates worktree branch from it (every subagent that calls the script as first action).
- **Main agent (manual):** `git fetch origin && git pull --ff-only origin main` before dispatching subagents.
- **Subagents (manual):** `just lifecycle ship` handles rebase automatically before push.

### Definition of Done

A task is **not done** until ALL of these are true:

1. **PR is open** with all commits pushed
2. **Local tests pass** — run relevant test files for your changes before shipping (coverage ratchet is a FAIL gate)
3. **PR merged** — `just lifecycle ship` handles merge; confirm with `gh pr view --json state -q .state`
4. **Local main updated** — `just lifecycle ship` syncs local main automatically
5. **Bulletproof quality checklist passed** — see `/dev:bulletproof` for the full process (Phases 0-5)
6. **Clean loop completed** — run `/dev:clean-loop` to capture lessons, update docs, and verify clean state

> **No CI gates** — GitHub Actions are disabled (cost constraint). All enforcement is local: pre-commit hook runs coverage ratchet and assert audit. Pre-push hook runs module-gated tests when UNITY_PATH is set. Agents are on the honor system for test criteria.

### Commit Rules

- Commit every file immediately after creating or updating/editing/writing to it, even if it's just a stub.
- Never leave uncommitted changes at the end of a task.
- Commit message format: `type: short description` (e.g., `feat: add surface friction zone`, `fix: correct wheel radius`).
- Conventional commit types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`, `perf`, `style`, `build`

### Master Protection (Three Layers)

- **Local hooks:** `.githooks/pre-commit` blocks commits on main, `.githooks/pre-push` blocks pushes to main
- **Claude hooks:** `.claude/hooks/` PreToolUse hooks block file edits on main
- **GitHub branch protection:** Required PR, required CI checks, no force push, no deletion
- **Bypass (release only):** `ALLOW_MASTER_COMMIT=1` / `ALLOW_MASTER_PUSH=1` environment variables

## Testing (TDD)

> **MANDATORY:** Write tests FIRST, run them, then implement. Never claim "fixed" without running tests.
> Full standards: `.ai/knowledge/architecture/coding-standards.md` | Skills: `unity-testing-patterns`, `clean-room-qa`
>
> **Enforcement reality:** TDD is a mandatory agent-discipline requirement, NOT a CI gate. CI enforces lint (`Lint & Preflight`), not test results. Tests must pass locally before you declare done — Unity isn't in PATH on CI, so pre-push doesn't run tests automatically. You are on the honor system for all test criteria.

**TDD Cycle:** Hypothesize -> Write failing test (RED) -> Implement (GREEN) -> Commit. Steps 2-3 are non-negotiable.

**Module-based test gating:** Pre-push detects changed `.cs` files, resolves affected modules and their transitive dependents via `scripts/tools/resolve_module_tests.py`, and reports which test classes would need to run. Tests are only executed if `UNITY_PATH` is set and the Unity binary exists on PATH — if Unity is not found, the hook reports the required tests but does not block the push. Bypass test execution entirely: `SKIP_TEST_CHECK=1 git push`. `ff-main` respects `SKIP_FF_TESTS=1 just ff-main`.

**Test Tiers:** Unit (every public method, 1 positive + 1 negative) + Integration (cross-system wiring) + E2E (user-facing features). All three required for user-facing changes.

**Test naming:** `MethodName_Scenario_ExpectedOutcome`

**Autonomous debugging:** Agents drive the entire debug cycle via MCP tools (`read_console`, `manage_editor`, `manage_components`). NEVER defer to user. Prefer unit tests over play-mode debugging when possible. When MCP disconnects during domain reload, wait and retry — don't hand off.

**Postmortems:** For significant bugs, save `postmortem_<name>.md` to memory with: issue summary, diagnostic signal, fix approach, reusable pattern.

**Local TDD only:** Run your test locally for red-green. Don't run full suite locally. CI runs post-merge. If post-merge tests fail, `/ci:next-fix` picks it up.

### Physics Conformance Audit

- Every physics change must be validated against the conformance check catalogue
- Use `ConformanceRecorder` to record results to the local SQLite DB
- Tagged debug logs (`[physics]`, `[grip]`, etc.) are auto-persisted by `DebugLogSink`
- See `.agents/skills/physics-conformance-audit/SKILL.md` for methodology

## Python Tooling (scripts/tools only, not game code)

- **Python 3.14** with **uv** as the package manager
- Virtual environment at `.venv/` (managed by uv)

```bash
uv sync                          # Install dependencies
uv add <package>                 # Add a dependency
uv run python <script.py>        # Run a script
```

## Architecture

<!-- ENGINE-SPECIFIC: Autoloads/singletons, scene structure, key scripts added by setup-engine.sh -->

### Conventions

<!-- ENGINE-SPECIFIC: Engine-specific conventions (coordinate system, physics, etc.) added by setup-engine.sh -->

- **Signal Up, Call Down** — global systems emit signals, children call methods on parents
- **No magic numbers** — use named constants for layer IDs, type enumerations, algorithm parameters
- **Type annotations** — always annotate function signatures and return types

## Coding Patterns

See `.ai/knowledge/architecture/coding-standards.md` for value mutability tiers (Const/Export/Settings/Dynamic), DRY extraction thresholds, declarative patterns, and code organization rules. No bare numeric literals — use named constants.

## RC Car Physics Domain

1/1 scale test: mass 15kg, wheel radius 1.66m, wheelbase 13.6m, track 10.0m (×10 of Godot 1/10 RC reference). Spring 75 N/m, damping 4.25 N·s/m (unchanged from Godot). Curve-sampled grip (NOT Pacejka).
Key invariants: suspension force >= 0, no grip without normal load, differential conserves force.
Full constants + invariants: `.ai/knowledge/architecture/adr-001-physics-model.md` | Skills: `unity-physics-3d`, `unity-physics-tuning`

## System Registry

> **MANDATORY:** Every game system must have a manifest in `resources/manifests/`.
> When adding files to a system, add them to that system's manifest. When creating a new system, create a manifest.
> When deprecating a system, set `status = DEPRECATED` and fill in `replaced_by`.

- **Manifests:** `resources/manifests/` — one per system, declares owned files, status, dependencies
- **Validation:** `just validate-registry` (CI) or runs on debug boot
- **Statuses:** ACTIVE (in use), DEPRECATED (replaced, do not modify), EXPERIMENTAL (WIP, not integrated)

## Documentation Self-Improvement

> **MANDATORY:** When editing or fixing code in any directory, review the local `CLAUDE.md` file.
> If the documentation is outdated, inaccurate, or missing information about the change you just made,
> update the `CLAUDE.md` in the same commit or a follow-up commit. This keeps documentation evergreen.

- Every non-hidden directory has a `CLAUDE.md` describing its contents and linking relevant skills
- Each `CLAUDE.md` has a "Relevant Skills" section pointing to `.agents/skills/` for lazy-loaded context
- When adding a new file to a directory, add it to that directory's `CLAUDE.md` file listing
- When removing a file, remove it from the listing (do not leave "removed" comments)
- When adding a new directory, create a `CLAUDE.md` for it with skill references
- Skill files live in `.agents/skills/<name>/SKILL.md` — reference them, don't duplicate their content
- **Automated:** `/dev:clean-loop` Step 2 performs this documentation sweep at end-of-task

## Session End — Self-Reflect and Self-Improve

> **MANDATORY:** Before reporting "done":
> 1. **Reflect:** What surprised me? What mistake did I make? What would I tell the next agent?
> 2. **Update docs:** Update local `CLAUDE.md` if stale. Add/remove files from listings.
> 3. **Maintain memories:** Add gotchas discovered, update affected memories, prune stale entries.
> 4. **Evolve:** Small improvement -> update CLAUDE.md. Reusable pattern -> update skill.

## Issue-Driven Workflow

> **PREFERRED:** File issues for non-trivial work (bug reports, features, CI failures).

**Agent workflow:** `/dev:next-task` -> work in worktree -> reference `#issue-number` in commits/PR -> PR merge auto-closes issue.

**Persistence:** Memory files store diagnostic patterns. Issue history provides queryable record. CLAUDE.md docs give immediate understanding. MCP reconnects automatically (wait 10-15s after script changes).

## Troubleshooting — Common Agent Issues

### MCP Disconnects After Script Changes

Unity domain reload temporarily disconnects MCP. This is normal.

**Recovery:**
1. Wait 10-15 seconds for domain reload to complete
2. Retry your MCP tool call
3. If still disconnected after 30s, use `/mcp` command in Claude Code to refresh
4. If still failing, check `read_console` for compilation errors blocking reload

**Do NOT** hand off to the user, assume MCP is broken, or skip verification steps.

### Compilation Errors After Script Changes

Always check compilation after creating or modifying C# scripts:
1. Call `read_console` and filter for errors
2. Common traps: `Physics` vs `UnityEngine.Physics` namespace collision, `Camera` ambiguity
3. Fix ALL errors before proceeding — Unity won't reload until compilation succeeds

## Key Reference Files

- `.agents/skills/branch-workflow/SKILL.md` — **Branch workflow, merge queue, worktree recipes, gotchas (READ FIRST)**
- `.ai/knowledge/architecture/system-overview.md` — scene graph, signal map, data flow, ADRs
- `.ai/knowledge/architecture/coding-standards.md` — coding conventions for this project
- `.ai/knowledge/status/project-status.md` — phase checklist, full scripts/scenes inventory
- `.agents/skills/feature-toggles/SKILL.md` — **Compile-time and runtime feature toggle patterns, decision matrix, active registry**
- `VERSIONING.md` — SemVer convention, release lifecycle, build types
- `SETUP.md` — first-time project setup guide

<!-- ENGINE-SPECIFIC: Engine-specific skill references added by setup-engine.sh -->
