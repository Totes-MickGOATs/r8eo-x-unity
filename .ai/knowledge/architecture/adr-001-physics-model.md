# ADR-001: Raycast-Based Wheel Physics with Hooke's Law Suspension

## Status
Accepted

## Context
R8EO-X is a realistic RC buggy racing simulator operating at **1/1 physical scale** — the Unity scene uses values ×10 larger than the Godot 1/10th scale reference prototype. This scaling was done to align Unity's collision/physics/visual proportions with the Godot reference while preserving the underlying dynamics.

We are porting a proven physics implementation from a Godot 4.6.1 prototype (in `RCGameProject/`) that drives well and has been tuned over multiple iterations. The key question is which Unity physics approach to use.

### Scale Note
The Godot prototype modelled a 1/10th scale RC buggy (mass 1.5 kg, wheel radius ~0.042 m). The Unity production build uses values scaled ×10 to achieve physical consistency with Unity's engine proportions:

| Parameter | Godot Reference (1/10) | Unity Production (1/1 ×10) |
|---|---|---|
| Mass | 1.5 kg | 15 kg |
| Wheel radius (front) | 0.0425 m (Proline Electron 2.2" front OD 85 mm) | 0.425 m |
| Wheel radius (rear)  | 0.0420 m (Proline Electron 2.2" rear OD 84 mm)  | 0.420 m |
| Wheelbase | 1.36 m | 13.6 m (wheel pivots ±6.8 m) |
| Track width | 1.00 m | 10.0 m (wheel pivots ±5.0 m) |
| Spring strength — front | 75 N/m  | **700 N/m** (B6.4 red spring, 4.0 lbs/in) |
| Spring strength — rear  | 75 N/m  | **350 N/m** (B6.4 gray spring, 2.0 lbs/in) |
| Spring damping — front  | 4.25 N·s/m | **41 N·s/m** (c_crit × 0.40 at k=700, m_wheel=3.75 kg) |
| Spring damping — rear   | 4.25 N·s/m | **29 N·s/m** (c_crit × 0.40 at k=350, m_wheel=3.75 kg) |
| Rest distance         | 0.20 m  | **0.25 m** (springLen_eq: front 0.208 m, rear 0.124 m) |
| Over-extend (droop)   | 0.08 m  | **0.24 m** (B6.4 droop 28.5mm ×10 − sag) |
| Min spring len (bump) | 0.032 m | **0.12 m** (springLen_eq − 0.075m bump compression) |
| Wheel MoI | 0.000120 kg·m² | 0.120 kg·m² |
| CoM ground offset (Y) | −0.20 m (Godot, CoM ≈5 cm above ground) | **−0.12 m** (CoM 0.50m above ground at 10× scale) |

Spring rates are now per-axle based on real Team Associated B6.4 spring specs. Front axle uses red spring (4.0 lbs/in = 700 N/m), rear uses gray spring (2.0 lbs/in = 350 N/m). Weight split is 40% front / 60% rear (rear-drive 2WD buggy). Equilibrium sag: front = 0.4×15×9.81/(2×700) = 0.042 m → springLen_eq 0.208 m; rear = 0.6×15×9.81/(2×350) = 0.126 m → springLen_eq 0.124 m.

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