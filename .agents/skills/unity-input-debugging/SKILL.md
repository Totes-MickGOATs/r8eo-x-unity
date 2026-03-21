---
name: unity-input-debugging
description: Unity Input Debugging: Phantom Input & Platform Quirks
---


# Unity Input Debugging: Phantom Input & Platform Quirks

Use this skill when diagnosing phantom input values, platform-specific input quirks, or unexpected axis readings in Unity's Legacy Input Manager, especially on Windows.

---

## Section 1: Windows Phantom Axis Values

### The Problem

Unity's Legacy Input Manager reports non-zero values for certain axes even when **no controller is connected** on Windows:

| Axis | Phantom Value | Notes |
|------|--------------|-------|
| `CombinedTriggers` | `-1.0` (constant) | Most dangerous — full brake/throttle signal |
| `Horizontal` | Small non-zero values | Varies by system |
| Other joystick axes | Small non-zero values | Hardware-dependent |

### Root Cause

This is a **known Windows platform behavior**, not a Unity bug. Windows reports default/resting values for gamepad axes through DirectInput/XInput even when no physical device is present. The Legacy Input Manager passes these values through without filtering.

### Key Insight

- This does **not** happen on all machines or all Windows versions consistently
- The values are **constant** (no variance frame-to-frame) — this is the key diagnostic signal
- Deadzones alone do NOT fix this: a phantom value of `-1.0` blows past any reasonable deadzone
- The New Input System (`com.unity.inputsystem`) handles device presence detection better, but if you are using the Legacy Input Manager, you must defend against this manually

---

## Quick Reference: Input Bug Triage

```
Vehicle moves with no player touching anything?
  |
  v
Attach InputDiagnostics, enter Play mode, hands off controller
  |
  v
Are axis values constant (no variance)?
  YES -> Phantom input. Check:
    1. Is TriggerDetector detecting the stuck axis? (variance < 0.02)
    2. Is mode gating returning 0 for Detecting/None modes?
    3. Are ALL axes gated, not just the one you noticed first?
  NO  -> Real input from a connected device. Check:
    1. Is a controller actually connected? (check Device Manager)
    2. Is the controller drifting? (hardware issue)
    3. Are deadzones too small for the noise level?
```

---

## Related Skills

| Skill | Relevance |
|-------|-----------|
| `unity-input-system` | New Input System (alternative to Legacy Input Manager) — handles device presence natively |
| `unity-testing-debugging-qa` | Testing philosophy, TDD cycle, test infrastructure |
| `unity-testing-patterns` | Test code examples, parameterized tests, assertions |
| `reverse-engineering` | Chain-of-custody debugging methodology |
| `debug-system` | Debug overlay and logging architecture |
| `clean-room-qa` | Black-box testing from function signatures |


## Topic Pages

- [Section 2: Three-Layer Input Defense](skill-section-2-three-layer-input-defense.md)

