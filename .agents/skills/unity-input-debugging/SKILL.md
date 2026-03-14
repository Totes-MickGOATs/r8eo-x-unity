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

## Section 2: Three-Layer Input Defense

A single defense layer is insufficient. Phantom values at `-1.0` defeat deadzones. Mode gating alone leaves gaps during transitions. Use all three layers together.

### Layer 1: TriggerDetector (Variance-Based Rejection)

Detect whether a combined trigger axis is reporting real input or a stuck phantom value by measuring variance over a window of frames:

```
Algorithm:
- Sample the axis value every frame during a detection window (e.g., 300 frames)
- Compute jitter = max(samples) - min(samples)
- If jitter < threshold (0.02): axis is STUCK -> phantom value -> reject it
- If jitter >= threshold: axis shows real human variance -> accept it
```

A real human cannot hold a trigger at exactly `-1.000` for 300 consecutive frames. Variance-based detection exploits this.

### Layer 2: Mode Gating

The input controller operates in distinct modes. **During detection and when no controller is found, all vehicle input channels must return zero.**

| Mode | Throttle | Brake | Steering | Description |
|------|----------|-------|----------|-------------|
| **Detecting** | `0` | `0` | `0` | Observing axes, no output |
| **None** | `0` | `0` | `0` | No controller detected |
| **Separate** | Live | Live | Live | Throttle/brake on separate axes |
| **Combined** | Live | Live | Live | Throttle/brake on combined trigger axis |

### Layer 3: Deadzones

Apply deadzones as the final filter on live input values:

| Axis | Deadzone | Rationale |
|------|----------|-----------|
| Triggers (throttle/brake) | 0.15 | Filters analog stick noise and light resting pressure |
| Steering | 0.20 | Wider zone to prevent drift from stick centering imprecision |

**Deadzones are necessary but not sufficient.** They catch small noise values but cannot reject a phantom `-1.0`. The variance detector (Layer 1) handles that case.

---

## Section 3: The "Detecting Phase" Trap

### The Anti-Pattern

During the detection window (e.g., 300 frames of observation), it is tempting to read raw axis values for throttle/brake to provide "early" or "responsive" input:

```csharp
// BAD: Reading raw input during detection
public float GetThrottle()
{
    // This returns phantom values during Detecting mode!
    float raw = Input.GetAxis("RightTrigger");
    return ApplyDeadzone(raw, _triggerDeadzone);
}
```

This leaks phantom values as real vehicle input. The car accelerates or brakes with no player action.

### The Principle

> **Detection observes, never drives.** The detection phase exists to classify axes. It must NEVER produce vehicle input. All input channels return zero until detection completes and confirms a valid controller.

### The Fix

```csharp
public float GetThrottle()
{
    if (_currentMode == InputMode.Detecting || _currentMode == InputMode.None)
        return 0f;  // Hard zero — no exceptions

    float raw = Input.GetAxis("RightTrigger");
    return ApplyDeadzone(raw, _triggerDeadzone);
}
```

Every public input accessor (throttle, brake, steering) must gate on mode **before** reading any axis.

---

## Section 4: Diagnostic Pattern (InputDiagnostics)

### Setup

Create a diagnostic MonoBehaviour script in the `Assets/` root (not in an Editor asmdef — Editor scripts do not run in Play mode):

```csharp
using UnityEngine;

public class InputDiagnostics : MonoBehaviour
{
    private Rigidbody _rb;
    private int _frameCount;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        _frameCount++;
        if (_frameCount % 30 == 0)
        {
            float t = Input.GetAxis("RightTrigger");
            float b = Input.GetAxis("LeftTrigger");
            float s = Input.GetAxis("Horizontal");
            Vector3 v = _rb != null ? _rb.velocity : Vector3.zero;

            Debug.Log($"[InputDiag] F:{_frameCount} T:{t:F4} B:{b:F4} S:{s:F4} " +
                      $"V:({v.x:F2},{v.y:F2},{v.z:F2}) Spd:{v.magnitude:F2}");
        }
    }
}
```

### Attachment Rules

- Attach to the vehicle GameObject **before** entering Play mode (cannot add MonoBehaviours during Play mode)
- `Application.runInBackground` must be `true` for MCP-driven testing (otherwise Unity pauses when unfocused)
- The script must NOT be in an Editor-only assembly (asmdef with `includePlatforms: ["Editor"]`) — those assemblies are stripped from Play mode

### Reading the Logs

