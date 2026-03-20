---
name: unity-physics-tuning
description: Unity PhysX Tuning for RC Racing
---

# Unity PhysX Tuning for RC Racing

Use this skill when configuring Unity's PhysX engine for realistic 1/10-scale RC car simulation. Covers project settings, timestep, collision layers, Rigidbody configuration, raycast vs WheelCollider, scale fixes, determinism, and common physics bugs.

## When to Use

- Configuring Project Settings > Physics for an RC racing game
- Diagnosing vehicle jitter, tunneling, flip-on-curb, or suspension oscillation
- Choosing between WheelCollider and raycast-based suspension
- Setting up collision layers and physics materials for track surfaces
- Tuning Rigidbody parameters for 1/10-scale vehicles
- Implementing deterministic physics for replay or ghost systems

## When NOT to Use

- General Unity physics concepts (colliders, joints, raycasting basics) -- use `unity-physics-3d`
- Shader or visual effects work -- use `unity-3d-materials` or `unity-shaders`
- Networking/multiplayer physics sync -- use `unity-networking`
- Non-vehicle physics (ragdolls, destruction, cloth)

---

## PhysX Project Settings

Configure in **Edit > Project Settings > Physics**.

| Setting | Recommended Value | Why |
|---------|------------------|-----|
| Solver Type | **TGS (Temporal Gauss-Seidel)** | Better convergence for vehicle constraints than PGS |
| Default Solver Iterations | **10-12** | Higher = more stable joints/contacts. Below 8 causes jitter on suspensions |
| Default Solver Velocity Iterations | **4-6** | Controls velocity-level correction. 4 is minimum for stable wheel contact |
| Bounce Threshold | **0.5-1.0** | Velocities below this won't bounce. Prevents micro-bouncing on track surface |
| Default Contact Offset | **0.005-0.01** | Smaller than default (0.01) for 1/10 scale. Too small = missed contacts |
| Sleep Threshold | **0.001-0.005** | Lower than default (0.005) so small RC cars don't sleep prematurely |
| Default Max Angular Speed | **50** | RC cars spin fast; default 7 rad/s clips wheel rotation |
| Auto Sync Transforms | **Off** | Manual sync for performance; call `Physics.SyncTransforms()` when needed |
| Reuse Collision Callbacks | **On** | Reduces GC allocation from collision events |
| Enhanced Determinism | **On** (if replay/ghosts needed) | See Deterministic Physics section |

### Fixed Timestep

Set in **Edit > Project Settings > Time > Fixed Timestep**.

| Timestep | Hz | Use Case |
|----------|----|----------|
| 0.02 | 50 Hz | **Too coarse for vehicles.** Suspension oscillates, wheels tunnel through thin colliders |
| 0.01 | 100 Hz | **Minimum acceptable.** Stable for casual RC sim |
| 0.005 | 200 Hz | **Preferred for RC racing.** Smooth suspension, accurate tire contact, no tunneling |
| 0.002 | 500 Hz | Overkill for most cases. Only if doing sub-mm precision sim |

**Why 50 Hz fails for vehicles:** At 50 Hz each physics step moves a vehicle traveling 20 m/s by 0.4m. A thin curb collider (2cm) is completely skipped. Suspension springs with high stiffness overshoot in a single step, causing oscillation. RC cars at 1/10 scale compound this -- colliders are 10x thinner than full-scale.

```csharp
// Set via script if needed
Time.fixedDeltaTime = 0.005f; // 200 Hz
```

---

## Collision Detection Modes

Set per-Rigidbody via `Rigidbody.collisionDetectionMode`.

| Mode | Cost | When to Use |
|------|------|-------------|
| **Discrete** | Lowest | Static objects, slow-moving debris, track barriers |
| **Continuous** | Medium | Vehicle chassis -- prevents tunneling through thin barriers |
| **ContinuousDynamic** | High | Fast projectiles, wheels at high RPM if using collider-based wheels |
| **ContinuousSpeculative** | Medium | Good compromise for vehicle body. Uses speculative CCD -- cheaper than full CCD but can allow some ghost collisions |

