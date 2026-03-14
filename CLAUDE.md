# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.

## BEFORE YOU WRITE ANY CODE — Read This First

> **MANDATORY PREREQUISITE:** You must be on a feature branch before writing, editing, or creating any file.
>
> **If you are a subagent spawned with `isolation: "worktree"`:** You are already on a feature branch. Verify with `git branch --show-current` — it should show `feat/...` or `.claude/...`. Proceed to implementation.
>
> **If you are the main agent or were NOT spawned with `isolation: "worktree"`:**
> 1. **Do NOT edit any files yet.** You are on `main` and commits will be blocked.
> 2. **Dispatch a subagent with `isolation: "worktree"`** to do the implementation work.
> 3. The subagent handles: code changes → commit → push → PR → CI green → label `ready-to-merge`.
> 4. You verify the PR merged successfully, then run `just worktree-cleanup <task>`.
>
> **Why this matters:** Three enforcement layers block main branch commits (PreToolUse hook, git pre-commit hook, GitHub branch protection). If you edit files on main, you'll waste work that can't be committed.
>
> **Full workflow guide:** `.agents/skills/branch-workflow/SKILL.md`

---

## Session Start — Self-Reflect Before Acting

> **MANDATORY:** At the beginning of every session or task, before writing any code, perform this reflection.

