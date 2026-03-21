---
description: Multi-pass quality assurance — requirements alignment, implementation quality, convergence, knowledge hygiene
---

# Bulletproof Quality Assurance

Multi-pass quality assurance for code changes. Ensures requirements alignment, implementation quality, convergence, and knowledge hygiene before declaring done.

User's task: $ARGUMENTS

---

## Complexity Gate

Before entering the full bulletproof pipeline, assess the task scope:

| Criteria | LIGHT | FULL |
|----------|-------|------|
| Lines of code changed | < 50 | >= 50 |
| Files touched | 1-2 | 3+ |
| New files created | 0 | 1+ |
| Cross-system interaction | No | Yes |
| User explicitly requested thoroughness | No | Yes |

**LIGHT path** (trivial change — typo, config tweak, single-function fix):
- Ask-First Phase 1 (Interrogate) still required — brief (1-2 minutes)
- Ask-First Phase 2 (Test-First) still required — positive + negative tests minimum
- Skip subagent dispatch — implement inline
- Phase 0: Quick churn check (30 seconds); Phase 1: State AC briefly; Phase 2: Implement with TDD
- Phase 3: One self-review pass; Phase 4: Update docs only if directly affected; Phase 5: Brief summary

**FULL path** (substantial changes): Run all phases as described in the detail pages below.

If in doubt, start LIGHT — escalate to FULL if complexity reveals itself during implementation.

---

## Phase Index

| Phase | Detail Page | Summary |
|-------|-------------|---------|
| Phase 0: Churn Detection | [bulletproof-phases-0-2.md](./bulletproof-phases-0-2.md) | Ask-First prerequisite check, weighted churn scoring, knowledge base cleanup |
| Phase 1: Requirements Alignment | [bulletproof-phases-0-2.md](./bulletproof-phases-0-2.md) | Parse AC, ground-truth verification, scope diagram, user confirmation |
| Phase 2: Implementation | [bulletproof-phases-0-2.md](./bulletproof-phases-0-2.md) | Subagent dispatch, model routing, CI-first testing |
| Phase 3: Review Loop | [bulletproof-phases-3-5.md](./bulletproof-phases-3-5.md) | 11-point checklist, contract sync, convergence loop, rollback protocol |
| Phase 4: Knowledge Sync | [bulletproof-phases-3-5.md](./bulletproof-phases-3-5.md) | Contract verification punchlist, `/dev:clean-loop` |
| Phase 5: Completion Report | [bulletproof-phases-3-5.md](./bulletproof-phases-3-5.md) | Merge readiness, CI green, PR label, 11-point report |

---

## Token Efficiency Rules

1. Complexity gate routes small tasks to LIGHT path — no subagent overhead for trivial changes
2. Phase 0 is weighted scoring — single ambiguous keywords don't trigger expensive cleanup
3. Subagent context budgets — each subagent receives ONLY relevant CLAUDE.md + memory entries
4. Incremental review diffing — pass N+1 reads only files changed since pass N
5. Early exit ramp — small clean implementations skip the two-consecutive-clean requirement
6. Phase 0→4 cache — systems verified clean in Phase 0 are not re-audited in Phase 4
7. Use `git diff` to scope — never re-explore the codebase when the diff tells you what changed
8. Ground-truth before dispatch — verify critical claims with `wc -l`, `grep`, or `Read` before dispatching

---

## Relevant Skills

| Skill | Location | When to Use |
|-------|----------|-------------|
| `ask-first` | `.agents/skills/ask-first/` | Phase 0 prerequisite |
| `unity-testing-debugging-qa` | `.agents/skills/unity-testing-debugging-qa/` | Phase 2 (TDD), Phase 3 (test verification) |
| `swarm-development` | `.agents/skills/swarm-development/` | Phase 2 (subagent dispatch), Phase 3 (review loop) |
| `clean-room-qa` | `.agents/skills/clean-room-qa/` | Phase 0 (Ask-First Phase 2), Phase 3 |
| `reverse-engineering` | `.agents/skills/reverse-engineering/` | Phase 0 (churn diagnosis), Phase 2-3 (debugging) |
| `unity-architecture-patterns` | `.agents/skills/unity-architecture-patterns/` | Phase 1 (scope analysis), Phase 2 |
| `unity-csharp-mastery` | `.agents/skills/unity-csharp-mastery/` | Phase 2 (implementation), Phase 3 (code review) |
| `unity-physics-3d` | `.agents/skills/unity-physics-3d/` | Phase 2 (physics implementation) |
