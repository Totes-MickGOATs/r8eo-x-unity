---
name: physics-conformance-audit
description: Physics Conformance Audit
---


# Physics Conformance Audit

Use this skill when validating that the RC car physics simulation produces physically correct results. Covers the full catalogue of 93 conformance checks across 12 categories, the ConformanceRecorder API, tolerance tiers, and how to add new checks.

## When to Use

- After modifying any physics code (suspension, grip, drivetrain, air physics)
- When tuning physics parameters and need regression detection
- When implementing a new physics subsystem that needs validation
- When debugging physics behaviour that seems incorrect

## When NOT to Use

- General Unity testing patterns -- use `unity-testing-patterns`
- Non-physics code changes (UI, audio, input)
- Performance profiling -- use `unity-debugging-profiling`

---

## Overview

The conformance framework validates simulation accuracy by comparing measured values against analytical predictions derived from Newtonian mechanics. This is **black-box testing** -- expected values come from physics equations, never from the implementation.

### Core Principle

> Derive expected values from F=ma, Hooke's law, friction models, and kinematics.
> Never derive expected values from reading the source code.

---

## The 12 Categories

| Cat | Name | Checks | What It Validates |
|-----|------|--------|-------------------|
| A | Geometric Fidelity | 10 | Wheel positions, rotations, dimensions match physics state |
| B | Force Fidelity | 12 | Forces have correct direction, magnitude, and application point |
| C | Conservation Laws | 6 | Energy, momentum, and force conservation hold |
| D | Kinematic Consistency | 8 | Velocity, acceleration, position relationships are consistent |
| E | Contact & Collision | 6 | Ground detection, surface identification, penetration prevention |
| F | Suspension Specific | 9 | Spring/damper behaviour follows Hooke's law with correct bounds |
| G | Grip & Tire | 8 | Tire forces respect grip circle, load sensitivity, surface modifiers |
| H | Drivetrain | 6 | Differential behaviour, torque distribution, motor mapping |
| I | Air Physics | 6 | Airborne forces, trajectory, gyroscopic effects |
| J | Temporal | 4 | Determinism, timestep independence, continuity |
| K | ESC/Motor | 5 | Electronic speed controller state machine, throttle mapping |
| L | Compound Scenarios | 13 | Multi-system interactions (jumps, donuts, weight transfer) |

---

## Tolerance Tiers

Each check measures `tolerance = |actual - expected| / |expected|`:

| Tier | Threshold | Meaning |
|------|-----------|---------|
| Excellent | < 1% | Simulation matches theory precisely |
| Good | < 5% | Within acceptable engineering tolerance |
| Noticeable | < 15% | Player might notice but gameplay is acceptable |
| Poor | < 50% | Clearly wrong, needs investigation |
| Broken | >= 50% | Fundamentally incorrect, blocks progress |

A check **passes** if tolerance is below the Poor threshold (< 50%).

### Conformance Score

The overall conformance score is a weighted average:
- Excellent checks contribute 1.0
- Good checks contribute 0.8
- Noticeable checks contribute 0.5
- Poor checks contribute 0.2
- Broken checks contribute 0.0

---

## How to Add a New Conformance Check

1. **Identify the physics law** that governs the behaviour you want to validate.
2. **Derive the expected value** analytically. Write the formula in a comment. Example: `// F_spring = k * compression (Hooke's law)`
3. **Measure the actual value** from the simulation at runtime.
4. **Call `ConformanceRecorder.Record()`** with category, ID, name, expected, and actual.
5. **Add the check to the catalogue** in `.ai/knowledge/architecture/audit-physics-conformance.md`.
6. **Write a unit test** that exercises the check with known inputs.

### Naming Convention

- Check ID: `{category_letter}{number}` (e.g. `A1`, `B12`, `L13`)
- Check name: Short description of what is being validated
- Numbers are sequential within each category

---

## Tagged Debug Logging

The audit system automatically persists tagged debug messages. Use this format in physics code:

```csharp
Debug.Log("[physics] Suspension force: 3.2N at wheel 0");
Debug.Log("[grip] Slip ratio exceeded threshold: 0.25");
Debug.Log("[conformance] Check B6 failed: expected 14.715, got 12.3");
```

Recognised tags: `physics`, `grip`, `suspension`, `drivetrain`, `air`, `esc`, `input`, `surface`, `conformance`.

Each persisted log gets a `[db:HASH]` suffix appended in the console output. Use this hash to query the database:

```sql
SELECT * FROM debug_logs WHERE log_hash = 'a1b2c3d4';
```

---

## Full Check Catalogue

The complete catalogue of 93 checks with analytical prediction formulas is maintained in:
`.ai/knowledge/architecture/audit-physics-conformance.md`

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| `debug-system` | Structured logging, overlays, runtime inspection |
| `unity-physics-tuning` | PhysX configuration for RC racing |
| `clean-room-qa` | Black-box testing methodology |
| `unity-testing-patterns` | Unity Test Framework patterns (EditMode/PlayMode) |


## Topic Pages

- [Using ConformanceRecorder](skill-using-conformancerecorder.md)

