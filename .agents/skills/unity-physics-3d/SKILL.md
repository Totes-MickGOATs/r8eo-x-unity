---
name: unity-physics-3d
description: Unity 3D Physics
---


# Unity 3D Physics

Use this skill when configuring Rigidbodies, colliders, joints, raycasts, or physics materials in Unity's PhysX-based 3D physics system.

## FixedUpdate — The Physics Loop

**All physics code goes in `FixedUpdate`, never `Update`.**

```csharp
void FixedUpdate()
{
    // Runs at fixed intervals (default 50Hz = 0.02s)
    // Use Time.fixedDeltaTime for calculations
    rb.AddForce(Vector3.forward * speed * Time.fixedDeltaTime, ForceMode.Force);
}

void Update()
{
    // Variable framerate — for input reading, visuals, UI
    // NEVER apply forces or move rigidbodies here
    float h = Input.GetAxis("Horizontal"); // Read input here
}
```

### Timestep Configuration

```csharp
// Project Settings > Time > Fixed Timestep
Time.fixedDeltaTime = 0.02f;   // 50 Hz (default)
Time.fixedDeltaTime = 0.01f;   // 100 Hz (more accurate, more CPU)
```

For vehicle or fighting games, consider 100 Hz. For casual games, 50 Hz is fine.

## Physics Materials

> **Unity 6:** \ was renamed to \ (with an 's') and \ was renamed to \. The menu item is now **Assets > Create > Physics Material**.

Create via **Assets > Create > Physics Material**.

```csharp
PhysicsMaterial mat = new PhysicsMaterial("Ice");
mat.staticFriction = 0.05f;     // Friction when stationary
mat.dynamicFriction = 0.03f;    // Friction when moving
mat.bounciness = 0.1f;          // 0 = no bounce, 1 = perfect bounce
mat.frictionCombine = PhysicsMaterialCombine.Minimum;   // How two materials combine
mat.bounceCombine = PhysicsMaterialCombine.Maximum;

collider.material = mat;
```

### Combine Modes

| Mode | Result |
|------|--------|
| Average | (a + b) / 2 |
| Minimum | min(a, b) |
| Maximum | max(a, b) |
| Multiply | a * b |

Priority: Average < Minimum < Multiply < Maximum. The higher-priority mode wins.

## PhysX Settings

**Edit > Project Settings > Physics:**

| Setting | Default | Notes |
|---------|---------|-------|
| Gravity | (0, -9.81, 0) | Change for moon/space games |
| Default Solver Iterations | 6 | Increase for stable joints/stacking |
| Default Solver Velocity Iterations | 1 | Increase for jitter-free contacts |
| Auto Sync Transforms | true | Set false for performance, call `Physics.SyncTransforms()` manually |
| Reuse Collision Callbacks | true | Reduces GC in collision callbacks |

## Performance Tips

1. **Use primitive colliders** over MeshColliders whenever possible
2. **Pre-allocate raycast buffers** with `NonAlloc` variants in hot paths
3. **Disable `Auto Sync Transforms`** and call `Physics.SyncTransforms()` once per frame
4. **Increase Fixed Timestep** (0.02 to 0.03) if physics load is high and precision is not critical
5. **Use layers** to minimize collision pair checks
6. **Sleep thresholds** — PhysX automatically sleeps idle bodies; don't wake them unnecessarily
7. **Avoid `GetComponent` in collision callbacks** — cache references in `Awake`


## Topic Pages

- [Rigidbody Fundamentals](skill-rigidbody-fundamentals.md)
- [Applying Forces](skill-applying-forces.md)
- [Common Gotchas](skill-common-gotchas.md)