1. **Read relevant memories.** Scan `MEMORY.md` for topic files and feedback memories related to the current task. If the user's request touches a system with known gotchas (e.g., physics, terrain, audio, CI), read that memory file.
2. **Check for past failures.** Search memories for feedback entries about mistakes, missed patterns, or corrections the user gave. Ask yourself: "Has an agent been burned by this before? What went wrong and how was it fixed?"
3. **Flag stale memories.** If any memory you read looks outdated, incorrect, or references deleted files/old patterns — note it for cleanup during Session End. Don't let bad information persist.
4. **Review the relevant CLAUDE.md files.** Read the `CLAUDE.md` in the directory you're about to modify. It may contain warnings, conventions, or recent changes that affect your approach.
5. **Identify risks.** Based on your reflection, list any gotchas or non-obvious constraints that apply.
6. **State your plan briefly** before starting implementation, incorporating lessons from steps 1-5.
7. **Run the Ask-First workflow.** For any dev task (bug fix, feature, refactor), read and follow `.agents/skills/ask-first/SKILL.md`. This is the mandatory three-phase workflow: Interrogate -> Test-First -> Implement. Do NOT skip this — it prevents agents from charging ahead on misunderstood requirements and ensures tests are written by a separate agent with no implementation bias.

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
just worktree-create <task>          # 1. Create feature branch + worktree (tags wt/active/<task>)
# ... develop, commit, test ...      # 2. Work on feat/<task> branch
git push -u origin feat/<task>       # 3. Push
gh pr create --base main             # 4. Open PR → CI runs automatically
# CI auto-labels ready-to-merge      # 5. Auto-merge triggers on label → squash-merges
just worktree-mark-done <task>       # 6. After PR merges, transition tag to wt/done/<task>
just worktree-cleanup <task>         # 7. Clean up worktree, branches, and tags
```

### Branch Workflow

1. **Create a worktree:** `just worktree-create <task-name>` (creates `feat/<task>` from `origin/main`)
2. **Develop:** Commit frequently in the feature branch. Commit message format: `type: short description`
3. **Push + PR:** `git push -u origin feat/<task>` then `gh pr create --base main`
4. **CI validates:** Lint & Preflight must pass (tests are advisory unless configured otherwise)
5. **Merge:** CI auto-adds `ready-to-merge` label → auto-merge workflow squash-merges when up-to-date
6. **Mark done:** `just worktree-mark-done <task>` (transitions `wt/active` → `wt/done` tag, requires merged PR)
7. **Cleanup:** `just worktree-cleanup <task>` or `just worktree-sync` for batch cleanup. Cleanup is blocked if `wt/active` tag exists (override with `FORCE=1`)

### Agent Protocol (Claude Code agents with `isolation: "worktree"`)

> **HARD RULES FOR AGENTS:**
> - **NEVER commit on the `main` branch.** A PreToolUse hook will block you. Work on your feature branch.
> - **NEVER use `--no-verify`** on git commit. The hook will block this too. Fix the underlying issue instead.
> - **ALWAYS use `isolation: "worktree"`** when spawning subagents that write code.
> - **NEVER leave your branch unmerged or CI failing.** You own your branch from creation to merge. See Definition of Done below.

Agents follow the same workflow: develop in worktree → push → PR → label `ready-to-merge` → auto-merge serializes. The worktree is created automatically by Claude Code — start at step 2.

### Dispatching Subagents (Main Agent Responsibility)

When you receive a task that requires code changes:

1. **Do NOT start editing files.** You are likely on `main`.
2. **Dispatch a subagent** with `isolation: "worktree"` to handle implementation.
3. **The subagent is responsible for the full lifecycle:** code → test → commit → push → PR → CI green → `ready-to-merge` label.
4. **After the subagent completes**, verify the PR was created and CI is green.
5. **Mark done:** `just worktree-mark-done <task>` once the PR merges (auto-handled by hooks in most cases).
6. **Cleanup:** `just worktree-cleanup <task>` once marked done. Session-start hook auto-cleans done worktrees.

**Common mistake:** Starting to edit files directly, then discovering you can't commit because you're on main. Always dispatch first, edit never.

### Keeping Branches Fresh

**Main agent (on main):** Before dispatching subagents, pull the latest remote main so worktrees start from the newest commit:

```bash
git fetch origin && git pull --ff-only origin main
```

**Subagents (on feature branches):** Before pushing, rebase onto the latest main to incorporate changes that landed while you were working:

```bash
git fetch origin && git rebase origin/main
```

If rebase conflicts occur, resolve them before pushing. Do not push a branch that is behind `origin/main` when commits have landed since your worktree was created.

### Definition of Done

A task is **not done** until ALL of these are true:

1. **PR is open** with all commits pushed
2. **Lint CI is green** — `Lint & Preflight` passes on GitHub Actions
3. **Auto-merge is enabled** — `gh pr merge --auto --squash` or `ready-to-merge` label applied
4. **Local tests pass** — run relevant test files for your changes before pushing
5. **Bulletproof quality checklist passed** — see `/dev:bulletproof` for the full process (Phases 0-5)
6. **Clean loop completed** — run `/dev:clean-loop` to capture lessons, update docs, and verify clean state

**You do NOT need to wait for merge.** Once lint CI is green and auto-merge is enabled, your task is done. The merge queue handles the rest. Move on to the next task.

If lint CI fails after you push, you are responsible for:
- Checking the CI output: `gh run view --log-failed` or `gh run view <run-id> --log`
- Diagnosing and fixing the failure in your feature branch
- Pushing the fix and confirming lint CI goes green

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

> **MANDATORY:** Write tests FIRST, run them, then implement. Tests MUST be executed — never claim "fixed" or "verified" without running tests. No exceptions.
>
> **Pre-implementation workflow:** Before writing any tests or code, complete the Ask-First workflow (`.agents/skills/ask-first/SKILL.md`). Tests must be written by a separate black-box agent — see Phase 2 of the Ask-First skill.

<!-- ENGINE-SPECIFIC: Test framework, test directory layout, and runner commands added by setup-engine.sh -->

### TDD Cycle (Red-Green-Commit)

Every bug fix or feature MUST follow this exact cycle. No steps may be skipped.

1. **Hypothesize** — Identify the potential cause of the issue or the behavior to implement
2. **Write a failing test** — Write a test that confirms your hypothesis (demonstrates the bug or specifies the desired behavior)
3. **Run the test → confirm RED** — Execute the test and verify it fails for the expected reason. If it passes, your hypothesis is wrong — revise it
4. **Implement the fix/feature** — Write the minimum code to make the test pass
5. **Run the test → confirm GREEN** — Execute the test and verify it now passes. If it still fails, iterate on the implementation
6. **Commit** — Tests and implementation together (or tests first if independent)

> **CRITICAL:** Steps 3 and 5 are non-negotiable. A test that was never run proves nothing. An implementation that was never verified against a test is not done. Agents that skip test execution are violating this project's core development practice.

### Test Tiers — Unit AND Integration AND E2E

Most changes require ALL THREE test tiers. This is non-negotiable.

| Tier | What it tests | When required | Minimum coverage |
|------|--------------|---------------|-----------------|
| **Unit** | Single function/class in isolation, mocked dependencies | Always — every public method | **1 positive + 1 negative test per method** |
| **Integration** | Multiple systems working together at runtime | When the change involves wiring, signals, or cross-system interaction | **1 test per cross-class interaction path** |
| **E2E** | Full game running, real scene tree | Every user-facing feature or behavior change | **1 test per feature/behavior** |

- **Positive test:** Verifies correct behavior with valid input (happy path)
- **Negative test:** Verifies correct handling of invalid/edge/boundary input (zero, null, out-of-range, NaN)
- **Unit tests** verify the logic is correct in isolation
- **Integration tests** verify the wiring is correct at runtime — signals connected, nodes found, systems interacting properly
- **E2E tests** verify the complete user-facing behavior from input to visible outcome in PlayMode
- A unit test passing does NOT mean the feature works in-game. If the change involves system wiring, write an integration test too
- When in doubt, write more tests. It is better to over-test than to ship a "tested" feature that breaks at runtime
- **Test naming:** `MethodName_Scenario_ExpectedOutcome` — must read like a sentence

### Autonomous Debugging — NEVER Defer to User

> **MANDATORY:** Agents MUST drive the entire debug → test → fix → verify cycle autonomously.
> Never ask the user to manually attach components, enter play mode, read logs, or perform any
> step that can be done via MCP tools or scripts. You have the tools — use them.

**Debug workflow (fully autonomous):**

1. **Read console** — `read_console` to check for errors/warnings
2. **Attach debug components** — `manage_components(action="add")` to add temporary debug scripts
3. **Enter play mode** — `manage_editor(action="play")` to start the game
4. **Wait for data** — sleep 3-5 seconds for logs to accumulate
5. **Read results** — `read_console` to capture debug output
6. **Stop play mode** — `manage_editor(action="stop")`
7. **Clean up** — remove temporary debug scripts

**When MCP is unresponsive** (domain reload after script changes):
- Wait and retry (sleep 10-15s between attempts)
- Do NOT hand off to the user — the editor will come back
- If truly stuck after 60s, fall back to unit tests to verify hypotheses

**Prefer unit tests over play-mode debugging:**
- If the bug can be reproduced in a unit test, write the test FIRST
- Unit tests are faster, more reliable, and don't require MCP connectivity
- Use play-mode debugging only for issues that require runtime wiring (scene tree, physics, rendering)

### Postmortems — Learning from Significant Bugs

When fixing a significant bug (Blocker/Major severity, or one that required novel diagnosis):
1. Save a postmortem memory with: issue summary, diagnostic signal, fix approach, reusable pattern
2. These memories help future sessions recognize similar problems faster
3. Format: `postmortem_<short_name>.md` in the memory directory

### Testing Strategy: Local TDD, Post-Merge Safety Net

> **IMPORTANT:** Run tests locally during TDD. The full suite runs automatically post-merge on main.

**Do NOT run the full test suite locally.** It ties up the developer's machine. Instead:

1. Write your test, run it locally for the red-green TDD cycle
2. Push to your feature branch — lint CI runs automatically
3. Once lint passes, auto-merge handles the rest
4. Full test suite runs post-merge on main
5. If post-merge tests fail, a `ci-failure` issue is auto-created — run `/ci:next-fix` to pick it up

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

## Value Mutability Tiers

When deciding how to declare a value, use the appropriate tier:

| Tier | Mechanism | When to Use | Example |
|------|-----------|-------------|---------|
| **Const** | Language constant / `static readonly` | Algorithm logic, physics math, layer IDs, enum values — never changes at runtime | Layer bitmasks, string identifiers |
| **Export** | Inspector-editable field | Per-instance tuning set in the editor — varies between scenes/nodes but fixed at runtime | Spring stiffness, difficulty tier |
| **Settings** | Settings manager / config file | User preferences persisted to disk — changed via Options menu | Graphics quality, audio volumes, input bindings |
| **Dynamic** | Runtime variable | Computed or changed every frame/event — driven by gameplay | Current speed, input vectors, health points |

- **Central constant classes** for collision layers, surface types, or other domain enumerations
- **Never use bare numeric literals** for values that have a named constant

## DRY / Declarative Coding Patterns

> **MANDATORY:** Prefer declarative data structures over imperative boilerplate. When you see 3+ instances
> of the same pattern (UI creation, setup wiring, validation loops), extract a shared helper or use a
> data-driven approach.

### When to Extract

- **3+ instances** of the same creation/validation/setup pattern → extract a helper
- **200+ lines** of match/if arms for property routing → use a bridge/descriptor pattern
- **10+ signal connections** between two nodes → use a wiring table
- **5+ setup methods** in an orchestrator → consider a subsystem registry
- Adding a new item should require **1 data entry**, not touching UI/logic code

### Declarative Patterns

| Pattern | When to Use |
|---------|-------------|
| **Data Model → Renderer** | Complex UI driven by configuration (tuning panels, option menus) |
| **Property Bridge** | Multi-target property routing (replaces match/if chains) |
| **Signal Wiring Table** | Connecting 5+ signals between two nodes |
| **Subsystem Registry** | Orchestrators that init 5+ subsystems (editors, main scene) |
| **Validation Runner** | Test loops that check arrays of items against rules |
| **Extracted Processors** | Pipeline steps that can be unit-tested independently |

## RC Car Physics Domain

> **Context for all agents:** These constants and invariants apply to every physics-related system in the project. Reference them when writing or reviewing physics code.

### Physical Constants (1/10 Scale RC Car)

| Constant | Value | Unit | Notes |
|----------|-------|------|-------|
| Vehicle mass | 1.5 | kg | Typical 1/10 buggy with battery |
| Wheel radius | 0.166 | m | Standard 1/10 buggy tire |
| Gravity | 9.81 | m/s² | Standard Earth gravity |
| Weight per wheel | ~3.68 | N | mass × gravity / 4 |
| Wheelbase | ~0.28 | m | Front-to-rear axle |
| Track width | ~0.24 | m | Left-to-right wheel |

### Physics Invariants (MUST hold in all code)

- **Suspension force ≥ 0** — suspension compresses but NEVER pulls (no tension)
- **Lateral force opposes lateral velocity** — restores straight-line travel
- **Differential conserves force** — left_share + right_share = total_input
- **No grip without normal load** — zero suspension force = zero tire grip
- **Throttle in air → nose pitches UP** — wheel spin reaction torque
- **Brake in air → nose pitches DOWN**
- **Gyroscopic stabilization** — spinning wheels resist tumbling

### Relevant Skills

- **`unity-physics-3d`** — Rigidbody, colliders, raycasting, WheelCollider
- **`clean-room-qa`** — Black-box physics testing from domain first principles

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

> **MANDATORY:** At the end of every task or session, before reporting "done", perform this reflection.

### 1. Reflect on What Happened

- **What surprised me?** Unexpected behavior, undocumented constraints, gotchas discovered the hard way.
- **What mistake did I make (or almost make)?** Wrong API usage, missed a project rule, off-by-one.
- **What took longer than expected?** Usually means documentation or code comments are missing.
- **What would I tell the next agent working on this system?** That's exactly what should be documented.

### 2. Update Documentation

- Update the local `CLAUDE.md` if it's stale or missing info about your change
- Add new files to directory CLAUDE.md file listings, remove deleted files
- Reference relevant skills from `.agents/skills/` — don't duplicate skill content

### 3. Improve Code Comments Where You Struggled

- Add comments for non-obvious constraints, workarounds, "why not the obvious approach"
- Update stale/misleading comments based on what you now know
- Comments explain **why**, not **what**

### 4. Maintain Memories

- **Add** feedback/project memories for gotchas discovered this session
- **Update** memories your work affects (bug fixed that a memory warns about → update it)
- **Prune** stale entries — remove links to deleted files, resolved items
- **Verify** new memory files are indexed in MEMORY.md

### 5. Evolve Strategies

- Small improvement → update relevant CLAUDE.md
- Reusable pattern → update or create a skill in `.agents/skills/`
- Process friction → update workflow docs

> **The goal:** Every agent session leaves the codebase smarter. A bug fix adds a test, updates docs, saves a memory. This compounds into self-improving documentation.

## Issue-Driven Workflow

> **PREFERRED:** File issues for non-trivial work. This creates persistence, audit trails,
> and allows agents to pick up work across sessions without losing context.

### When to Create Issues
- Bug reports with diagnostic data (console logs, audit output)
- Feature requests with acceptance criteria
- Gameplay/physics feel issues with controller info and references
- CI failures (auto-created by CI Monitor)

### Agent Workflow
1. **Pick up work:** `/dev:next-task` reads open issues sorted by priority
2. **Work in worktree:** Agent creates feature branch, implements fix/feature
3. **Reference the issue:** Commit messages and PR descriptions reference `#issue-number`
4. **Close with context:** PR merge auto-closes the issue. Resolution details in PR body.
5. **Postmortem (significant bugs):** Save diagnostic pattern to memory for future reference

### Persistence Across Sessions
- **Memory files** store diagnostic patterns, user preferences, project context
- **Issue history** provides queryable record of past fixes and their approaches
- **CLAUDE.md docs** give every new session immediate project understanding
- **MCP reconnection** is handled automatically — see recovery procedure in memory

## Key Reference Files

- `.agents/skills/branch-workflow/SKILL.md` — **Branch workflow, merge queue, worktree recipes, gotchas (READ FIRST)**
- `.ai/knowledge/architecture/system-overview.md` — scene graph, signal map, data flow, ADRs
- `.ai/knowledge/architecture/coding-standards.md` — coding conventions for this project
- `.ai/knowledge/status/project-status.md` — phase checklist, full scripts/scenes inventory
- `VERSIONING.md` — SemVer convention, release lifecycle, build types
- `SETUP.md` — first-time project setup guide

<!-- ENGINE-SPECIFIC: Engine-specific skill references added by setup-engine.sh -->
