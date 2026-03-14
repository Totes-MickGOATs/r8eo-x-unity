# ADR-001: Raycast-Based Wheel Physics with Hooke's Law Suspension

## Status
Accepted

## Context
R8EO-X is a realistic 1/10th scale RC buggy racing simulator. The physics model is the core of the game — it must feel authentic to real RC racing while being computationally efficient and fully controllable by the developer.

We are porting a proven physics implementation from a Godot 4.6.1 prototype (in `RCGameProject/`) that drives well and has been tuned over multiple iterations. The key question is which Unity physics approach to use.

### Options Considered

1. **Unity WheelCollider** — built-in vehicle physics component
2. **Raycast-based custom physics** — our own suspension, grip, and drivetrain model
3. **Third-party asset** (e.g., Edy's Vehicle Physics, NWH Vehicle Physics)

### Why Not WheelCollider?
- Designed for full-size cars, not 1/10th scale RC vehicles (mass: 1.5 kg)
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