**RC racing recommendation:** Vehicle chassis = `Continuous` or `ContinuousSpeculative`. Track geometry = `Discrete` (static).

```csharp
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
```

---

## Layer Collision Matrix

Define layers in **Edit > Project Settings > Tags and Layers**, then configure the matrix in Physics settings.

### Recommended Layers

| Layer | Index (example) | Purpose |
|-------|----------------|---------|
| Vehicle | 8 | Chassis, body shell |
| Wheel | 9 | Wheel colliders or raycast origins |
| Track_Surface | 10 | Drivable track surface mesh |
| Track_Barrier | 11 | Walls, tire stacks, fences |
| Track_Trigger | 12 | Lap triggers, sector triggers, checkpoints |
| Debris | 13 | Loose parts, kicked-up rocks |
| Effects | 14 | Particle collision planes, dust zones |

### Collision Matrix

|  | Vehicle | Wheel | Surface | Barrier | Trigger | Debris | Effects |
|--|---------|-------|---------|---------|---------|--------|---------|
| **Vehicle** | Yes | No | Yes | Yes | Yes | Yes | No |
| **Wheel** | -- | No | Yes | No | No | No | No |
| **Surface** | -- | -- | No | No | No | No | No |
| **Barrier** | -- | -- | -- | No | No | Yes | No |
| **Trigger** | -- | -- | -- | -- | No | No | No |
| **Debris** | -- | -- | -- | -- | -- | No | No |
| **Effects** | -- | -- | -- | -- | -- | -- | No |

Key decisions:
- **Vehicle-Vehicle: Yes** -- cars collide with each other
- **Vehicle-Wheel: No** -- wheels are children of the vehicle, no self-collision
- **Wheel-Surface: Yes** -- wheels need surface contact for grip calculation
- **Vehicle-Trigger: Yes** -- lap counting, sector timing via `OnTriggerEnter`

```csharp
// Set collision matrix via script
Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Vehicle"), LayerMask.NameToLayer("Effects"), true);
```

---

## Rigidbody Configuration for RC Cars

> **Unity 6:** `Rigidbody.drag` was renamed to `Rigidbody.linearDamping` and `Rigidbody.angularDrag` was renamed to `Rigidbody.angularDamping`. The old names are removed and will cause compile errors.

### Recommended Settings

```csharp
// Vehicle chassis Rigidbody setup
rb.mass = 1.2f;                    // 1.0-1.5 kg (real 1/10 RC buggy)
rb.linearDamping = 0.1f;           // Low linear damping -- aero drag modeled separately
rb.angularDamping = 0.5f;          // Moderate -- prevents infinite spinning after impacts
rb.maxAngularVelocity = 50f;       // Default 7 is way too low for RC
rb.interpolation = RigidbodyInterpolation.Interpolate;
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
rb.centerOfMass = new Vector3(0f, -0.02f, 0.01f); // Low and slightly forward
```

### Center of Mass

**Always set manually.** Unity computes CoM from collider geometry, which is wrong for an RC car (battery is low, motor is rear, electronics are centered).

```csharp
// Set in Awake() or via inspector
rb.centerOfMass = centerOfMassOffset; // Local space offset

// Visualize in editor
void OnDrawGizmos()
{
    if (TryGetComponent<Rigidbody>(out var rb))
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.01f);
    }
}
```

Effects of CoM position:
- **Lower** = more stable, less likely to flip on curbs
- **Forward** = more front grip, less oversteer
- **Rear** = more rear grip, more responsive turn-in but tail-happy

### Inertia Tensor

For non-symmetric vehicles, manually set the inertia tensor to prevent unrealistic rotation:

```csharp
// Approximate inertia for a 1/10 RC buggy (0.4m x 0.15m x 0.2m, 1.2kg)
rb.inertiaTensor = new Vector3(0.003f, 0.005f, 0.004f);
rb.inertiaTensorRotation = Quaternion.identity;
```

