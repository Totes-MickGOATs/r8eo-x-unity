---
name: clean-room-qa
description: Clean Room QA Skill
---


# Clean Room QA Skill

Use this skill when writing black-box tests with zero implementation knowledge, deriving test cases purely from function signatures and domain expectations. Enforces strict separation between test-writing and implementation.

## Philosophy

> "If you know how the code works, you can't objectively test whether it works correctly."

This skill enforces a strict separation between test-writing and implementation knowledge. The QA agent:

1. **READS**: Only public API signatures (class name, method name, parameter types, return type)
2. **NEVER READS**: Method bodies, private fields, internal logic, comments explaining "how"
3. **DERIVES**: Expected behavior from the function's name, domain context, and physics/math principles
4. **WRITES**: Tests that assert physically correct outcomes
5. **EXPECTS**: Some tests to FAIL — that's the point. Failing tests = bugs found.

## Minimum Coverage Requirements

> **MANDATORY:** Every public method/function MUST have at minimum these tests:

| Level | Requirement | Minimum |
|-------|-------------|---------|
| **Unit** | Every public method touched or added | **1 positive + 1 negative per method** |
| **Integration** | Every cross-class/cross-system interaction | **1 per interaction path** |
| **E2E (PlayMode)** | Every user-facing feature or behavior change | **1 per feature/behavior** |

- **Positive test:** Valid input, correct output (happy path)
- **Negative test:** Invalid/edge/boundary input handled correctly (zero, null, out-of-range, NaN, negative values)

These are MINIMUMS. Most methods should also have boundary, conservation, monotonicity, symmetry, and independence tests where applicable (see Step 3 below).

## When to Use

Invoke this skill when:
- **Every dev task** — this skill is Phase 2 of the Ask-First workflow (`.agents/skills/ask-first/SKILL.md`)
- Porting code between engines/languages (Godot → Unity, etc.)
- Refactoring physics, math, or business logic
- Adding new systems that must behave correctly by domain rules
- Auditing code you didn't write
- Verifying that unit tests actually test behavior, not implementation

## How to Invoke

```
/clean-room-qa <system-name>
```

Or dispatch as a subagent:
```
Agent(subagent_type="general-purpose", prompt="Use the clean room QA skill at .agents/skills/clean-room-qa/SKILL.md to audit <system>")
```



## Topic Pages

- [Process](skill-process.md)

