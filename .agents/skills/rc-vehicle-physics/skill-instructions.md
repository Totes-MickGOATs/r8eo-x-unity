# Instructions

> Part of the `rc-vehicle-physics` skill. See [SKILL.md](SKILL.md) for the overview.

## Instructions

### 1. Locate the correct math class

| Change type | File |
|---|---|
| Spring / damping / ray length | `Assets/Scripts/Vehicle/Physics/SuspensionMath.cs` |
| Slip ratio / lateral / longitudinal grip | `Assets/Scripts/Vehicle/Physics/GripMath.cs` |
| Differential / axle split / center diff | `Assets/Scripts/Vehicle/Physics/DrivetrainMath.cs` |
| Throttle / brake / reverse / coast drag | `Assets/Scripts/Vehicle/Physics/ESCMath.cs` |
| Gyroscopic precession torque | `Assets/Scripts/Vehicle/Physics/GyroscopicMath.cs` |
| Flip / tumble detection | `Assets/Scripts/Vehicle/Physics/TumbleMath.cs` |

Read the target file before changing anything.

### 2. Follow the static-class pattern exactly

```csharp
using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>One-line description of what this class computes.</summary>
    public static class SuspensionMath
    {
        // Named constants — k_ prefix, no bare literals anywhere
        const float k_BumpStopSlop = 0.01f;

        /// <summary>XML doc on every public method.</summary>
        public static float ComputeSuspensionForce(
            float springStrength, float restDistance, float currentLength, float maxSpringForce)
        {
            float rawForce = springStrength * (restDistance - currentLength);
            return Mathf.Clamp(rawForce, 0f, maxSpringForce); // invariant: force >= 0
        }
    }
}
```

- Method prefix: always `Compute*`
- Namespace: `R8EOX.Vehicle.Physics`
- Return structs for multi-value results (see `AxleSplit`, `DriveResult` patterns below)

### 3. Use structs for multi-value returns

```csharp
// DrivetrainMath pattern
public readonly struct AxleSplit
{
    public float LeftShare { get; }
    public float RightShare { get; }
    public AxleSplit(float left, float right) { LeftShare = left; RightShare = right; }
}
```

Verify: `left + right == input force` in your test before proceeding.

### 4. Wire result into RaycastWheel.cs or RCCar.cs

- **RaycastWheel** calls suspension and grip math per wheel — add new math call after the existing `SuspensionMath.Compute*` call chain
- **RCCar** calls drivetrain and ESC math — find the existing `DrivetrainMath.Compute*` call and add adjacent
- Pass config values (spring strength, damping, grip coeff) from `[SerializeField]` fields — never hardcode in the MonoBehaviour
- Log via tagged debug: `Debug.Log($"[physics] {value}")` for conformance audit auto-capture

### 5. Write the EditMode test FIRST (RED)

```csharp
// Assets/Tests/EditMode/SuspensionMathTests.cs  (or GripMathTests.cs, etc.)
using NUnit.Framework;
using R8EOX.Vehicle.Physics;

[TestFixture]
public class SuspensionMathTests
{
    // Mirror constants from ADR-001 — do not import from production code
    const float k_SpringStrength = 700f;   // front axle
    const float k_RestDistance   = 0.20f;
    const float k_MaxSpringForce = 50f;

    [Test]
    public void ComputeSuspensionForce_FullyCompressed_ClampsToMaxForce()
    {
        float result = SuspensionMath.ComputeSuspensionForce(
            k_SpringStrength, k_RestDistance, currentLength: 0f, k_MaxSpringForce);
        Assert.AreEqual(k_MaxSpringForce, result, 0.0001f);
    }

    [Test]
    public void ComputeSuspensionForce_FullyExtended_ReturnsZero()
    {
        float result = SuspensionMath.ComputeSuspensionForce(
            k_SpringStrength, k_RestDistance, currentLength: 1.0f, k_MaxSpringForce);
        Assert.AreEqual(0f, result, 0.0001f); // invariant: no tension
    }
}
```

- Naming: `MethodName_Condition_ExpectedOutcome`
- Tolerances: `0.0001f` for pure math, `0.01f` for computed floats, `0.1–0.5f` for integrated physics
- Every new `Compute*` method needs: ≥1 normal case + ≥1 boundary/zero case + invariant case
- Run: `just test` — confirm RED, implement, confirm GREEN

### 6. Confirm physics invariants in tests

```csharp
// Force conservation — required for every differential test
[Test]
public void ComputeAxleSplit_Open_ConservesForce()
{
    var split = DrivetrainMath.ComputeAxleSplit(engineForce: 20f, speedDiff: 0f, diffType: 0);
    Assert.AreEqual(20f, split.LeftShare + split.RightShare, 0.0001f);
}
```

