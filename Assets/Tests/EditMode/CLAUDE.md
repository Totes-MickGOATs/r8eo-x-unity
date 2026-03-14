# Assets/Tests/EditMode/

Edit Mode unit tests for pure physics math. No MonoBehaviour instantiation needed.

## Files

| File | Tests | Coverage |
|------|-------|----------|
| `SuspensionMathTests.cs` | Hooke's law, damping, bump stop, grip load, ray length | `SuspensionMath` |
| `GripMathTests.cs` | Slip ratio, lateral force, longitudinal friction, traction modes, RPM | `GripMath` |
| `DrivetrainMathTests.cs` | Open/BallDiff/Spool diffs, one-wheel-off, AWD center diff, force conservation | `DrivetrainMath` |
| `AirPhysicsMathTests.cs` | Pitch/roll torque, gyro damping, RPM averaging | `AirPhysicsMath` |
| `TumbleMathTests.cs` | Smoothstep, hysteresis, airborne zeroing, tilt angle | `TumbleMath` |

## Running Tests

```bash
just test  # Runs EditMode tests
```

Or in Unity: Window → General → Test Runner → EditMode → Run All
