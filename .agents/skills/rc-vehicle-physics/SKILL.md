---
name: rc-vehicle-physics
description: Adds or modifies physics math in Assets/Scripts/Vehicle/Physics/ (SuspensionMath, GripMath, DrivetrainMath, GyroscopicMath, ESCMath, TumbleMath) and wires results into RaycastWheel.cs or RCCar.cs. Use when the user says 'change spring', 'fix grip curve', 'tune suspension', 'drivetrain behavior', 'gyro torque', or modifies any file under Assets/Scripts/Vehicle/Physics/. Key capabilities: physics invariant enforcement, AnimationCurve grip sampling, force conservation assertions, per-axle differential logic, and EditMode test coverage in Assets/Tests/EditMode/. Do NOT use for ScriptableObject config (SuspensionConfig, TractionConfig) or Editor-only scripts.
---

# RC Vehicle Physics

## Critical

- **Physics invariants — never violate:**
  - Suspension force `>= 0` (no tension): always `Mathf.Max(0f, force)`
  - No grip without normal load: guard `if (gripLoad <= 0f) return 0f`
  - Differential conserves force: `leftShare + rightShare == axleForce` (assert in tests)
- All physics math lives in **pure static classes** — no MonoBehaviour, no state, no Unity API calls
- Lateral and longitudinal grip forces use **negative sign** (they oppose motion): `F = -velocity * factor * coeff * load`
- No bare numeric literals — use `const float k_PascalCase` for every magic number
- Tests are mandatory (RED → GREEN → commit). Run `just test` before pushing.

## Examples

**User says:** "Front spring rate should be 700 N/m, rear 350 N/m"

1. Read `SuspensionMath.cs` — find `ComputeSuspensionForce` signature
2. Read `RaycastWheel.cs` — find `[SerializeField] float _springStrength`
3. Write test: `ComputeSuspensionForce_FrontAxle700_MatchesExpectedForce` → RED
4. Add `k_FrontSpring = 700f`, `k_RearSpring = 350f` constants in `RaycastWheel.cs`
5. Assign in `Awake()` based on axle tag — `_springStrength = IsRearAxle ? k_RearSpring : k_FrontSpring`
6. `just test` → GREEN
7. Commit: `feat: per-axle spring rates — front 700 N/m / rear 350 N/m from real B6.4 specs`

## Common Issues

**`CS0117: 'Physics' does not contain a definition for 'Raycast'`**
→ Namespace collision between `UnityEngine.Physics` and `R8EOX.Vehicle.Physics`. Add `using UnityEngine;` and qualify: `UnityEngine.Physics.SphereCast(...)`.

**Test fails with `NullReferenceException` on `AnimationCurve`**
→ GripMath tests that use a curve must construct it: `var curve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(1f, 0.7f));`

**Suspension force goes negative in play mode**
→ Missing `Mathf.Max(0f, ...)` clamp. Check every path in `ComputeSuspensionForce` and `ComputeSuspensionForceWithDamping` — the damping term can produce negative total if `prevLen < curLen` (extension stroke).

**Differential test: `left + right != total` by small epsilon**
→ Floating-point drift from separate multiply paths. Fix by computing one share and deriving the other: `right = total - left`.

**`just test` runs no tests after adding new test file**
→ New test assembly `.asmdef` must reference `R8EOX.Vehicle.Physics` and `NUnit.Framework`. Check `Assets/Tests/EditMode/R8EOX.Tests.EditMode.asmdef`.

## Topic Pages

- [Instructions](skill-instructions.md)

