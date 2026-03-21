# Layer Collision Matrix

> Part of the `unity-physics-tuning` skill. See [SKILL.md](SKILL.md) for the overview.

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

