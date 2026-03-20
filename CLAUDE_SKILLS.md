# CLAUDE_SKILLS.md — Model Routing Guide

Map work to the right model strength. Do not send all work to one model by default.
When using a single model for a multi-phase task, state the reason explicitly.

## Model Roles

| Model | Use For | Do NOT Use For |
|-------|---------|----------------|
| **haiku** | File discovery, dependency tracing, grep-based edge scanning, churn detection, broad searches, reading CLAUDE.md files | Implementation, planning, architecture decisions |
| **sonnet** | Focused implementation in small approved chunks, test updates, refactors within an approved plan, straightforward reviews | Ambiguous problems, architectural decisions, multi-system changes |
| **opus** | Planning, architecture, ambiguous debugging, edge-case analysis, risk review, final integration decisions, work that crosses system boundaries | Routine single-file changes, simple implementations |

## Team Assignment Model

Think of model selection as team assignment, not tool selection:
- **haiku = junior researcher** — fast, cheap, broad coverage
- **sonnet = senior developer** — focused execution on well-scoped work
- **opus = architect/lead** — resolves ambiguity, sets direction, final sign-off

## Review Routing

| Review Type | Assign To | Why |
|-------------|-----------|-----|
| Ambiguity resolution | opus | Needs judgment and context synthesis |
| Architecture fit check | opus | Cross-system awareness required |
| Edge-case discovery scan | haiku | Broad grep/file scan, low cost |
| Duplicate-pattern scan | haiku | Pattern matching across large codebase |
| Implementation pass | sonnet | Focused, well-scoped execution |
| Risk review (cross-boundary change) | opus | Final gate before merge |
| Test-scenario generation | haiku | Enumerate permutations cheaply |
| Security review | opus | Requires reasoning about threat model |

## Examples

**Bug fix in one file (single system):**
1. haiku — find all call sites, scan for same pattern elsewhere
2. opus — confirm diagnosis and fix strategy if uncertain; skip if straightforward
3. sonnet — implement fix + tests

**New feature crossing two systems:**
1. opus — plan and define task packet
2. haiku — scan for reference implementations, existing patterns
3. sonnet — implement each chunk (one per PR)
4. opus — final integration risk review

**Refactor within approved plan:**
1. sonnet only — plan already exists, scope is clear

**Architecture question:**
1. opus only — do not assign to sonnet or haiku

## Rule: Require Stated Reason for Single-Model Multi-Phase Tasks

If an entire multi-phase task is assigned to one model, you must state why:
- GOOD: "Using sonnet for all phases because the plan is approved, scope is bounded, and no architecture decisions remain."
- BAD: (no reason given — defaults to sonnet for everything)