---

## Raycast vs WheelCollider

### Comparison

| Aspect | WheelCollider | Raycast Suspension |
|--------|--------------|-------------------|
| Setup complexity | Low (built-in) | Medium (manual spring/damper) |
| Customizability | Limited -- black-box friction model | Full control over every parameter |
| 1/10 scale support | Poor -- designed for full-size cars, tuning is fragile at small scale | Excellent -- scale-independent math |
| Multi-surface friction | Hacky (runtime `WheelFrictionCurve` swaps) | Natural (raycast hit returns surface, apply friction directly) |
| Suspension travel visualization | Requires manual sync | Direct control |
| Determinism | Less predictable | Fully controllable |
| Performance | Slightly better (native) | Slightly worse (managed raycasts) |

**Verdict for RC sim: Raycast wins.** WheelCollider's internal friction model is a black box that fights you at 1/10 scale. Raycast suspension gives full control over spring force, damping, tire slip, and surface interaction.

### Raycast Suspension Pattern

```csharp
void FixedUpdate()
{
    foreach (var wheel in wheels)
    {
        if (Physics.Raycast(wheel.origin, -transform.up, out RaycastHit hit, wheel.maxLength, surfaceMask))
        {
            // Spring force (Hooke's law)
            float compression = 1f - (hit.distance / wheel.maxLength);
            float springForce = compression * wheel.springStiffness;

            // Damping force
            float velocity = (wheel.previousLength - hit.distance) / Time.fixedDeltaTime;
            float dampForce = velocity * wheel.dampingCoefficient;

            float totalForce = springForce + dampForce;
            rb.AddForceAtPosition(transform.up * totalForce, wheel.origin, ForceMode.Force);

            wheel.previousLength = hit.distance;
            wheel.isGrounded = true;
            wheel.surfaceType = hit.collider.sharedMaterial; // For friction lookup
        }
        else
        {
            wheel.isGrounded = false;
            wheel.previousLength = wheel.maxLength;
        }
    }
}
```

### SphereCast for Suspension

Use `Physics.SphereCast` instead of `Physics.Raycast` for more stable ground contact at small scale:

```csharp
float radius = 0.01f; // Tire contact patch radius
if (Physics.SphereCast(wheel.origin, radius, -transform.up, out RaycastHit hit,
    wheel.maxLength - radius, surfaceMask))
{
    // More stable contact normal on uneven terrain
    // Prevents "needle" raycast missing thin geometry
}
```

---

## Physics Materials per Surface

> **Unity 6:** \ was renamed to \ (with an 's') and \ was renamed to \. The old names are removed and will cause compile errors.

Create `PhysicsMaterial` assets for each surface type.

| Surface | Dynamic Friction | Static Friction | Bounciness | Combine Mode |
|---------|-----------------|----------------|------------|-------------|
| Asphalt | 0.8 | 1.0 | 0.05 | Average |
| Dirt (packed) | 0.6 | 0.7 | 0.02 | Average |
| Dirt (loose) | 0.4 | 0.5 | 0.01 | Average |
| Gravel | 0.35 | 0.45 | 0.03 | Average |
| Grass | 0.5 | 0.6 | 0.01 | Average |
| Mud | 0.25 | 0.3 | 0.0 | Average |

**Note:** If using raycast suspension (recommended), these PhysicsMaterials are only for chassis-barrier collisions. Tire friction is calculated in your custom tire model using the surface type from the raycast hit.

```csharp
// Surface lookup from raycast hit
PhysicsMaterial surfaceMat = hit.collider.sharedMaterial;
float gripMultiplier = surfaceConfig.GetGrip(surfaceMat); // Your lookup table
```

---

## Contact Modification API

For advanced tire friction, use `Physics.ContactModifyEvent` (Unity 2022.2+):

