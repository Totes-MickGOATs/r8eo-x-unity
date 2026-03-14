# System Overview

Architecture map for the R8EO-X RC Racing Simulator.

---

## Scene Graph

```
TestTrack.unity [Root scene]
├── RCBuggy (Rigidbody) [RCCar, RCInput]
│   ├── Collision shapes (BoxColliders for chassis, bumpers, body shell)
│   ├── Visual meshes (ChassisPlate, BodyShell, RearWing, ControlArms)
│   ├── WheelFL (RaycastWheel) [isSteer=true]
│   │   ├── WheelVisual (CylinderMesh r=0.166)
│   │   └── HubVisual
│   ├── WheelFR (RaycastWheel) [isSteer=true]
│   │   ├── WheelVisual
│   │   └── HubVisual
│   ├── WheelRL (RaycastWheel) [isMotor=true]
│   │   ├── WheelVisual
│   │   └── HubVisual
│   ├── WheelRR (RaycastWheel) [isMotor=true]
│   │   ├── WheelVisual
│   │   └── HubVisual
│   ├── AirPhysics (RCAirPhysics)
│   └── Drivetrain (Drivetrain) [driveLayout=RWD]
├── Camera (CameraController) [target=RCBuggy.transform, modes=Chase/Orbit/FPV/Trackside]
├── TelemetryHUD (TelemetryHUD) [car=RCBuggy]
├── SurfaceZones (trigger colliders with SurfaceConfig references)
├── Terrain / Track geometry
└── Lighting (Directional Light, Skybox)
```

---

## Scripts Inventory

| File | Class | Namespace | Base | Role |
|------|-------|-----------|------|------|
| `Vehicle/RCCar.cs` | `RCCar` | `R8EOX.Vehicle` | `MonoBehaviour` | Vehicle orchestrator: motor, steering, tumble, airborne |
| `Vehicle/RaycastWheel.cs` | `RaycastWheel` | `R8EOX.Vehicle` | `MonoBehaviour` | Per-wheel physics: suspension, grip, friction, motor force |
| `Vehicle/Drivetrain.cs` | `Drivetrain` | `R8EOX.Vehicle` | `MonoBehaviour` | Differential coupling (Open/BallDiff/Spool, RWD/AWD) |
| `Vehicle/RCAirPhysics.cs` | `RCAirPhysics` | `R8EOX.Vehicle` | `MonoBehaviour` | Airborne pitch/roll/gyro torques |
| `Vehicle/Physics/SuspensionMath.cs` | `SuspensionMath` | `R8EOX.Vehicle.Physics` | static | Hooke's law, bump stop, grip load |
| `Vehicle/Physics/GripMath.cs` | `GripMath` | `R8EOX.Vehicle.Physics` | static | Slip ratio, lateral/longitudinal force, RPM |
| `Vehicle/Physics/DrivetrainMath.cs` | `DrivetrainMath` | `R8EOX.Vehicle.Physics` | static | Axle diff split, AWD center diff |
| `Vehicle/Physics/AirPhysicsMath.cs` | `AirPhysicsMath` | `R8EOX.Vehicle.Physics` | static | Pitch/roll torque, gyro damping |
| `Vehicle/Physics/TumbleMath.cs` | `TumbleMath` | `R8EOX.Vehicle.Physics` | static | Smoothstep, hysteresis, tilt angle |
| `Vehicle/Config/MotorPresetConfig.cs` | `MotorPresetConfig` | `R8EOX.Vehicle.Config` | `ScriptableObject` | Motor preset data asset |
| `Vehicle/Config/SuspensionConfig.cs` | `SuspensionConfig` | `R8EOX.Vehicle.Config` | `ScriptableObject` | Suspension tuning data asset |
| `Vehicle/Config/TractionConfig.cs` | `TractionConfig` | `R8EOX.Vehicle.Config` | `ScriptableObject` | Grip/traction data asset |
| `Input/RCInput.cs` | `RCInput` | `R8EOX.Input` | `MonoBehaviour` | Player input: keyboard + gamepad, implements IVehicleInput |
| `Input/IVehicleInput.cs` | `IVehicleInput` | `R8EOX.Input` | interface | Swappable input source contract |
| `Input/InputMath.cs` | `InputMath` | `R8EOX.Input` | static | Deadzone, steering curve, input merging |
| `Camera/CameraController.cs` | `CameraController` | `R8EOX.Camera` | `MonoBehaviour` | Multi-mode camera (Chase/Orbit/FPV/Trackside) |
| `Camera/CameraMode.cs` | `CameraMode` | `R8EOX.Camera` | enum | Camera mode enumeration |
| `Camera/TracksideAnchor.cs` | `TracksideAnchor` | `R8EOX.Camera` | `MonoBehaviour` | Scene marker for trackside camera |
| `Core/SurfaceType.cs` | `SurfaceType` | `R8EOX.Core` | enum | Surface type enumeration (Dirt, Gravel, etc.) |
| `Core/SurfaceConfig.cs` | `SurfaceConfig` | `R8EOX.Core` | `ScriptableObject` | Surface friction properties |
| `Track/SurfaceZone.cs` | `SurfaceZone` | `R8EOX.Track` | `MonoBehaviour` | Trigger zone for surface grip modifiers |
| `Debug/TelemetryHUD.cs` | `TelemetryHUD` | `R8EOX.Debug` | `MonoBehaviour` | OnGUI telemetry overlay |
| `Editor/SceneSetup.cs` | `SceneSetup` | `R8EOX.Editor` | static | Editor scene/prefab builder |

