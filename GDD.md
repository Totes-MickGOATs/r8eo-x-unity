# R8EO-X — Game Design Document

**Version:** 0.1 (Pre-production)
**Engine:** Unity 6 (URP)
**Platform:** PC (Steam / Desktop)
**Mode:** Single-player first

---

## 1. Concept

A realistic RC buggy racing simulation set on clay/dirt tracks. The player controls
1/10th or 1/8th scale electric buggy cars from a chase-camera perspective. The game
captures the unique feel of RC racing: twitchy throttle, snap oversteer on dirt, and
precise air control using inertia and gyroscopic effects from spinning wheels.

**Core fantasy:** _You are a skilled RC racer who can feel every bump, every slip, and
stick the perfect landing from a triple jump._

---

## 2. Core Pillars

| Pillar | Description |
|--------|-------------|
| **Authentic Feel** | Physics tuned to real RC buggy behavior, not arcade approximation |
| **Track Mastery** | Tracks reward learning optimal lines, jump approach angles, berm use |
| **Air Craft** | Air control via inertia/gyro is a skill ceiling, not a gimmick |
| **Accessible Depth** | Easy to pick up (chase cam, forgiving defaults); hard to master |

---

## 3. Player Experience Goals

- First 5 minutes: driving feels immediately responsive and fun even before mastery
- First hour: player discovers that throttle timing on jumps changes landing angle
- Long-term: mastering optimal lines and air correction shaves seconds per lap

---

## 4. Car Physics Design

### 4.1 Buggy Characteristics

| Property | 1/10th Scale | 1/8th Scale |
|----------|-------------|-------------|
| Mass (sim units) | ~1.5 kg | ~3.5 kg |
| Wheelbase | Short (twitchy) | Medium |
| Power-to-weight | Very high | High |
| Top speed | ~70 km/h real | ~90 km/h real |
| Suspension travel | Long, soft | Long, moderate |
| Default grip | Low-medium | Medium |

### 4.2 Ground Physics

**Surface types and friction modifiers:**

| Surface | Friction | Notes |
|---------|----------|-------|
| Packed clay (dry) | 0.85 | Main racing surface; best grip |
| Loose dirt | 0.55 | Off-line, slippery, hard to recover |
| Wet clay | 0.65 | Reduced grip, more rotation |
| Rut / groove | 0.90 | Ideal racing line reward |
| Curb/edge | 0.40 | Destabilizing if hit wrong |

**Key behaviors:**
- Wheelspin on throttle application (especially out of corners)
- Snap oversteer if rear grip breaks suddenly (not gradual)
- Weight transfer visible in suspension animation and car body lean
- Differential behavior: open rear diff = inside wheel spins, car rotates

### 4.3 Air Physics (Key Differentiator)

When all four wheels leave the ground:

1. **Throttle -> Pitch control**
   - Blipping throttle UP: wheel spin inertia rotates nose DOWN (dive)
   - Lifting off throttle: wheel deceleration rotates nose UP (lift nose)
   - Used to correct flat landings on jumps

2. **Steering -> Roll control**
   - Steering input while airborne applies roll torque
   - Allows car to correct body lean mid-air
   - Subtle effect — requires finesse

3. **Gyroscopic stabilization**
   - Spinning wheels resist sudden changes in orientation
   - Higher wheel speed = more gyro stability in air

4. **Landing**
   - Landing nose-first: compression -> possible bounce/flip
   - Landing flat: safe but may bottom out
   - Landing rear-first: wheelie, then nose slap -> loss of control
   - Ideal: slight nose-up or flat

---

## 5. Track Design

### 5.1 Track Anatomy

```
[Starting Grid]
  -> [Technical Low-Speed Section] (tight turns, rhythm bumps)
  -> [Jump Section] (single, double, or triple jumps)
  -> [High-Speed Sweeper] (berm-assisted corner)
  -> [Rhythm Lane] (series of bumps for jumping vs. rolling choice)
  -> [Jump-Into-Corner] (commitment point)
  -> [Finish Line]
```

### 5.2 Track Features

| Feature | Purpose |
|---------|---------|
| **Berms** | Banked corners — faster with proper entry angle |
| **Rhythm Section** | Alternating bumps — doubles vs singles is a skill choice |
| **Table Top** | Flat-top jump — safe landing, but single vs table choice |
| **On/Off Camber** | Tests weight transfer and line selection |
| **Ruts** | Develops along optimal line — rewards learning the groove |

