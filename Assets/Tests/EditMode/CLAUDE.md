# Assets/Tests/EditMode/

Edit Mode unit tests for physics math, input processing, and game systems. No MonoBehaviour instantiation needed — all tests exercise pure static/logic classes.

## Files

| File | Tests | Coverage |
|------|-------|----------|
| `SphereCastTests.cs` | SphereCast radius constant accessible and in valid range (0.010–0.020m) | `RaycastWheel` |
| `SuspensionMathTests.cs` | Hooke's law, damping, bump stop, grip load, ray length | `SuspensionMath` |
| `SuspensionStabilityTests.cs` | M7 landing damping spike suppression, M8 oscillation decay | `SuspensionMath` |
| `GripMathTests.cs` | Slip ratio, lateral force, longitudinal friction, traction modes, RPM | `GripMath` |
| `DrivetrainMathTests.cs` | Open/BallDiff/Spool diffs, one-wheel-off, AWD center diff, force conservation | `DrivetrainMath` |
| `AirPhysicsMathTests.cs` | Pitch/roll torque, gyro damping, RPM averaging (deprecated) | `AirPhysicsMath` |
| `GyroscopicMathTests.cs` | Gyroscopic precession torque, reaction torque, wheel angular velocity | `GyroscopicMath` |
| `TumbleMathTests.cs` | Smoothstep, hysteresis, airborne zeroing, tilt angle | `TumbleMath` |
| `GripTractionCriticalTests.cs` | C1-C4 regression tests: grip curve baseline, damped grip load, friction direction, ramp sliding | `SuspensionMath`, `GripMath`, `RaycastWheel` |
| `TuningApiTests.cs` | Setter API for runtime tuning: motor, suspension, traction, crash, CoM, mass | `RCCar` setters |
| `InputMathTests.cs` | Deadzone remapping, steering curve, input merging | `InputMath` |
| `InputDetectionTests.cs` | Trigger detection grace period, sustained input confirmation, symmetric deadzone | `TriggerDetector`, `InputGuard`, `InputMath` |
| `PhantomTriggerTests.cs` | 12 tests: phantom trigger bug — constant combined axis rejection, combined trigger throttle/brake helpers | `TriggerDetector`, `InputMath` |
| `ZeroInputTests.cs` | 20 tests: zero-input pipeline — phantom values, detecting-phase contracts, all modes x all axes | `TriggerDetector`, `InputMath`, `RCInput` |
| `GroundDriveTests.cs` | ESC ground drive logic: engine cutoff, braking, reverse, coast drag | `ESCMath` |
| `BlackBoxPhysicsTests.cs` | Black-box tests for all public physics functions using realistic 1/10 RC values | `SuspensionMath`, `GripMath`, `DrivetrainMath`, `AirPhysicsMath` |
| `ForceDirectionTests.cs` | Force direction verification — catches axis mapping bugs from Godot-to-Unity port | `SuspensionMath`, `GripMath` |
| `InputProcessingTests.cs` | Input edge cases: complements InputMathTests with bug-catching scenarios | `InputMath` |
| `ReverseESCTests.cs` | Reverse ESC state machine: engage/disengage transitions, coast drag | `ESCMath` |
| `SteeringTests.cs` | Steering direction and speed-dependent angle reduction | `RCCar` steering math |
| `GameFlowStateMachineTests.cs` | Game flow state machine transitions | `GameFlowStateMachine` |
| `NavigationStackTests.cs` | Navigation stack push/pop behavior | `NavigationStack` |
| `SceneRegistryTests.cs` | Scene registry validation | `SceneRegistry` |
| `SessionConfigTests.cs` | Session configuration data | `SessionConfig` |
| `GameFlowManagerTests.cs` | Singleton guard, transitions, session, navigation, BootDirectToPlaying | `GameFlowManager` |
| `SceneBootstrapperTests.cs` | Standalone vs full-flow detection, manager creation | `SceneBootstrapper` |
| `ScreenRegistryTests.cs` | Screen lookup by ID, missing entries, null prefab | `ScreenRegistry` (UI) |
| `UIManagerTests.cs` | Initialization null checks, overlay count | `UIManager` |
| `ContractDebuggerTests.cs` | Toggle defaults, counter reset, null-ref safety, SetTarget API, DetectorMode exposure and contract wiring | `ContractDebugger`, `RCInput`, `TriggerDetector` |
| `WheelTerrainDiagnosticsTests.cs` | Wheel discovery lifecycle: normal order, no wheels, no car, Start order race condition | `WheelTerrainDiagnostics`, `RCCar` |
| `DebugBootstrapTests.cs` | Component attachment, no-duplication guard, null-safe no-RCCar case, log assertion | `DebugBootstrap`, `ContractDebugger`, `WheelTerrainDiagnostics`, `InputDiagnostics` |

## Running Tests

```bash
just test  # Runs EditMode tests
```

Or in Unity: Window → General → Test Runner → EditMode → Run All

## Relevant Skills

- **`unity-testing-patterns`** — TDD with Unity Test Framework
- **`unity-testing-debugging-qa`** — Testing, debugging, and quality assurance workflows
- **`clean-room-qa`** — Independent QA validation process