| Log Pattern | Diagnosis |
|-------------|-----------|
| `T:-1.0000` constant across all frames | Phantom input on trigger axis |
| `T:` varies between frames (e.g., -0.98, -1.0, -0.97) | Real human input |
| Value snaps from non-zero to `0.0000` at a specific frame | Detection phase completed and mode gating activated |
| `Spd:` increasing while T/B/S show constant phantom values | Phantom values leaking into vehicle physics |
| `Spd:0.00` while T/B/S show phantom values | Defense layers working correctly |

### Key Diagnostic Signals

- **Constant value = phantom input.** Real humans introduce variance.
- **Value that never changes across hundreds of frames = stuck/phantom.** Even a finger resting on a trigger produces micro-jitter.
- **Speed increasing with no input variance = phantom leak.** The defense layers have a gap.

---

## Section 5: TDD for Input Bugs

### The Comprehensive Test Matrix

Input bugs require testing ALL modes crossed with ALL axes. Fixing one axis or one mode and declaring victory is the primary anti-pattern.

| | Throttle | Brake | Steering |
|---|----------|-------|----------|
| **Detecting** | Must be 0 | Must be 0 | Must be 0 |
| **None** | Must be 0 | Must be 0 | Must be 0 |
| **Separate** | Live | Live | Live |
| **Combined** | Live | Live | Live |

### Unit Tests

```csharp
[TestCase(InputMode.Detecting)]
[TestCase(InputMode.None)]
public void AllAxes_ReturnZero_InNonActiveMode(InputMode mode)
{
    // Arrange
    var controller = CreateControllerInMode(mode);

    // Act & Assert
    Assert.AreEqual(0f, controller.GetThrottle(), "Throttle must be zero in " + mode);
    Assert.AreEqual(0f, controller.GetBrake(), "Brake must be zero in " + mode);
    Assert.AreEqual(0f, controller.GetSteering(), "Steering must be zero in " + mode);
}
```

### Integration Tests

```csharp
[UnityTest]
public IEnumerator Car_DoesNotMove_WithNoInput()
{
    // Arrange: spawn car, wait for detection phase to complete
    yield return new WaitForSeconds(6f); // 300 frames at 50fps

    // Record position
    Vector3 startPos = car.transform.position;

    // Act: wait with zero input
    yield return new WaitForSeconds(3f);

    // Assert: car has not moved
    float distance = Vector3.Distance(startPos, car.transform.position);
    Assert.Less(distance, 0.1f, "Car moved without input — phantom values leaking");
}
```

### The Anti-Pattern: Serial Symptom Chasing

What happened in the original debugging session (4 PRs):

1. **PR 1:** Fixed throttle phantom values, did not test brake or steering
2. **PR 2:** Found brake had the same issue, fixed it, did not test during detection phase
3. **PR 3:** Found detection phase leaked values, fixed it, did not test all modes
4. **PR 4:** Comprehensive test matrix finally caught remaining edge cases

**This should have been 1 PR.** Write the full test matrix first (red), then implement all defenses (green). The test matrix IS the specification.

---

## Section 6: Common Gotchas

### Editor Assembly Scripts Do Not Run in Play Mode

Scripts in an assembly definition with `includePlatforms: ["Editor"]` are compiled only for the Unity Editor environment. They are **not** available during Play mode. If you put a diagnostic MonoBehaviour in an Editor asmdef, it will silently not execute.

**Fix:** Place diagnostic scripts in the runtime assembly or in the `Assets/` root (which is part of the default runtime assembly `Assembly-CSharp`).

### Rigidbody API Version Differences

In older Unity versions (pre-2023), use `_rb.velocity` to read the rigidbody's velocity vector. In newer versions (2023+), the property is `_rb.linearVelocity`. Using the wrong one produces a compile error.

**Check your Unity version first.** In this project, use `_rb.velocity`.

### MCP Domain Reload Disconnects

When Unity performs a domain reload (triggered by script compilation), the MCP connection may drop. Cascading script changes — editing multiple files that trigger sequential recompiles — increase the chance of disconnection.

**Mitigation:** Make all script changes, then trigger a single recompile. Avoid rapid-fire small edits to multiple files.

### InputManager Axis Definitions

`Input.GetAxis("SomeAxis")` returns 0 silently if the axis is not defined in the InputManager settings. When polling joystick axes for diagnostics, ensure the axes exist in **Edit > Project Settings > Input Manager**.

The default Unity project template defines basic axes but NOT all gamepad-specific axes (e.g., `CombinedTriggers`, `RightTrigger`, `LeftTrigger`). You may need to add these manually.

### Application.runInBackground

For MCP-driven or automated testing, set `Application.runInBackground = true` in a startup script or in **Player Settings > Resolution and Presentation**. Without this, Unity pauses the game loop when the Editor window loses focus, causing tests driven by external tools (MCP, CLI) to hang.

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
