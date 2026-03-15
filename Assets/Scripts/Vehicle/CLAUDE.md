# Assets/Scripts/Vehicle/

RC car vehicle system: MonoBehaviour controllers that wire together physics, drivetrain, and wheel simulation.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `RCCar.cs` | `RCCar` | Root vehicle controller — reads input, applies forces, exposes tuning API |
| `RaycastWheel.cs` | `RaycastWheel` | SphereCast-based wheel: suspension, grip, surface detection. Uses `Physics.SphereCast` with radius `k_SphereCastRadius = 0.015f` to smooth contact normals over terrain triangle edges (anti-snag). |
| `Drivetrain.cs` | `Drivetrain` | Differential force distribution across wheels |
| `RCAirPhysics.cs` | `RCAirPhysics` | Airborne physics: gyroscopic precession and reaction torque (physics-first) |
| `R8EOX.Vehicle.asmdef` | — | Assembly definition for the Vehicle system |

## Subdirectories

| Dir | Purpose |
|-----|---------|
| `Physics/` | Pure static math classes extracted from MonoBehaviours for testability |
| `Config/` | ScriptableObject configurations for motor, suspension, and traction tuning |

## Architecture

- MonoBehaviours in this folder delegate calculations to static classes in `Physics/`
- Configuration comes from ScriptableObjects in `Config/` (with inline fallback defaults)
- Namespace: `R8EOX.Vehicle`

## Relevant Skills

- **`unity-physics-3d`** — Raycast wheel physics, force application, suspension models
- **`unity-composition`** — Component architecture and MonoBehaviour patterns
