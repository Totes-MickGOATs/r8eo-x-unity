---
name: unity-testing-debugging-qa
description: Unity Testing, Debugging & QA
---


# Unity Testing, Debugging & QA

Comprehensive guide to testing, debugging, and quality assurance for Unity games. Use this skill as the unified entry point when planning QA strategy, setting up test infrastructure, debugging issues, or integrating quality checks into CI/CD. For deep dives into specific areas, see the Related Skills section.

## Minimum Coverage Requirements (MANDATORY)

> **Every change MUST meet these minimum test requirements. No exceptions.**

| Level | What | Minimum | Where |
|-------|------|---------|-------|
| **Unit** | Every public method/function touched or added | **1 positive + 1 negative per method** (minimum 2) | `Assets/Tests/EditMode/` |
| **Integration** | Every cross-class/cross-system interaction | **1 per interaction path** | `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/` |
| **E2E (PlayMode)** | Every user-facing feature or behavior change | **1 per feature/behavior** | `Assets/Tests/PlayMode/` |

- **Positive test:** Valid input, correct output (happy path)
- **Negative test:** Invalid/edge/boundary input handled correctly (zero, null, out-of-range, NaN)
- **Test naming:** `MethodName_Scenario_ExpectedOutcome`
- **Pre-implementation:** Tests MUST be written by a separate black-box agent (no implementation knowledge) before implementation begins. See `.agents/skills/ask-first/SKILL.md` Phase 2.
- **Test Integrity Rule:** Implementing agents MUST NOT silently modify tests to make them pass. If a test assertion appears wrong, file it as a finding and discuss with the user.

## Diagnostics & Crash Reporting

### Cloud Diagnostics Advanced (Backtrace)

Unity's crash reporting tool captures:

- Environment snapshot at time of crash
- Call stack, heap state, register values
- Automatic analysis to determine root cause

**Deduplication:** A clustering algorithm groups crashes by root cause, helping prioritize which errors to fix first for maximum stability improvement.

**Analytics:** Trend and pattern analysis over time reveals systemic issues and the impact of fixes.

---

## Cross-Platform Testing

### Device Coverage Strategy

Test on a range of devices within your target platforms, especially for mobile:

- **Operating systems:** Different OS versions (Android API levels, iOS versions)
- **Screen sizes:** Phone, tablet, different aspect ratios
- **Hardware tiers:** Lowest-spec supported device, mid-range, flagship
- **Input methods:** Touch, gamepad, keyboard+mouse

### Mobile Considerations

Beyond functionality, test for:

- **Battery drainage** — sustained gameplay power consumption
- **Thermal throttling** — performance under heat (endurance tests)
- **Memory pressure** — low-memory device behavior, background app kills

Revisit device benchmarks when adding features. A change that is fine on flagship hardware may break minimum-spec devices.

---

## Case Study: Phantom Input on Windows

This case study illustrates the diagnostic pattern and TDD discipline described above. It took 4 PRs to fully resolve because testing was not comprehensive from the start.

### Symptom

Vehicle accelerated and braked with no controller connected on Windows. The Legacy Input Manager reported `-1.0` on the `CombinedTriggers` axis — a constant phantom value from Windows platform behavior, not a Unity bug.

### Diagnostic Signal

**Constant value = phantom input.** Attaching an `InputDiagnostics` MonoBehaviour to the vehicle and logging axis values every 30 frames revealed that trigger values showed zero variance across hundreds of frames. Real human input always shows micro-jitter. A value that never changes is stuck/phantom.

### Resolution

Three-layer defense: variance-based TriggerDetector (jitter < 0.02 = stuck), mode gating (zero output during Detecting/None modes), and deadzones (0.15 triggers, 0.2 steering). The critical insight was that the detection phase must **observe, never drive** — reading raw axes during detection leaked phantom values as real input.

### Lesson

Write the full test matrix first: ALL modes (Detecting, Separate, Combined, None) x ALL axes (throttle, brake, steering). Fixing one axis at a time without comprehensive tests caused 4 rounds of fixes instead of 1. The test matrix IS the specification.

> **Deep dive:** See `unity-input-debugging` skill for the complete guide.

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-input-debugging`** | Deep dive: Phantom input on Windows, variance-based detection, three-layer defense, input TDD matrix, diagnostic MonoBehaviour |
| **`unity-testing-patterns`** | Deep dive: UTF code examples, assertions reference, mocking, parameterized tests, setup/teardown patterns |
| **`unity-debugging-profiling`** | Deep dive: Unity Profiler, Frame Debugger, Memory Profiler, Gizmos, custom debug tools, logging |
| **`unity-e2e-testing`** | Deep dive: E2E automation, InputTestFixture, visual testing, AltTester, third-party tools, CI integration |
| **`unity-performance-optimization`** | Deep dive: batching, GC reduction, object pooling, LOD, shader optimization |
| **`clean-room-qa`** | Black-box testing with zero implementation knowledge — derive tests from function signatures and domain physics |
| **`reverse-engineering`** | Systematic debugging methodology — chain of custody from symptom to root cause |
| **`debug-system`** | Debug overlay architecture — structured logging, F-key overlays, runtime inspection |

---

## Quick Reference: What Testing Do I Need?

```
Is this a bug fix?
  YES -> Write a failing test that reproduces it (TDD), then fix
  NO  |
      v
Is this pure logic / math / data?
  YES -> Unit test (Edit Mode, [Test])
  NO  |
      v
Does it involve multiple systems interacting?
  YES -> Integration test (Play Mode, [UnityTest])
  NO  |
      v
Is it a full user journey (boot -> menu -> play)?
  YES -> E2E test (Play Mode, InputTestFixture, [Category("E2E")])
  NO  |
      v
Is it a performance concern?
  YES -> Performance test (Profiler + Performance Testing Extension)
  NO  |
      v
Is it a visual/rendering concern?
  YES -> Screenshot test (Graphics Test Framework) -- needs GPU in CI
  NO  |
      v
Is it a design requirement ("when X, then Y")?
  YES -> Functional test (black-box or white-box)
  NO  |
      v
Did code just change and might have broken things?
  YES -> Run regression suite, add tests for changed areas
```


## Topic Pages

- [Testing Philosophy](skill-testing-philosophy.md)

