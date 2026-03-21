# CLAUDE.md — Testing, Architecture & Troubleshooting Details

Expanded details for the root [CLAUDE.md](./CLAUDE.md).

---

## Testing (TDD)

> **MANDATORY:** Write tests FIRST, run them, then implement. Never claim "fixed" without running tests.
> Full standards: `.ai/knowledge/architecture/coding-standards.md` | Skills: `unity-testing-patterns`, `clean-room-qa`

**TDD Cycle:** Hypothesize -> Write failing test (RED) -> Implement (GREEN) -> Commit. Steps 2-3 are non-negotiable.

**Test naming:** `MethodName_Scenario_ExpectedOutcome`

**Module-based test gating:** Pre-push detects changed `.cs` files and resolves affected modules. Tests only run if `UNITY_PATH` is set. Bypass: `SKIP_TEST_CHECK=1 git push`.

**Local verification queue:** All Unity compile/test jobs are serialized through one verifier worktree. Subagent worktrees do NOT run Unity directly — they submit to the queue after committing. See `scripts/tools/unity-queue.sh`.

**Autonomous debugging:** Agents drive the entire debug cycle via MCP tools. NEVER defer to user. Prefer unit tests over play-mode debugging.

### Physics Conformance Audit

Every physics change must be validated. See `.agents/skills/physics-conformance-audit/SKILL.md`.

---

## Architecture

### Conventions

- **Signal Up, Call Down** — global systems emit signals, children call methods on parents
- **No magic numbers** — use named constants for layer IDs, type enumerations, algorithm parameters
- **Type annotations** — always annotate function signatures and return types

See `.ai/knowledge/architecture/coding-standards.md` for full coding standards.

### Unity-Specific Rules for AI Development

- **Deterministic scene boot** — all scene state must be set in code, not via Inspector serialization
- **Minimize inspector wiring** — resolve dependencies via `GetComponent` or service locators
- **Code-built test fixtures** — create GameObjects and components programmatically in test setup
- **Keep gameplay logic testable in pure static classes** — can be tested in EditMode without Unity runtime

---

## RC Car Physics Domain

1/1 scale test: mass 15kg, wheel radius 1.66m, wheelbase 13.6m, track 10.0m. Curve-sampled grip (NOT Pacejka).
Key invariants: suspension force >= 0, no grip without normal load, differential conserves force.
Full constants + invariants: `.ai/knowledge/architecture/adr-001-physics-model.md`

---

## System Registry

> **MANDATORY:** Every game system must have a manifest in `resources/manifests/`.

- **Manifests:** `resources/manifests/` — one per system, declares owned files, status, dependencies
- **Validation:** `just validate-registry`

---

## Documentation Self-Improvement

> **MANDATORY:** When editing code in any directory, review the local `CLAUDE.md`. Update it if stale.

- Every non-hidden directory has a `CLAUDE.md` describing its contents and linking relevant skills
- Skill files live in `.agents/skills/<name>/SKILL.md` — reference them, don't duplicate content

---

## Session End — Self-Reflect and Self-Improve

> **MANDATORY:** Before reporting "done":
> 1. **Reflect:** What surprised me? What mistake did I make? What would I tell the next agent?
> 2. **Update docs:** Update local `CLAUDE.md` if stale. Add/remove files from listings.
> 3. **Maintain memories:** Add gotchas discovered, update affected memories, prune stale entries.
> 4. **Evolve:** Small improvement -> update CLAUDE.md. Reusable pattern -> update skill.

---

## Troubleshooting

### MCP Disconnects After Script Changes

Unity domain reload temporarily disconnects MCP. Wait 10-15s, retry. If still failing after 30s, use `/mcp` to refresh. Check `read_console` for compilation errors. **Do NOT** hand off to the user.

### Compilation Errors After Script Changes

Call `read_console` after modifying C# scripts. Common traps: `Physics` vs `UnityEngine.Physics`, `Camera` ambiguity. Fix ALL errors before proceeding.

### Compiler Errors When Opening the Game

Three common causes:
- **main not promoted** — a feature branch was committed but never fast-forwarded via `just queue-promote`
- **stale Library** — delete `Library/` and let Unity reimport (slow but reliable)
- **missing .meta files** — every new asset or script needs a paired `.meta` file committed alongside it
