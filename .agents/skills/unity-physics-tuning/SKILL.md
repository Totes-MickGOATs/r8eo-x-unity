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


## Topic Pages

- [PhysX Project Settings](skill-physx-project-settings.md)
- [Layer Collision Matrix](skill-layer-collision-matrix.md)
- [Common Bugs and Fixes](skill-common-bugs-and-fixes.md)
- [Contact Modification API](skill-contact-modification-api.md)
- [Scale Issues at 1/10 Scale](skill-scale-issues-at-110-scale.md)
- [Deterministic Physics](skill-deterministic-physics.md)

