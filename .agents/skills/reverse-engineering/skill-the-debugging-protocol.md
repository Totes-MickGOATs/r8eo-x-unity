# The Debugging Protocol

> Part of the `reverse-engineering` skill. See [SKILL.md](SKILL.md) for the overview.

## The Debugging Protocol

### Phase 1: Observe the Symptom

Before forming any hypothesis, collect raw facts:

1. **What exactly happens?** — Not "it's broken" but "the player falls through the floor at position (12, 0, -5) when landing from a jump"
2. **When does it happen?** — Always? Sometimes? Only after a specific sequence of actions?
3. **What changed recently?** — `git log --oneline -20` to see recent commits. `git diff HEAD~5` for recent changes.
4. **What are the exact error messages?** — Copy them verbatim. Don't paraphrase.
5. **Is it reproducible?** — If yes, write down the exact reproduction steps.

### Phase 2: Form a Hypothesis

Based on the symptoms, propose a specific, testable explanation:

- **Good hypothesis:** "The collision shape is not updating its transform after the animation plays, so the physics body uses stale collision geometry"
- **Bad hypothesis:** "Something is wrong with the physics"

A hypothesis must be:
- **Specific** — points to a particular mechanism or code path
- **Testable** — you can design an experiment to confirm or refute it
- **Falsifiable** — there exists evidence that would prove it wrong

### Phase 3: Gather Evidence

Test your hypothesis with the **minimum intervention** that produces useful data:

```
Read code → Add targeted logging → Inspect runtime state → Check config
```

**Do NOT** start changing code to "try things." That introduces new variables and makes the problem harder to diagnose.

#### Evidence-Gathering Techniques

| Technique | When to Use | Example |
|-----------|-------------|---------|
| **Read the code** | Always start here | Trace the call chain from symptom to origin |
| **Add logging** | When you need runtime values | `Debug.log("physics", "velocity: %s" % velocity)` |
| **Inspect state** | When values look wrong | Check variable values at breakpoints or via debug overlay |
| **Check config** | When behavior differs from expectation | Verify project settings, export values, config files |
| **Binary search** | When the failure is in a large change set | Bisect commits or comment out code blocks |
| **Minimal reproduction** | When the bug is intermittent | Strip the scenario to the simplest case that still fails |

### Phase 4: Confirm or Refute

Your evidence either supports or contradicts your hypothesis:

- **Supported:** Proceed to Phase 5 (fix). But verify with a second independent piece of evidence if possible — one data point can be coincidence.
- **Contradicted:** Go back to Phase 2 with a new hypothesis. The failed hypothesis is still valuable — it rules out a possibility.

### Phase 5: Fix and Verify

1. Write a failing test that captures the bug (TDD red phase)
2. Implement the minimum fix
3. Run the test — confirm it passes (TDD green phase)
4. Run related tests — confirm no regressions
5. Commit with a message that explains the root cause, not just the symptom

