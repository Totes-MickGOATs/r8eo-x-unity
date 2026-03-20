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
> 3. The subagent handles: worktree setup → code changes → commit → push → PR → merge.
> 4. You verify the PR merged successfully, then run `just task-complete <task>`.
>
> **Why this matters:** Three enforcement layers block main branch commits (PreToolUse hook, git pre-commit hook, GitHub branch protection). If you edit files on main, you'll waste work that can't be committed.
>
> **Full workflow guide:** `.agents/skills/branch-workflow/SKILL.md`

---

## First Commands — Start Here

Before exploring or fixing anything, run:

```bash
just diagnose      # syntax → registry → compile (if UNITY_PATH set) → targeted module tests
```

- Clean tree → exits after phase 3 with no tests run
- C# changes → auto-selects only affected module tests (not the full suite)
- Compile error → stops at phase 3 with the exact error
- Registry drift → stops at phase 2 with the affected manifest

Set `UNITY_PATH` once in your shell profile to enable compile check and test execution:
```bash
export UNITY_PATH="/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity"
```

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

**Game:** R8EO-X — A realistic RC Racing Simulator.

Current state: _Update `.ai/knowledge/status/project-status.md` with phase tracking._

## Git — Branch Workflow

> **MANDATORY:** All changes go through feature branches + PRs. Direct commits to main are blocked.
> Full details: `.agents/skills/branch-workflow/SKILL.md`

### Quick Reference

```
just lifecycle init <task>     # Create worktree + feature branch (subagent first action)
# ... TDD develop, commit ...  # Write tests, implement, commit
just lifecycle ship            # Push, create PR, merge, sync main
just task-complete <task>      # Main agent: cleanup after merge
```

### Agent Rules (Hard)

- **NEVER** commit on `main` — PreToolUse hook blocks it
- **NEVER** use `--no-verify` — hook blocks it
- **NEVER** use `isolation: "worktree"` — subagents call `safe-worktree-init.sh`
- **ALWAYS** set `model` on Agent calls: `haiku` for Explore, `opus` for Plan, `sonnet` for general-purpose
- **ALWAYS** commit every file immediately after creating or modifying it

### Sequential Task Coordination

When the main agent decomposes work into multiple tasks:
1. Dispatch ONE subagent at a time — each performs one atomic, non-breaking task
2. After each PR merges: remove in-flight memories, update permanent memories
3. Full protocol: `.agents/skills/swarm-development/SKILL.md`

### Definition of Done

1. **PR is open** with all commits pushed
2. **Local tests pass** — coverage ratchet is a FAIL gate
3. **PR merged** — confirmed with `gh pr view --json state -q .state`
4. **Local main updated** — `just lifecycle ship` syncs automatically

### Commit Rules

- Commit message format: `type: short description` (e.g., `feat: add surface friction zone`)
- Conventional commit types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`, `perf`, `style`, `build`

## Testing (TDD)

> **MANDATORY:** Write tests FIRST, run them, then implement. Never claim "fixed" without running tests.
> Full standards: `.ai/knowledge/architecture/coding-standards.md` | Skills: `unity-testing-patterns`, `clean-room-qa`

**TDD Cycle:** Hypothesize -> Write failing test (RED) -> Implement (GREEN) -> Commit. Steps 2-3 are non-negotiable.

**Test naming:** `MethodName_Scenario_ExpectedOutcome`

**Module-based test gating:** Pre-push detects changed `.cs` files and resolves affected modules. Tests only run if `UNITY_PATH` is set. Bypass: `SKIP_TEST_CHECK=1 git push`.

**Autonomous debugging:** Agents drive the entire debug cycle via MCP tools. NEVER defer to user. Prefer unit tests over play-mode debugging.

### Physics Conformance Audit

Every physics change must be validated. See `.agents/skills/physics-conformance-audit/SKILL.md`.

## Python Tooling (scripts/tools only, not game code)

- **Python 3.14** with **uv** as the package manager

```bash
uv sync                          # Install dependencies
uv run python <script.py>        # Run a script
```

## Architecture

### Conventions

- **Signal Up, Call Down** — global systems emit signals, children call methods on parents
- **No magic numbers** — use named constants for layer IDs, type enumerations, algorithm parameters
- **Type annotations** — always annotate function signatures and return types

See `.ai/knowledge/architecture/coding-standards.md` for full coding standards.

## RC Car Physics Domain

1/1 scale test: mass 15kg, wheel radius 1.66m, wheelbase 13.6m, track 10.0m. Curve-sampled grip (NOT Pacejka).
Key invariants: suspension force >= 0, no grip without normal load, differential conserves force.
Full constants + invariants: `.ai/knowledge/architecture/adr-001-physics-model.md`

## System Registry

> **MANDATORY:** Every game system must have a manifest in `resources/manifests/`.

- **Manifests:** `resources/manifests/` — one per system, declares owned files, status, dependencies
- **Validation:** `just validate-registry`

## Documentation Self-Improvement

> **MANDATORY:** When editing code in any directory, review the local `CLAUDE.md`. Update it if stale.

- Every non-hidden directory has a `CLAUDE.md` describing its contents and linking relevant skills
- Skill files live in `.agents/skills/<name>/SKILL.md` — reference them, don't duplicate their content

## Session End — Self-Reflect and Self-Improve

> **MANDATORY:** Before reporting "done":
> 1. **Reflect:** What surprised me? What mistake did I make? What would I tell the next agent?
> 2. **Update docs:** Update local `CLAUDE.md` if stale. Add/remove files from listings.
> 3. **Maintain memories:** Add gotchas discovered, update affected memories, prune stale entries.
> 4. **Evolve:** Small improvement -> update CLAUDE.md. Reusable pattern -> update skill.

## Issue-Driven Workflow

> **PREFERRED:** File issues for non-trivial work (bug reports, features).

**Agent workflow:** `/dev:next-task` -> work in worktree -> reference `#issue-number` in commits/PR.

## Troubleshooting

### MCP Disconnects After Script Changes

Unity domain reload temporarily disconnects MCP. Wait 10-15s, retry. If still failing after 30s, use `/mcp` to refresh. Check `read_console` for compilation errors. **Do NOT** hand off to the user.

### Compilation Errors After Script Changes

Call `read_console` after modifying C# scripts. Common traps: `Physics` vs `UnityEngine.Physics`, `Camera` ambiguity. Fix ALL errors before proceeding.

## Key Reference Files

- `.agents/skills/branch-workflow/SKILL.md` — **Branch workflow, worktree recipes, gotchas (READ FIRST)**
- `.ai/knowledge/architecture/system-overview.md` — scene graph, signal map, data flow, ADRs
- `.ai/knowledge/architecture/coding-standards.md` — coding conventions for this project
- `.ai/knowledge/status/project-status.md` — phase checklist, full scripts/scenes inventory
- `.agents/skills/feature-toggles/SKILL.md` — **Compile-time and runtime feature toggle patterns**
- `VERSIONING.md` — SemVer convention, release lifecycle, build types
- `SETUP.md` — first-time project setup guide

<!-- ENGINE-SPECIFIC: Engine-specific skill references added by setup-engine.sh -->
