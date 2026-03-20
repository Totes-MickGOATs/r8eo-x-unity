---
description: Mandatory ask-first pre-implementation workflow — Interrogate, Test-First, Implement
---

# Ask-First Pre-Implementation Workflow

Mandatory pre-implementation workflow. Run this before starting any dev task.

## Usage
```
/dev:ask-first
```

## Pre-Alignment Checkpoint

Before executing any phase, answer these four questions:

1. **Clear plan?** — Is the goal stated in one sentence with acceptance criteria?
2. **Minimal scope?** — Is this one function / one view / one bounded fix?
3. **Correct model?** — Is the model assigned appropriate for this phase? (See `CLAUDE_SKILLS.md`)
4. **Only relevant context?** — Are only the necessary files loaded (not the full codebase)?

If any answer is "no" or "unclear" → **return to planning, do not proceed to implementation.**

## Process

1. Read the full skill guide: `.agents/skills/ask-first/SKILL.md`
2. Execute **Phase 1: Interrogate** — complete all 10 steps, write answers down
3. If confidence < 3, STOP and ask the user for clarification before proceeding
4. Execute **Phase 2: Test-First** — dispatch a separate agent (`model: "sonnet"`) to write black-box tests:
   - Give it ONLY: public API signatures, gameplay mechanics, acceptance criteria, physics invariants
   - DO NOT give it: implementation code, internal architecture, your hypothesis
   - Minimum coverage: 1 positive + 1 negative per method, 1 integration per cross-class call, 1 E2E per feature
   - Verify ALL tests are RED before proceeding
5. Execute **Phase 3: Implement** — standard TDD against failing tests
6. Verify no regressions in existing tests

## Arguments
- `$ARGUMENTS` — Optional: brief description of the task (for context in Phase 1)

## Quick Checklist

### Phase 1: Interrogate
- [ ] Problem stated in one sentence
- [ ] Memories searched
- [ ] Git history checked
- [ ] Questions listed and answered
- [ ] Guards and invariants identified
- [ ] Hypothesis formed
- [ ] "What would break?" analysis done
- [ ] Confidence rated (1-5)
- [ ] Plan stated

### Phase 2: Test-First
- [ ] Test agent dispatched (black-box, no implementation knowledge)
- [ ] Unit: >= 1 positive + 1 negative per method
- [ ] Integration: 1 per cross-class interaction
- [ ] E2E: 1 per user-facing feature
- [ ] All tests confirmed RED

### Phase 3: Implement
- [ ] Each test GREEN
- [ ] No regressions
- [ ] Committed and pushed
