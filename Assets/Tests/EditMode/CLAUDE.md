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
| `GripTractionCriticalTests.cs` | C1-C4 regression tests: grip curve baseline, damped grip load, friction direction, ramp sliding | `SuspensionMath`, `GripMath`, `RaycastWheel` |
| `TuningApiTests.cs` | Setter API for runtime tuning: motor, suspension, traction, crash, CoM, mass | `RCCar` setters |
| `InputMathTests.cs` | Deadzone remapping, steering curve, input merging | `InputMath` |
| `InputDetectionTests.cs` | Trigger detection grace period, sustained input confirmation, symmetric deadzone | `TriggerDetector`, `InputGuard`, `InputMath` |
| `PhantomTriggerTests.cs` | Phantom trigger bug: constant combined axis rejection, combined trigger throttle/brake helpers | `TriggerDetector`, `InputMath` |
| `GroundDriveTests.cs` | ESC ground drive logic: engine cutoff, braking, reverse, coast drag | `ESCMath` |

## Running Tests

```bash
just test  # Runs EditMode tests
```

Or in Unity: Window → General → Test Runner → EditMode → Run All

## Relevant Skills

- **`unity-testing-patterns`** — TDD with Unity Test Framework
- **`unity-testing-debugging-qa`** — Testing, debugging, and quality assurance workflows
- **`clean-room-qa`** — Independent QA validation process
