# ADR-001: Raycast-Based Wheel Physics with Hooke's Law Suspension

## Status
Accepted

## Context
R8EO-X is a realistic RC buggy racing simulator operating at **1/1 physical scale** — the Unity scene uses values ×10 larger than the Godot 1/10th scale reference prototype. This scaling was done to align Unity's collision/physics/visual proportions with the Godot reference while preserving the underlying dynamics.

We are porting a proven physics implementation from a Godot 4.6.1 prototype (in `RCGameProject/`) that drives well and has been tuned over multiple iterations. The key question is which Unity physics approach to use.

### Scale Note
The Godot prototype modelled a 1/10th scale RC buggy (mass 1.5 kg, wheel radius 0.166 m). The Unity production build uses values scaled ×10 to achieve physical consistency with Unity's engine proportions:

| Parameter | Godot Reference (1/10) | Unity Production (1/1 ×10) |
|---|---|---|
| Mass | 1.5 kg | 15 kg |
| Wheel radius | 0.166 m | 1.66 m |
| Wheelbase | 1.36 m | 13.6 m (wheel pivots ±6.8 m) |
| Track width | 1.00 m | 10.0 m (wheel pivots ±5.0 m) |
| Spring strength | 75 N/m | 75 N/m (unchanged — spring rate does not scale) |
| Spring damping | 4.25 N·s/m | 4.25 N·s/m (unchanged) |
| Rest distance | 0.20 m | 2.0 m |
| Wheel MoI | 0.000120 kg·m² | 0.120 kg·m² |

Spring rate and damping are left at Godot values because they already produce the desired suspension feel at the Unity scale; rescaling them by 10× would over-stiffen the ride.

### Options Considered

1. **Unity WheelCollider** — built-in vehicle physics component
2. **Raycast-based custom physics** — our own suspension, grip, and drivetrain model
3. **Third-party asset** (e.g., Edy's Vehicle Physics, NWH Vehicle Physics)

### Why Not WheelCollider?
- Designed for full-size cars, not RC-scale vehicles (mass: 15 kg at 1/1 production scale)
- Limited control over suspension behavior — can't implement our specific Hooke's law + damping model
- Grip model uses simplified Pacejka curves — we need direct curve-sampled slip ratios
- No support for RC-specific behaviors: reverse ESC state machine, tumble detection, airborne gyro stabilization
- Debugging is opaque — hard to visualize individual force contributions

### Why Not Third-Party?
- Adds external dependency to a core system
- Would need heavy modification to support RC-scale physics anyway
- Our Godot prototype already has a proven, well-understood model

## Decision
Use **raycast-based wheel physics** with:

- **Suspension:** Hooke's law (`F = k × compression + c × velocity_change / dt`), no-tension clamp, bump stops
- **Grip:** AnimationCurve sampled by lateral slip ratio → grip factor, multiplied by grip coefficient and spring load
- **Drivetrain:** Open/BallDiff/Spool differentials with configurable preload, RWD/AWD layouts
- **Air physics:** Pitch/roll torques from throttle/steer inputs, gyroscopic stabilization from wheel RPM
- **Ground detection:** Physics.Raycast from wheel anchor downward, with normal validation

Each wheel operates as a `RaycastWheel` MonoBehaviour that computes and applies forces at the ground contact point via `Rigidbody.AddForceAtPosition()`.

## Consequences

### Positive
- **Full control** over every aspect of the physics model
- **Proven model** — direct port from working Godot prototype
- **Debuggable** — force arrows at contact points (yellow=suspension, red=lateral, green=longitudinal, cyan=motor)
- **Testable** — physics formulas are pure math, easily unit-tested with known inputs
- **Tunable** — all parameters exposed as serialized fields or ScriptableObject configs
- **RC-authentic** — supports reverse ESC, tumble physics, motor presets, differential types

### Negative
- **More code to maintain** than using WheelCollider (~700 lines across 4 scripts)
- **Must handle edge cases ourselves** — terrain clipping, invalid normals, ramp sliding
- **No built-in integration** with Unity's vehicle tools or terrain system

### Neutral
- Physics runs in FixedUpdate at Unity's fixed timestep (default 50 Hz) — same as WheelCollider would
- Performance is comparable — 4 raycasts + force math per frame is lightweight