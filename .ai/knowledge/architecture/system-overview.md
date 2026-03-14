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
├── Terrain / Track geometry
└── Lighting (Directional Light, Skybox)
```

---

## Scripts Inventory

| File | Class | Namespace | Base | Role |
|------|-------|-----------|------|------|
| `Scripts/Vehicle/RCCar.cs` | `RCCar` | — | `MonoBehaviour` | Main vehicle controller: motor presets, throttle ramping, steering, tumble detection, reverse ESC, airborne management |
| `Scripts/Vehicle/RaycastWheel.cs` | `RaycastWheel` | — | `MonoBehaviour` | Per-wheel physics: Hooke's law suspension, curve-sampled grip, longitudinal friction, motor force, visual update |
| `Scripts/Vehicle/Drivetrain.cs` | `Drivetrain` | — | `MonoBehaviour` | Differential coupling and drive layout (Open/BallDiff/Spool, RWD/AWD) |
| `Scripts/Vehicle/RCAirPhysics.cs` | `RCAirPhysics` | — | `MonoBehaviour` | Airborne pitch/roll torques from throttle/steer, gyroscopic stabilization from wheel spin |
| `Scripts/Input/RCInput.cs` | `RCInput` | — | `MonoBehaviour` | Input abstraction: keyboard WASD + gamepad triggers with auto-detection, steering curve |
| `Scripts/Camera/CameraController.cs` | `CameraController` | — | `MonoBehaviour` | Multi-mode camera: Chase, Orbit, FPV, Trackside with smooth transitions |
| `Scripts/Camera/CameraMode.cs` | `CameraMode` | — | `enum` | Camera mode enumeration (Chase, Orbit, Fpv, Trackside) |
| `Scripts/Camera/TracksideAnchor.cs` | `TracksideAnchor` | — | `MonoBehaviour` | Scene marker for trackside camera positions |
| `Scripts/Camera/ChaseCamera.cs` | `ChaseCamera` | — | `MonoBehaviour` | Legacy chase camera (deprecated, kept for migration) |
| `Scripts/Debug/TelemetryHUD.cs` | `TelemetryHUD` | — | `MonoBehaviour` | OnGUI telemetry overlay: speed, forces, per-wheel state, toggle with F2 |
| `Scripts/Editor/SceneSetup.cs` | `SceneSetup` | — | — | Editor utilities for scene configuration |

> **Note:** No namespaces are currently declared. This is a Phase 1 task: add `R8EOX.*` namespaces + assembly definitions.

---

## Data Flow

### Physics Frame Pipeline (FixedUpdate)

```
RCInput.Update()                    ← Polls keyboard/gamepad each frame
  │
RCCar.FixedUpdate(dt)              ← Main physics orchestrator
  ├── CheckAirborne()               ← All wheels off ground for 5+ frames?
  ├── ComputeTumbleFactor()          ← Tilt angle → smoothstep → physics material blend
  ├── Read input (throttle, brake, steer) from RCInput
  ├── Ramp throttle (smoothThrottle) ← Prevents instant full power
  ├── if AIRBORNE:
  │   ├── Zero engine/brake forces
  │   └── RCAirPhysics.Apply(dt, throttle, brake, steer)
  │       ├── Pitch torque (throttle → nose up, brake → nose down)
  │       ├── Roll torque (steer → lateral roll)
  │       └── Gyro damping (wheel RPM → angular velocity damping)
  ├── if GROUNDED:
  │   ├── ApplyGroundDrive(dt, throttle, brake, speed)
  │   │   ├── Reverse ESC state machine
  │   │   ├── Engine force = throttle × engineForceMax
  │   │   ├── Brake force or coast drag
  │   │   └── Sets currentEngineForce, currentBrakeForce
  │   ├── Drivetrain.Distribute(engineForce, frontWheels, rearWheels)
  │   │   ├── Split force by drive layout (RWD/AWD)
  │   │   ├── Apply axle differentials (Open/BallDiff/Spool)
  │   │   └── Sets motorForceShare on each wheel
  │   └── ApplySteering(dt, steer, speed)
  │       └── Speed-dependent steering reduction + reverse flip
  └── For each RaycastWheel:
      └── wheel.ApplyWheelPhysics(rb, dt)
          ├── Raycast downward for ground contact
          ├── ComputeSuspension()     ← Hooke's law: F = k*x + c*v
          ├── ComputeLateralForce()   ← Grip curve × slip ratio × load
          ├── ComputeLongitudinalForce() ← Forward friction + ramp hold
          ├── ComputeMotorForce()     ← motorForceShare along wheel forward
          ├── AddForceAtPosition(total, contactPoint)  ← TO RIGIDBODY
          └── Update visual (position, spin rotation, RPM)
```

### Camera Pipeline (LateUpdate)

```
CameraController.Update()
  ├── Check mode cycle key (C) → CycleMode()
  └── If Orbit: read mouse/stick input → update yaw/pitch

CameraController.LateUpdate()
  ├── If transitioning: smooth interpolate position/rotation between modes
  └── Apply current mode:
      ├── Chase: Lerp behind car, LookAt car
      ├── Orbit: Spherical offset from yaw/pitch, LookAt car
      ├── FPV: Lock to car body with local offset
      └── Trackside: Static position (nearest anchor), LookAt car
```

### Telemetry Pipeline (OnGUI)

```
TelemetryHUD.OnGUI()
  ├── Read car state (speed, forces, airborne, tumble, steering)
  ├── Read per-wheel state (ground, spring, slip, grip, RPM)
  └── Render text overlay
```

---

## Dependency Graph

```
RCCar (orchestrator)
  ├── requires → Rigidbody (physics body)
  ├── requires → RCInput (input source)
  ├── requires → RaycastWheel[] (discovered in children)
  ├── optional → Drivetrain (force distribution)
  └── optional → RCAirPhysics (air control)

RaycastWheel (per-wheel physics)
  ├── receives → Rigidbody reference from RCCar
  ├── receives → springStrength, gripCoeff from RCCar
  └── reads → RCCar.currentEngineForce for static friction

Drivetrain (differential logic)
  ├── receives → RaycastWheel[] from RCCar
  └── writes → motorForceShare on each wheel

RCAirPhysics (air torques)
  ├── requires → Rigidbody (parent)
  └── reads → RaycastWheel[].wheelRpm for gyro

CameraController (camera)
  ├── requires → Transform target (set in inspector)
  └── optional → TracksideAnchor[] (discovered at runtime for Trackside mode)

TelemetryHUD (debug UI)
  └── requires → RCCar reference (set in inspector)

RCInput (input)
  └── standalone — no dependencies (reads Unity Input Manager)
```

---

## Architecture Decision Records

### ADR-001: Raycast-Based Wheel Physics

**Status:** Accepted
**Decision:** Use raycast-based wheel physics with Hooke's law suspension and curve-sampled grip model
**Details:** See `adr-001-physics-model.md`