# Assets/Scripts/Vehicle/Physics/

Pure math functions extracted from vehicle MonoBehaviours for testability.
No Unity lifecycle dependency — these are static utility classes.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `SuspensionMath.cs` | `SuspensionMath` | Hooke's law spring force, damping, bump stop, grip load |
| `GripMath.cs` | `GripMath` | Slip ratio, lateral/longitudinal force, traction modes, RPM |
| `DrivetrainMath.cs` | `DrivetrainMath` | Differential force distribution (Open/BallDiff/Spool), AWD center diff |
| `AirPhysicsMath.cs` | `AirPhysicsMath` | Pitch/roll torque, gyroscopic damping |
| `TumbleMath.cs` | `TumbleMath` | Tumble detection with smoothstep and hysteresis |
| `ESCMath.cs` | `ESCMath` | ESC ground drive logic: engine cutoff, braking, reverse, coast drag |

## Design Principle

MonoBehaviours in the parent `Vehicle/` folder delegate physics calculations to these
static classes. This separation allows:
- Unit testing without Play mode or MonoBehaviour instantiation
- Reuse of formulas across different vehicle types
- Clear documentation of force units (Newtons, N·m)

## Relevant Skills

- **`unity-physics-3d`** — Raycast physics, force models, suspension math
- **`unity-testing-patterns`** — All physics math has mandatory unit tests
- **`clean-room-qa`** — Black-box physics testing from domain first principles
