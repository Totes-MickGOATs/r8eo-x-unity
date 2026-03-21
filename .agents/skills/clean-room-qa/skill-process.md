# Process

> Part of the `clean-room-qa` skill. See [SKILL.md](SKILL.md) for the overview.

## Process

### Step 1: Extract Signatures Only

Read ONLY the public API surface. For each class, extract:
```
ClassName.MethodName(param1: Type, param2: Type, ...) → ReturnType
```

DO NOT read the method body. If you accidentally see implementation details, ignore them.

### Step 2: Determine Domain Context

For each function, answer:
- What physical/logical concept does this represent?
- What are the units? (Newtons, metres, radians, m/s, etc.)
- What are the valid input ranges?
- What invariants must hold? (conservation laws, monotonicity, bounds, signs)

### Step 3: Write Test Categories

For each function, write tests in these categories:

| Category | Description | Example |
|----------|-------------|---------|
| **Happy Path** | Normal inputs, expected output | Compressed spring → positive force |
| **Zero Input** | All zeros or neutral state | No velocity → no friction force |
| **Boundary** | At exact thresholds | Speed exactly at deadzone edge |
| **Sign/Direction** | Verify forces oppose motion, torques rotate correctly | Lateral force opposes sideways velocity |
| **Conservation** | Total in = total out | Differential force split sums to input |
| **Monotonicity** | More input → more output (where expected) | More compression → more spring force |
| **Symmetry** | Left/right, positive/negative behave symmetrically | Left steer = negative of right steer |
| **Extreme** | Very large or very small values | Near-zero speed, maximum force |
| **Invariant** | Rules that must ALWAYS hold | Suspension force ≥ 0 (no tension) |
| **Independence** | Changing one param doesn't affect unrelated outputs | Changing grip doesn't affect suspension |

### Step 4: Name Tests Descriptively

Pattern: `FunctionName_PhysicalScenario_ExpectedOutcome`

Good: `ComputeSuspensionForce_SpringFullyExtended_ReturnsZero`
Bad: `TestSuspension1`

### Step 5: Write Assertions

Use exact values where physics gives an exact answer:
```csharp
// F = k * x = 75 * 0.05 = 3.75 N
Assert.AreEqual(3.75f, result, 0.01f);
```

Use relational assertions where the exact value depends on implementation but the direction/sign is known:
```csharp
// Lateral force must oppose lateral velocity
Assert.Less(force, 0f, "Force should oppose positive lateral velocity");
```

Use invariant assertions where a rule must always hold:
```csharp
// Suspension never pulls
Assert.GreaterOrEqual(force, 0f, "Suspension must never produce tension");
```

## Test File Template

```csharp
using NUnit.Framework;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box tests for [SystemName].
    /// Written with ZERO knowledge of implementation.
    /// Tests derived from physical principles and domain knowledge.
    /// </summary>
    public class [SystemName]BlackBoxTests
    {
        // Use realistic values for a 1/10th scale RC car
        const float k_Mass = 1.5f;           // kg
        const float k_WheelRadius = 0.166f;  // m
        const float k_Gravity = 9.81f;       // m/s²
        const float k_WeightPerWheel = k_Mass * k_Gravity / 4f; // ~3.68 N

        [Test]
        public void FunctionName_Scenario_Expected()
        {
            // ARRANGE: Set up physically meaningful inputs
            // ACT: Call the function
            // ASSERT: Verify physically correct outcome
        }
    }
}
```

## Domain Knowledge Reference (RC Car Physics)

### Suspension (Hooke's Law)
- F_spring = stiffness × compression (positive when compressed)
- F_damping = damping × compression_velocity (positive resists compression)
- F_total = max(F_spring + F_damping, 0) — NEVER negative (no tension)
- Compression = restDistance - currentLength (positive when shorter than rest)

### Tire Grip
- Slip ratio = |lateral_velocity| / total_speed (0 = no slip, 1 = full slide)
- Lateral force OPPOSES sideways motion (restores straight-line travel)
- Longitudinal friction OPPOSES forward/backward motion (rolling resistance)
- Static friction is HIGH when stopped (prevents rolling on slopes)
- No grip without normal load (no suspension force = no grip)

### Drivetrain
- Open differential: always 50/50 split regardless of wheel speeds
- Ball differential: transfers force from faster wheel to slower wheel, up to preload limit
- Spool: fully locked, maximum coupling
- CONSERVATION: left_share + right_share = total_input (energy cannot be created or destroyed)
- One wheel off ground: ALL force goes to grounded wheel

### Air Physics
- Throttle in air: nose pitches UP (wheel spin reaction torque)
- Brake in air: nose pitches DOWN
- Steering in air: car ROLLS (counter-roll into the turn)
- Gyroscopic stabilization: spinning wheels resist tumbling (higher RPM = more stability)
- Gyro allows yaw (Y-axis rotation is free)

### Tumble Detection
- Tilt angle: 0° = upright, 90° = on side, 180° = inverted
- Tumble engages above threshold (e.g., 50°)
- Tumble uses smoothstep blending (smooth transition, not binary)
- Hysteresis prevents oscillation at threshold
- Airborne = no tumble (tumble is a ground-contact concept)

### Input
- Deadzone: small stick deflections are zeroed to prevent drift
- Remapping: after deadzone, output is 0 at edge and 1 at full deflection (no jump)
- Steering curve: exponent > 1 gives more precision near center
- Input merging: keyboard and gamepad combined, larger absolute value wins
