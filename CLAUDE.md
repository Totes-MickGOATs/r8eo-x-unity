# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.

## BEFORE YOU WRITE ANY CODE — Read This First

> **MANDATORY PREREQUISITE:** You must be on a feature branch before writing, editing, or creating any file.
>
> **If you are a subagent:** Run `bash scripts/tools/safe-worktree-init.sh <task>` as your FIRST action. It prints `WORKTREE_PATH=/absolute/path`. Work exclusively in that worktree using absolute paths. Never edit files in `$CLAUDE_PROJECT_DIR` (the main repo).
>
> **If you are the main agent:**
> 1. **Do NOT edit any files yet.** You are on `main` and commits will be blocked.
> 2. **Dispatch a subagent WITHOUT `isolation: "worktree"`** — the subagent calls `safe-worktree-init.sh` itself.
> 3. The subagent handles: worktree setup -> code changes -> commit -> submit for verification.
> 4. You verify the queue promoted the branch, then run `just task-complete <task>`.
>
> **Full workflow guide:** `.agents/skills/branch-workflow/SKILL.md`

---

## First Commands — Start Here

```bash
just diagnose      # syntax → registry → compile (if UNITY_PATH set) → targeted module tests
```

Set `UNITY_PATH` once in your shell profile to enable compile check and test execution:
```bash
export UNITY_PATH="/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity"
```

---

## Details (Topic Pages)

| Page | Contents |
|------|----------|
| [Git Workflow & Lint Stack](./claude-git-lint.md) | Branch quick-reference, agent rules, sequential coordination, definition of done, lint tiers |
| [Testing, Architecture & Troubleshooting](./claude-testing-architecture.md) | TDD cycle, physics conformance, architecture conventions, system registry, troubleshooting |

---

## Mandatory Intake — Before Any Implementation

> **MANDATORY:** Stop before coding. Confirm these seven items.

- [ ] **Goal** — one sentence stating what changes and why
- [ ] **Success criteria** — how you know it's done (observable, testable)
- [ ] **Scope boundary** — what is explicitly out of scope
- [ ] **Relevant files** — only the files that will be read or modified
- [ ] **Existing pattern** — one reference implementation to follow
- [ ] **Tests** — which tests to write or update (class name + scenario)
- [ ] **Smallest shippable chunk** — can this be one function, one view, or one bounded fix?

### Task Packet Format

```
Task:        <one sentence — what to do>
Acceptance:  <observable done condition>
Pattern:     <reference file or method to follow>
Allow:       <files allowed to touch>
Exclude:     <files that must not change>
Tests:       <test class + scenario names to write>
Done when:   <specific verifiable state>
```

---

## Session Start — Self-Reflect Before Acting

> **MANDATORY:** Before writing code:
> 1. Read relevant memories from `MEMORY.md` and the local `CLAUDE.md` for the directory you're modifying.
> 2. Follow the Ask-First workflow (`.agents/skills/ask-first/SKILL.md`) — Interrogate -> Test-First -> Implement.
> 3. State your plan briefly before starting.

---

## First-Time Setup

If this is a fresh clone or a new project, see **`SETUP.md`** for tool installation, engine selection, GitHub protection rules, and git hooks setup. Or run `/dev:init-project` for an interactive guided walkthrough.

---

## Project Overview

**Game:** R8EO-X — A realistic RC Racing Simulator.

Current state: _Update `.ai/knowledge/status/project-status.md` with phase tracking._

---

## Python Tooling (scripts/tools only)

```bash
uv sync                          # Install dependencies
uv run python <script.py>        # Run a script
```

---

## Issue-Driven Workflow

> **PREFERRED:** File issues for non-trivial work (bug reports, features).

**Agent workflow:** `/dev:next-task` -> work in worktree -> reference `#issue-number` in commits/PR.

---

## Key Reference Files

- `CLAUDE_SKILLS.md` — **Index of all available skills and when to use them (READ FIRST)**
- `.agents/skills/branch-workflow/SKILL.md` — **Branch workflow, worktree recipes, gotchas (READ FIRST)**
- `.ai/knowledge/architecture/system-overview.md` — scene graph, signal map, data flow, ADRs
- `.ai/knowledge/architecture/coding-standards.md` — coding conventions for this project
- `.ai/knowledge/status/project-status.md` — phase checklist, full scripts/scenes inventory
- `.agents/skills/feature-toggles/SKILL.md` — **Compile-time and runtime feature toggle patterns**
- `VERSIONING.md` — SemVer convention, release lifecycle, build types
- `SETUP.md` — first-time project setup guide

<!-- ENGINE-SPECIFIC: Engine-specific skill references added by setup-engine.sh -->