```csharp
void OnEnable()
{
    Physics.ContactModifyEvent += OnContactModify;
}

void OnContactModify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
{
    for (int i = 0; i < pairs.Length; i++)
    {
        var pair = pairs[i];
        // Modify friction, restitution, or contact normal per-contact
        for (int j = 0; j < pair.contactCount; j++)
        {
            pair.SetDynamicFriction(j, customFriction);
            pair.SetStaticFriction(j, customStaticFriction);
        }
    }
}
```

**Use case:** Runtime friction that varies per-contact based on tire slip angle, surface wetness, or tire temperature. More accurate than per-material friction for racing.

---

## Scale Issues at 1/10 Scale

Unity PhysX is tuned for human-scale (1 unit = 1 meter). At 1/10 scale, several defaults break.

### Problems and Fixes

| Problem | Cause | Fix |
|---------|-------|-----|
| Objects fall through floor | Contact offset too large relative to collider | Reduce `Physics.defaultContactOffset` to 0.005 |
| Vehicle sleeps while moving slowly | Sleep threshold too high for small velocities | Set `Rigidbody.sleepThreshold = 0.001` |
| Suspension oscillates wildly | Spring forces overshoot at default timestep | Use 200Hz timestep (0.005s) |
| Wheels clip through thin curbs | Discrete collision misses thin geometry | Use `ContinuousSpeculative` + SphereCast |
| Unrealistic bounce | Bounce threshold too low | Set `Physics.bounceThreshold = 0.5` |
| Angular velocity clamped | Default `maxAngularVelocity = 7` too low | Set to 50 on vehicle Rigidbody |

### Contact Offset

```csharp
// Global default
Physics.defaultContactOffset = 0.005f; // Half the default

// Per-collider override
collider.contactOffset = 0.003f; // Even smaller for wheel contact
```

---

## Manual Physics.Simulate

For substep simulation or replay systems:

```csharp
// Disable auto-simulation
Physics.simulationMode = SimulationMode.Script;

void FixedUpdate()
{
    int substeps = 4;
    float substepDt = Time.fixedDeltaTime / substeps;
    for (int i = 0; i < substeps; i++)
    {
        ApplyForces(substepDt);
        Physics.Simulate(substepDt);
    }
}
```

**Replay system:** Record inputs per-frame, then replay by calling `Physics.Simulate` with the same inputs and timestep. Requires Enhanced Determinism.

---

## Deterministic Physics

### What Enhanced Determinism Does

When enabled (`Physics.enhancedDeterminism = true` or via Project Settings):
- Same inputs + same initial state = same simulation result **on the same machine, same build**
- PhysX processes bodies in a deterministic order
- Floating-point operations use consistent ordering

### What It Does NOT Guarantee

- **Cross-platform determinism** -- x86 vs ARM, different GPU drivers, or different Unity versions may diverge
- **Cross-build determinism** -- Debug vs Release, IL2CPP vs Mono may differ due to floating-point optimization flags
- **Scene load order independence** -- objects must be instantiated in the same order

### Requirements for Deterministic Replay

1. Enhanced Determinism ON
2. `SimulationMode.Script` -- manual `Physics.Simulate` calls
3. Fixed timestep (no `Time.deltaTime` in physics code)
4. Identical object creation order
5. No `Destroy()` during replay -- disable instead
6. Record/replay all external inputs (steering, throttle, collisions)

---

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

## Conformance Audit

After any physics tuning change, run the conformance checks and record results via `ConformanceRecorder`:

```csharp
ConformanceRecorder.BeginRun();
// ... run checks ...
ConformanceRecorder.Record("B", "B6", "Normal force sum at rest", 14.715, measuredForceSum);
ConformanceRecorder.EndRun();
```

Results are persisted to the local SQLite DB (`Logs/physics_audit.db`) for trend tracking and regression detection. See the `physics-conformance-audit` skill for the full catalogue of 93 checks across 12 categories.

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-physics-3d`** | General Unity physics (colliders, joints, raycasting basics) -- this skill builds on top of it |
| **`clean-room-qa`** | Testing methodology for verifying physics tuning changes |
