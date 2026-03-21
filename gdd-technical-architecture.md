# GDD — Technical Architecture & Development Phases

Part of the [Game Design Document](./GDD.md).

---

## 11. Technical Architecture (Unity 6)

### 11.1 Scene Hierarchy

```
Main (Scene: GameplayScene.unity)
+-- TrackContainer (GameObject)
|   +-- Terrain (Unity Terrain with MicroSplat)
|
+-- Vehicle (Rigidbody, custom physics)  -- see 11.2
|
+-- UI
    +-- TuningPanel (Canvas)   [Scripts/UI/TuningPanel.cs]
    +-- RaceHud (Canvas)       [Scripts/UI/RaceHud.cs]

Manager Systems:
+-- Debug, SceneManager, GameManager, RaceManager, InputManager
+-- SettingsManager, GraphicsManager, AudioManager
+-- SteamManager, NetworkManager, WeatherManager
```

### 11.2 Vehicle Setup

```
Vehicle (Rigidbody)  [VehicleController.cs]
  motor, ESC, drivetrain, steering, stability systems
+-- CollisionShape (BoxCollider)
+-- Body (MeshRenderer)
+-- WheelFrontLeft  (Raycast wheel -- RCWheel.cs)
+-- WheelFrontRight (Raycast wheel -- RCWheel.cs)
+-- WheelRearLeft   (Raycast wheel -- RCWheel.cs)
+-- WheelRearRight  (Raycast wheel -- RCWheel.cs)
+-- AirPhysics      (MonoBehaviour -- AirPhysics.cs)
+-- WeightTransfer  (MonoBehaviour -- WeightTransfer.cs)

Camera: Cinemachine Virtual Camera (separate GameObject, not child of Vehicle)
```

### 11.3 Key Physics Parameters

```csharp
[SerializeField] private float vehicleMass = 1.5f;           // kg (1/10th scale)
[SerializeField] private float maxTorque = 0.8f;             // Nm peak motor torque
[SerializeField] private float maxRpm = 40000f;              // brushless motor rev limit
[SerializeField] private float gearRatio = 8.0f;             // single-speed transmission
[SerializeField] private float frontTorqueSplit = 0.0f;      // 0.0=RWD, 0.5=AWD
[SerializeField] private float maxSteeringAngle = 30f;       // degrees
[SerializeField] private float tireRadius = 0.045f;          // meters
[SerializeField] private float springRestRatio = 0.6f;       // spring rest point
[SerializeField] private float dampingRatio = 0.5f;          // damping coefficient
```

### 11.4 Air Physics System

```
AirPhysics (MonoBehaviour, child of Vehicle)  [Scripts/Vehicle/AirPhysics.cs]
  FixedUpdate():
    +-- pitch torque  -- throttle reaction: nose up on throttle, down on brake
    +-- roll torque   -- steering-induced roll correction
    +-- gyro damp     -- angular velocity damp scaled by avg wheel RPM

Airborne detection: Vehicle.GetWheelContactCount() == 0
```

---

## 12. Development Phases

### Phase 1 — Prototype (Unity Port)
- [x] Single car on a flat test track (Unity Terrain)
- [x] Vehicle physics (custom Rigidbody + raycast wheels)
- [x] Chase camera (Cinemachine)
- [x] Basic input (throttle, brake, steer, flip) — keyboard + controller
- [x] Air physics (pitch/roll/gyro)
- [x] Runtime tuning panel (live physics sliders + telemetry)
- [ ] Terrain track with basic sculpting

### Phase 2 — First Track
- [ ] Build Track 1 with jumps, berms, rhythm section
- [ ] Surface physics (grip zones)
- [ ] Lap timing + checkpoint system
- [ ] Time Trial mode

### Phase 3 — Race Mode
- [ ] AI drivers (spline following)
- [ ] Race start sequence
- [ ] Position tracking

### Phase 4 — Content + Polish
- [ ] Multiple tracks, car selection (1/10th vs 1/8th)
- [ ] Audio, Particles (dust, dirt spray)
- [ ] UI polish

### Phase 5 — Multiplayer (Future)
- [ ] Architecture review for network sync
- [ ] Online race lobbies

---

## 13. Open Questions

- [ ] Should the "groove" develop dynamically during a race, or be baked into the track?
- [ ] Damage model? (Cars can flip — reset or physics-based self-righting?)
- [ ] Car setup screen? (Adjust suspension, diff, tire compound before race)
- [ ] Is 1/8th buggy a separate class or just a tuning preset?
- [ ] Real track inspirations to model? (ROAR Nationals layouts, local club tracks?)