---

## Data Flow

### Physics Frame Pipeline (FixedUpdate)

```
RCInput.Update()                          ← Polls keyboard/gamepad via InputMath
  │
RCCar.FixedUpdate(dt)                    ← Main physics orchestrator
  ├── CheckAirborne()                     ← 5-frame threshold
  ├── ComputeTumbleFactor()               ← delegates to TumbleMath
  ├── Read input from IVehicleInput
  ├── Ramp throttle (smoothThrottle)
  ├── if AIRBORNE:
  │   └── RCAirPhysics.Apply()
  │       ├── AirPhysicsMath.ComputePitchTorque()
  │       ├── AirPhysicsMath.ComputeRollTorque()
  │       └── AirPhysicsMath.ComputeGyroDampingFactor()
  ├── if GROUNDED:
  │   ├── ApplyGroundDrive() → reverse ESC, engine/brake force
  │   ├── Drivetrain.Distribute() → DrivetrainMath.ComputeAxleSplit()
  │   └── ApplySteering() → speed-dependent reduction
  └── For each RaycastWheel:
      └── wheel.ApplyWheelPhysics(rb, dt)
          ├── Raycast → ground contact
          ├── SuspensionMath.ComputeSpringLength()
          ├── SuspensionMath.ComputeSuspensionForceWithDamping()
          ├── SuspensionMath.ComputeGripLoad()
          ├── GripMath.ComputeSlipRatio() → gripCurve.Evaluate()
          ├── GripMath.ComputeLateralForceMagnitude()
          ├── GripMath.ComputeEffectiveTraction()
          ├── GripMath.ComputeLongitudinalForceMagnitude()
          ├── AddForceAtPosition(total, contactPoint)
          └── GripMath.ComputeWheelRpm()
```

### Camera Pipeline (LateUpdate)

```
CameraController.Update()
  ├── Check mode cycle key (C) → CycleMode()
  └── If Orbit: read mouse/stick → update yaw/pitch

CameraController.LateUpdate()
  ├── If transitioning: smooth interpolate between modes
  └── Apply current mode:
      ├── Chase: Lerp behind car, LookAt car
      ├── Orbit: Spherical offset from yaw/pitch
      ├── FPV: Lock to car body with local offset
      └── Trackside: Static position (nearest anchor), LookAt car
```

---

## Dependency Graph

```
R8EOX.Core (no dependencies)
  └── SurfaceType, SurfaceConfig

R8EOX.Input (no dependencies)
  └── IVehicleInput, InputMath, RCInput

R8EOX.Vehicle (depends on: Input, Core)
  ├── RCCar → uses IVehicleInput, TumbleMath
  ├── RaycastWheel → uses SuspensionMath, GripMath
  ├── Drivetrain → uses DrivetrainMath (inline, not yet delegated)
  ├── RCAirPhysics → uses AirPhysicsMath
  └── Config/ → MotorPresetConfig, SuspensionConfig, TractionConfig

R8EOX.Camera (no dependencies)
  └── CameraController, CameraMode, TracksideAnchor

R8EOX.Track (depends on: Core)
  └── SurfaceZone → references SurfaceConfig

R8EOX.Debug (depends on: Vehicle)
  └── TelemetryHUD → reads RCCar properties

R8EOX.Editor (depends on: Vehicle, Input, Camera, Debug, Core, Track)
  └── SceneSetup → builds test scene and prefab

R8EOX.Tests.EditMode (depends on: Vehicle, Input)
  └── SuspensionMathTests, GripMathTests, DrivetrainMathTests,
      AirPhysicsMathTests, TumbleMathTests, InputMathTests
```

---

## Architecture Decision Records

### ADR-001: Raycast-Based Wheel Physics

**Status:** Accepted
**Decision:** Use raycast-based wheel physics with Hooke's law suspension and curve-sampled grip model
**Details:** See `adr-001-physics-model.md`