### 5.3 Racing Line System

The "groove" — the optimal, highest-grip path — is worn into clay tracks:
- Implemented as a spline-based path through the track
- Driving on the groove gives a grip bonus
- Visual indicator (darker, packed dirt texture) shows over time
- AI uses this path as its navigation line

---

## 6. Camera System

### 6.1 Chase Camera (Default)

- Cinemachine Virtual Camera — detached from car's rotation hierarchy, eliminating jitter on fast slides
- Follows car position with smooth damped follow over world-space yaw (smooth 3rd-person follow)
- Camera positioned behind and above the car via Cinemachine Transposer/Orbital
- Field of View: base + speed-scaled boost (`car.Rigidbody.linearVelocity.magnitude`)
- `camera_reset` action (C / right-stick click) snaps yaw back behind car

### 6.2 Camera Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| Arm length | 2.5m | Distance behind car |
| Height offset | 0.8m | Above car pivot |
| Rotation lag | 0.15s | Smooth follow |
| Position lag | 0.08s | Tight, responsive |
| FOV speed boost | +15 deg max | At top speed |

### 6.3 Future Camera Modes

- **Cockpit** (driver's eye view, very low to ground)
- **TV cam** (fixed broadcast-style cameras, auto-switches)
- **Spectator** (free-roam for replay)

---

## 7. Input Design

All input abstracted to actions via the Unity Input System:

| Action | KB Default | Controller |
|--------|-----------|------------|
| `throttle` | W / Up Arrow | Right trigger |
| `brake_reverse` | S / Down Arrow | Left trigger |
| `steer_left` | A / Left Arrow | Left stick X |
| `steer_right` | D / Right Arrow | Left stick X |
| `air_pitch_up` | (throttle) | (throttle) |
| `air_pitch_down` | (brake) | (brake) |
| `camera_reset` | C | Right stick click |
| `pause` | Escape | Start |

**Note:** Air pitch reuses throttle/brake — same input, different physics effect when airborne.

---

## 8. Game Modes (Phase 1)

| Mode | Description |
|------|-------------|
| **Time Trial** | Race alone, beat your best lap time |
| **Practice** | Free roam the track with no pressure |
| **Race (vs AI)** | Race a field of AI drivers |

### 8.1 Progression (Future)

- Unlock tracks by completing races
- Upgrade car parts (motor, suspension, tires, differential)
- 1/10th class -> 1/8th class progression

---

## 9. AI Design

### 9.1 AI Approach: Spline-Following with Perturbation

Phase 1 AI follows the optimal racing line (spline) with:
- Speed lookup table per section (how fast to take each corner/jump)
- Throttle control for landing corrections
- Overtaking: slight deviation from main spline around slower cars

### 9.2 AI Difficulty Levels

| Difficulty | Line Adherence | Speed Factor | Mistakes |
|-----------|---------------|-------------|---------|
| Beginner | 70% | 0.75x | Frequent |
| Intermediate | 90% | 0.90x | Occasional |
| Expert | 97% | 1.00x | Rare |
| Champion | 99% | 1.05x | Almost never |

---

## 10. Audio Design (Future)

| Sound | Description |
|-------|-------------|
| Motor | Electric whine pitch-shifted to RPM |
| Tire slip | Dirt spray sound when breaking traction |
| Suspension | Soft thud on landing, spring creak |
| Air | Wind rush that increases with speed |
| Collision | Plastic-on-dirt tumble sounds |

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

Manager Systems (MonoBehaviour singletons or ScriptableObject services):
+-- Debug           -- tagged logging, per-tag filtering, file logging
+-- SceneManager    -- scene transitions, menu flow, toast notifications
+-- GameManager     -- game state, pause, menu_active flag
+-- RaceManager     -- lap timing, checkpoints, race positions, best lap persistence
+-- InputManager    -- abstract input layer (keyboard + controller + profiles via Input System)
+-- SettingsManager -- unified PlayerPrefs/JSON persistence
+-- GraphicsManager -- tier switching, quality settings, config persistence
+-- AudioManager    -- AudioMixer hierarchy (Master>Music/SFX>Engine/Tires/Impact/Ambient/UI)
+-- SteamManager    -- Steamworks.NET stub (achievements, leaderboards)
+-- NetworkManager  -- Netcode for GameObjects, LAN auto-discovery
+-- WeatherManager  -- weather state management
```

### 11.2 Vehicle Setup

Vehicle physics built on custom Rigidbody + raycast wheel system (ported from GEVP concepts).

```
Vehicle (Rigidbody)  [VehicleController.cs]
  motor, ESC, drivetrain, steering, stability systems
+-- CollisionShape (BoxCollider)
+-- Body (MeshRenderer -- imported .fbx/.glb model)
+-- WheelFrontLeft  (Raycast wheel -- RCWheel.cs)
|   +-- FrontLeftWheel (Transform)
|       +-- WheelMesh (MeshRenderer)
+-- WheelFrontRight (Raycast wheel -- RCWheel.cs)
+-- WheelRearLeft   (Raycast wheel -- RCWheel.cs)
+-- WheelRearRight  (Raycast wheel -- RCWheel.cs)
+-- AirPhysics      (MonoBehaviour -- AirPhysics.cs)
+-- WeightTransfer  (MonoBehaviour -- WeightTransfer.cs)
+-- SuspensionPreload (MonoBehaviour -- SuspensionPreload.cs)

Camera: Cinemachine Virtual Camera
  Chase camera with view presets, drift look-ahead, inspection mode.
  Separate GameObject, not a child of Vehicle.
```

### 11.3 Key Physics Parameters (Vehicle + Wheel)

```csharp
// Vehicle (Rigidbody) -- example: RC buggy
[SerializeField] private float vehicleMass = 1.5f;           // kg (1/10th scale)
[SerializeField] private float maxTorque = 0.8f;             // Nm peak motor torque
[SerializeField] private float maxRpm = 40000f;              // brushless motor rev limit
[SerializeField] private float gearRatio = 8.0f;             // single-speed transmission
[SerializeField] private float frontTorqueSplit = 0.0f;      // 0.0=RWD, 0.5=AWD, 1.0=FWD
[SerializeField] private float maxSteeringAngle = 30f;       // degrees
[SerializeField] private float steeringSlipAssist = 0.3f;    // countersteer help
[SerializeField] private float centerOfGravityOffset = -0.01f; // meters

// Per Wheel (Raycast-based) -- custom tire model
[SerializeField] private float tireRadius = 0.045f;          // meters
[SerializeField] private float tireWidth = 0.038f;           // meters
[SerializeField] private float springRestRatio = 0.6f;       // spring rest point (fraction of travel)
[SerializeField] private float dampingRatio = 0.5f;          // damping coefficient
[SerializeField] private float antiRollBarRatio = 0.3f;      // anti-roll bar stiffness

// Surface detection: PhysicMaterial or tag-based (wheel raycast returns surface info)
// Collision layers: Terrain, Vehicles, Obstacles (configured in Physics Settings)
```

### 11.4 Air Physics System

Custom system layered on top of Vehicle:

```
AirPhysics (MonoBehaviour, child of Vehicle)  [Scripts/Vehicle/AirPhysics.cs]
  [SerializeField] fields: pitchTorque, rollTorque, gyroStrength, gyroFullRpm
  FixedUpdate():
    +-- pitch torque  -- throttle reaction: nose up on throttle, down on brake
    +-- roll torque   -- steering-induced roll correction
    +-- gyro damp     -- angular velocity damp scaled by avg wheel RPM

Airborne detection: Vehicle.GetWheelContactCount() == 0
  -> checks all wheel raycasts for ground contact
```

---

## 12. Development Phases

### Phase 1 — Prototype (Unity Port)
- [x] Single car on a flat test track (Unity Terrain)
- [x] Vehicle physics (custom Rigidbody + raycast wheels)
- [x] Chase camera (Cinemachine)
- [x] Basic input (throttle, brake, steer, flip) — keyboard + controller via Input System
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
- [ ] Multiple tracks
- [ ] Car selection (1/10th vs 1/8th)
- [ ] Audio
- [ ] Particles (dust, dirt spray)
- [ ] UI polish

### Phase 5 — Multiplayer (Future)
- [ ] Architecture review for network sync
- [ ] Online race lobbies

---

## 13. Open Questions / Notes

- [ ] Should the "groove" develop dynamically during a race, or be baked into the track?
- [ ] Damage model? (Cars can flip — reset or physics-based self-righting?)
- [ ] Car setup screen? (Adjust suspension, diff, tire compound before race)
- [ ] Is 1/8th buggy a separate class or just a tuning preset?
- [ ] Real track inspirations to model? (ROAR Nationals layouts, local club tracks?)
