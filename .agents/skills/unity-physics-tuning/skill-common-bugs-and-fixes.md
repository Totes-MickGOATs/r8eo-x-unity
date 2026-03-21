# Common Bugs and Fixes

> Part of the `unity-physics-tuning` skill. See [SKILL.md](SKILL.md) for the overview.

## Common Bugs and Fixes

### Vehicle Flips on Curbs

**Symptom:** Car launches into the air when hitting a curb edge.
**Cause:** Single contact point at curb edge creates massive torque.
**Fixes:**
- Lower center of mass
- Add a "belly" collider (flat box under chassis) to catch curb edges
- Increase solver iterations to 12+
- Add angular velocity damping when airborne

```csharp
void FixedUpdate()
{
    if (!IsGrounded())
    {
        rb.angularVelocity *= 0.98f; // Gentle air damping
    }
}
```

### Jitter at Rest

**Symptom:** Vehicle vibrates when stationary on a surface.
**Cause:** Spring force oscillation at low compression, or sleep threshold preventing stable rest.
**Fixes:**
- Increase damping ratio in suspension
- Lower sleep threshold
- Add dead zone to spring force when compression < 0.01
- Increase solver iterations

### Tunneling Through Barriers

**Symptom:** Fast-moving car passes through thin walls.
**Cause:** Discrete collision detection + high velocity + thin collider.
**Fixes:**
- Use `ContinuousSpeculative` on the vehicle
- Increase barrier collider thickness (add invisible collider behind visible mesh)
- Increase physics timestep to 200Hz
- Use SphereCast for wheel contact (won't miss thin ground)

### Suspension Oscillation

**Symptom:** Vehicle bounces continuously, never settling.
**Cause:** Underdamped spring, or timestep too large for spring stiffness.
**Fixes:**
- Increase damping coefficient (target damping ratio 0.6-0.8)
- Reduce spring stiffness
- Increase physics Hz (100 -> 200)
- Clamp spring force to prevent overshoot

```csharp
// Critical damping calculation
float criticalDamping = 2f * Mathf.Sqrt(springStiffness * rb.mass / 4f); // Per wheel
float dampingCoeff = criticalDamping * dampingRatio; // 0.6-0.8 for slightly underdamped
```

### Weight Transfer Issues

**Symptom:** Car doesn't pitch forward on braking or roll in corners.
**Cause:** CoM too low, spring too stiff, or forces applied at wrong position.
**Fixes:**
- Verify forces applied via `AddForceAtPosition` at wheel contact point
- Reduce spring stiffness for more travel
- Raise CoM slightly (tradeoff: more transfer but less stable)

### Ghost/Replay Divergence

**Symptom:** Replay ghost follows a different path than the original.
**Cause:** Non-deterministic physics state, different timesteps, or scene load order.
**Fixes:**
- Enable Enhanced Determinism
- Use `SimulationMode.Script`
- Record physics state snapshots periodically for correction
- Use input recording, not position recording

---

## Consolidated Recommended Settings

Quick-reference for a 1/10-scale RC racing project:

### Project Settings > Physics

| Setting | Value |
|---------|-------|
| Gravity | (0, -9.81, 0) |
| Default Solver Iterations | 12 |
| Default Solver Velocity Iterations | 4 |
| Solver Type | TGS |
| Bounce Threshold | 0.5 |
| Default Contact Offset | 0.005 |
| Sleep Threshold | 0.002 |
| Default Max Angular Speed | 50 |
| Auto Sync Transforms | Off |
| Reuse Collision Callbacks | On |
| Enhanced Determinism | On |

### Project Settings > Time

| Setting | Value |
|---------|-------|
| Fixed Timestep | 0.005 (200 Hz) |
| Maximum Allowed Timestep | 0.02 |

### Vehicle Rigidbody

| Property | Value |
|----------|-------|
| Mass | 1.2 kg |
| Linear Damping | 0.1 |
| Angular Damping | 0.5 |
| Max Angular Velocity | 50 |
| Interpolation | Interpolate |
| Collision Detection | ContinuousSpeculative |
| Center of Mass | (0, -0.02, 0.01) manual |

### Suspension (Raycast-based)

| Parameter | Value |
|-----------|-------|
| Ray/SphereCast length | 0.06-0.08 m |
| Spring stiffness | 800-1200 N/m |
| Damping ratio | 0.6-0.8 of critical |
| Cast radius (SphereCast) | 0.01 m |
| Surface layer mask | Track_Surface only |

---

