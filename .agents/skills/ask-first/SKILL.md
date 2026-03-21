---
name: ask-first
description: Ask-First: Mandatory Pre-Implementation Workflow
---


# Ask-First: Mandatory Pre-Implementation Workflow

Use this skill when starting any new dev task to clarify requirements and write failing tests before implementation. Covers the three-phase interrogate-test-implement cycle that every bug fix, feature, and refactor must follow.

Every dev task — bug fix, feature, refactor — follows three phases in strict order:

1. **Phase 1: Interrogate** — Understand before you act
2. **Phase 2: Test-First** — Prove your understanding with failing tests (black-box, separate agent)
3. **Phase 3: Implement** — Make the tests green via TDD

Skipping phases wastes time. Agents that skip Phase 1 misunderstand requirements. Agents that skip Phase 2 write tests biased by their implementation. This workflow prevents both failure modes.

---

## Pre-Phase-2 Gate: Task Packet Completeness

> **Do not start test writing or implementation until this packet is complete.**

Fill in every field. Missing fields mean planning is incomplete — stay in Phase 1.

```
Task:        <one sentence>
Acceptance:  <observable done condition>
Pattern:     <reference file or method>
Allow:       <files to touch>
Exclude:     <files that must not change>
Tests:       <test class + scenario names>
Done when:   <specific verifiable state>
```

**Check:**
- [ ] All seven fields filled
- [ ] Scope fits in one chunk (one function / one view / one fix)
- [ ] Model assignment stated and matches `CLAUDE_SKILLS.md` routing
- [ ] Context loaded is minimal (root CLAUDE.md + local CLAUDE.md + relevant files only)

If any field is empty or scope is too large → decompose further before proceeding.

---

## When to Skip (Almost Never)

| Change Type | Phase 1 | Phase 2 | Phase 3 |
|-------------|---------|---------|---------|
| Bug fix | REQUIRED | REQUIRED | REQUIRED |
| New feature | REQUIRED | REQUIRED | REQUIRED |
| Refactor | REQUIRED | REQUIRED (verify existing tests cover it) | REQUIRED |
| Performance optimization | REQUIRED | REQUIRED (benchmark tests) | REQUIRED |
| Pure documentation | Recommended | Skip | Skip |
| Formatting/lint fix | Skip | Skip | Skip |
| CLAUDE.md update | Skip | Skip | Skip |

---

## Quick Reference Checklist

Copy this into your working notes at the start of every task:

```
## Ask-First Checklist

### Phase 1: Interrogate
- [ ] Problem stated in one sentence
- [ ] Memories searched (postmortems, feedback, project)
- [ ] Git history checked for reverts/re-opens
- [ ] Questions listed
- [ ] Questions answered (code, docs, memories, console)
- [ ] Guards and invariants identified
- [ ] Hypothesis formed (specific, testable)
- [ ] Full contract verified (input, signals, collisions, scene wiring, dependencies, output, cleanup)
- [ ] "What would break?" adversarial analysis done
- [ ] All call sites audited — every location using the same pattern listed and assessed
- [ ] Confidence rated (1-5). If < 3, asked user for clarification
- [ ] Plan stated (3-5 bullets)

### Phase 2: Test-First (Black Box)
- [ ] Test agent dispatched with signatures + domain context only
- [ ] Unit tests: >= 1 positive + 1 negative per method
- [ ] Integration tests: 1 per cross-class interaction
- [ ] E2E tests: 1 per user-facing feature/behavior
- [ ] All tests run and confirmed RED
- [ ] Test names are descriptive sentences

### Phase 3: Implement
- [ ] Each test made GREEN with minimum code
- [ ] All new tests GREEN together
- [ ] Related existing tests still pass (no regressions)
- [ ] Committed and pushed
```

---

## Related Skills

- **`clean-room-qa`** — Black-box testing methodology (the foundation for Phase 2)
- **`reverse-engineering`** — Chain-of-custody debugging (useful during Phase 1 for bugs)
- **`unity-testing-patterns`** — UTF code examples, assertions, mocking patterns
- **`unity-e2e-testing`** — PlayMode testing, InputTestFixture, visual testing
- **`unity-testing-debugging-qa`** — Master QA reference, testing pyramid, CI integration
- **`debug-system`** — Structured logging and overlays for runtime diagnosis


## Topic Pages

- [Phase 1: Interrogate (Ask Yourself First)](skill-phase-1-interrogate-ask-yourself-first.md)
- [Phase 2: Test-First (Black Box, Separate Agent)](skill-phase-2-test-first-black-box-separate-agent.md)

