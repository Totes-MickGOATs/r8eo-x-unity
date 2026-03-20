---
name: unity-genre-racing
description: Unity Genre: Racing
---

# Unity Genre: Racing

Use this skill when building racing game systems — vehicle physics architecture, track design, race management, AI opponents, multiplayer, and input handling. Focuses on RC racing but patterns apply to any vehicle-based game.

---

## Vehicle Physics Architecture

### Suspension Model: Raycast vs WheelCollider

| Criteria | Raycast Suspension | WheelCollider |
|----------|-------------------|---------------|
| Friction control | Full custom model (Pacejka, brush, etc.) | Built-in only, limited tuning |
| RC scale (1/10) | Works at any scale | Struggles below ~1m wheelbase |
| Contact modification | Compatible with `Physics.ContactModifyEvent` | No access to contact points |
| Performance | Cheaper per wheel (single raycast) | Heavier (internal solver iterations) |
| Setup complexity | More code, full understanding required | Drop-in, less boilerplate |
| Arcade suitability | Overkill | Acceptable, fast iteration |
| Simulation suitability | Preferred | Inadequate for serious sim |
| Anti-roll bars | Manual implementation via opposing spring forces | Manual implementation required |

**Recommendation:** Use raycast suspension for any simulation or semi-sim racing game. Use WheelCollider only for arcade prototypes where physics fidelity is not a selling point.

### Raycast Suspension Implementation

Each wheel casts a ray downward from the suspension mount point. The hit distance determines spring compression, which drives the force applied to the Rigidbody.

```
suspensionForce = (restLength - currentLength) * springRate + compressionVelocity * damperRate
```

Key parameters per wheel:
- `springRate` — N/m, stiffness of the spring (RC typical: 20-80 N/m per wheel)
- `damperRate` — Ns/m, damping coefficient (RC typical: 2-10 Ns/m)
- `restLength` — meters, natural spring length at rest (RC typical: 0.02-0.04m)
- `maxTravel` — meters, maximum compression distance
- `wheelRadius` — meters (RC typical: 0.025-0.035m)

Apply forces at the wheel contact point using `Rigidbody.AddForceAtPosition()` — never at the center of mass, or the vehicle will not roll/pitch correctly.

### Pacejka Magic Formula (Tire Friction)

The Pacejka Magic Formula models tire grip as a function of slip:

```
F = D * sin(C * arctan(B * x - E * (B * x - arctan(B * x))))
```

Where:
- `F` — normalized force output (multiply by normal load for actual force)
- `x` — slip ratio (longitudinal) or slip angle in radians (lateral)
- `B` — stiffness factor (controls slope at origin, how quickly grip builds)
- `C` — shape factor (controls curve shape; ~1.3 for lateral, ~1.65 for longitudinal)
- `D` — peak factor (maximum grip coefficient, typically 0.8-1.2 on asphalt)
- `E` — curvature factor (controls drop-off past peak; negative = no drop-off)

Slip ratio (longitudinal): `slipRatio = (wheelAngularVelocity * wheelRadius - vehicleSpeed) / max(vehicleSpeed, epsilon)`

Slip angle (lateral): `slipAngle = arctan(lateralVelocity / max(longitudinalVelocity, epsilon))`

**Combined slip:** When both longitudinal and lateral slip are present, use the friction ellipse model — compute a combined slip magnitude and distribute the available grip proportionally between longitudinal and lateral forces.

### Rigidbody Configuration for Vehicles

```csharp
Rigidbody rb = GetComponent<Rigidbody>();
rb.mass = 2.0f;                              // kg (RC 1/10 scale)
rb.linearDamping = 0.05f;                    // minimal, let tire model handle drag
rb.angularDamping = 0.5f;                    // prevents spin oscillation
rb.interpolation = RigidbodyInterpolation.Interpolate;
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
rb.centerOfMass = new Vector3(0f, -0.01f, 0.01f); // low and slightly forward
rb.maxAngularVelocity = 50f;                 // default 7 is too low for RC
```

**Critical:** Set center of mass explicitly. Unity computes it from collider geometry, which is almost never correct for a vehicle. A CoM that is too high causes rollovers; too far back causes oversteer.

### WheelCollider Substep Configuration

If using WheelColliders, call `ConfigureVehicleSubsteps` to prevent jitter at low speeds:

```csharp
WheelCollider wc = GetComponent<WheelCollider>();
wc.ConfigureVehicleSubsteps(5f, 12, 15);
// speedThreshold: below 5 m/s, use 12 substeps; above, use 15
```

This is mandatory for any WheelCollider-based vehicle — the default substep count causes visible instability.

---

## Track Design

### Approaches: Spline vs Terrain vs Hybrid

| Approach | Best For | Drawbacks |
|----------|----------|-----------|
| **Spline-based** (Unity Splines package) | Smooth road surfaces, banked turns, procedural tracks | Harder to add off-road terrain variety |
| **Terrain-based** (Unity Terrain) | Off-road, rally, large open environments | Limited surface detail, LOD pop-in at edges |
| **Hybrid** (spline road on terrain) | Most racing games — road + surroundings | More complex asset pipeline |

For RC racing, the hybrid approach is typical: a detailed track surface (mesh or spline-extruded) sitting on a terrain for the surrounding environment.

### Unity Splines Package

```csharp
using Unity.Mathematics;
using UnityEngine.Splines;

// Sample a point along the spline
SplineContainer spline = GetComponent<SplineContainer>();
float t = 0.5f; // 0-1 normalized position
float3 position = spline.EvaluatePosition(t);
float3 tangent = spline.EvaluateTangent(t);
float3 up = spline.EvaluateUpVector(t);
```

Use `SplineExtrude` to generate track mesh geometry from a cross-section profile. Bake the mesh collider after extrusion for physics.

### Checkpoint System Architecture

Checkpoints are the backbone of race management. Every racing game needs them regardless of visual style.

```
Checkpoint[] orderedCheckpoints;  // placed along track in order
int lastCheckpointIndex;          // per-vehicle, last checkpoint passed
int lapCount;                     // per-vehicle
float raceDistance;               // per-vehicle, for position sorting
```

**Implementation:**
1. Place trigger colliders (BoxCollider with `isTrigger = true`) at regular intervals around the track
2. Number them sequentially (0, 1, 2, ... N-1) where checkpoint 0 is the start/finish line
3. On trigger enter, validate that the vehicle passed the previous checkpoint (prevents shortcuts)
4. Update `lastCheckpointIndex` and compute `raceDistance`

**Shortcut prevention:** Only accept a checkpoint if `checkpointIndex == (lastCheckpointIndex + 1) % totalCheckpoints`. If a vehicle skips checkpoints, do not update their position — they must return and pass checkpoints in order.

**Position sorting formula:**
```
raceDistance = (lapCount * numCheckpoints) + lastCheckpointIndex + fractionToNextCheckpoint
```

Where `fractionToNextCheckpoint` is the normalized distance between the last checkpoint and the next, computed by projecting the vehicle position onto the line segment between the two checkpoints. This gives sub-checkpoint-granularity position sorting.

### Racing Line Theory

Three fundamental corner types affect track design and AI pathing:

- **Late apex:** Turn-in point is deep, apex is past the geometric center. Favors exit speed. Most common in racing.
- **Early apex:** Apex before center. Used when the following straight is short or the next corner is close.
- **Decreasing radius:** Corner tightens. Requires progressive braking through the turn. The most difficult corner type.

**Track width:** RC 1/10 scale tracks are typically 2-4 meters wide (real-world). In-game, scale accordingly — a 1/10 RC car is ~0.05m wide, so track width is ~0.3-0.5m at model scale or 3-5m if using 1:1 Unity units with scaled physics.

### RC Track Dimensions (1/10 Scale, Real-World Reference)

| Parameter | Typical Value |
|-----------|--------------|
| Track width | 2.5 - 4.0 m |
| Straight length | 10 - 30 m |
| Minimum corner radius | 1.5 - 3.0 m |
| Jump face angle | 15 - 35 degrees |
| Jump landing zone | 3 - 6 m from lip |
| Lap length | 150 - 400 m |
| Surface | Clay, carpet, astroturf, or asphalt |

---

## Race Management

### Position Tracking

Use `raceDistance` for real-time position sorting across all vehicles:

```csharp
float raceDistance = (lapCount * checkpoints.Length) + lastCheckpointIndex + fractionToNext;
```

Sort all vehicles by `raceDistance` descending to get positions. Update every frame for smooth position display.

### Lap Counting with Checkpoint Validation

```csharp
void OnCheckpointPassed(int checkpointIndex)
{
    int expected = (lastCheckpointIndex + 1) % checkpoints.Length;
    if (checkpointIndex != expected) return; // wrong order, ignore

    lastCheckpointIndex = checkpointIndex;
    checkpointsPassed++;

    if (checkpointIndex == 0 && checkpointsPassed > checkpoints.Length)
    {
        lapCount++;
        OnLapCompleted();
    }
}
```

**Edge case:** The first lap starts when the race begins, not when the vehicle first crosses checkpoint 0. Initialize `lastCheckpointIndex = 0` and `checkpointsPassed = 0` at race start.

### Split Times

Sector timing provides granular feedback to the driver:

1. Divide the track into 3-5 sectors using additional trigger colliders
2. Record the time when entering each sector trigger
3. Compute sector time as `currentSectorEntry - previousSectorEntry`
4. Compare against the best sector time for the current session

**Display convention:**
- Green / negative delta: faster than best
- Red / positive delta: slower than best
- Purple: all-time best (across sessions)

### Race Start Sequence

```
State: PRE_RACE    → Vehicles positioned on grid, motors locked
State: COUNTDOWN   → 3... 2... 1... GO! (or traffic light sequence)
State: RACING      → Motors unlocked, timer running
State: FINISHED    → Winner crossed line, others finishing
State: RESULTS     → Final standings displayed
```

**Motor authority:** During COUNTDOWN, set a flag that prevents throttle from reaching the motor. On GO, release the flag. This is cleaner than zeroing input — it preserves the player's input state so they can pre-load throttle.

**Jump-start detection:** If the vehicle moves more than a threshold distance (e.g., 0.1m) before GO, apply a time penalty (typically 1-5 seconds added to final time, or a stop-go penalty at race start).

---

## AI Racing

### Waypoint-Based Path Following

Use Craig Reynolds' steering behaviors adapted for racing:

1. **Seek:** Steer toward the next waypoint
2. **Arrive:** Slow down as approaching a waypoint (for braking zones)
3. **Path following:** Project position onto the racing line spline, steer toward a look-ahead point

```csharp
float3 lookAheadPoint = racingLine.EvaluatePosition(currentT + lookAheadDistance);
float3 toTarget = lookAheadPoint - vehiclePosition;
float steerAngle = Vector3.SignedAngle(vehicleForward, toTarget, Vector3.up);
float normalizedSteer = Mathf.Clamp(steerAngle / maxSteerAngle, -1f, 1f);
```

### Racing Line Spline with Per-Waypoint Target Speed

Define a `RacingLinePoint` struct:

```csharp
struct RacingLinePoint
{
    float3 position;
    float targetSpeed;      // m/s, computed from corner radius
    float brakeDistance;     // meters before this point to start braking
    bool isBrakingZone;     // true if AI should decelerate here
}
```

**Target speed from corner radius:** `targetSpeed = sqrt(gripCoefficient * gravity * cornerRadius)`

Pre-compute target speeds for the entire racing line at startup. The AI controller reads ahead to find the next braking zone and begins decelerating when within `brakeDistance`.

### Difficulty Scaling

Scale AI difficulty through these independent parameters:

| Parameter | Easy | Medium | Hard |
|-----------|------|--------|------|
| Reaction time delay | 200-400ms | 100-200ms | 0-50ms |
| Speed limit (% of optimal) | 70-80% | 85-95% | 98-100% |
| Trajectory error (random offset) | 0.3-0.5m | 0.1-0.2m | 0-0.05m |
| Braking point error | 2-4m early | 0.5-1m early | 0m (optimal) |
| Recovery skill (off-track) | Slow, wide return | Moderate | Fast, tight return |

Apply these as multipliers and offsets to the base AI controller. This gives natural-feeling difficulty variation without requiring separate AI implementations.

### Rubber Banding

Rubber banding adjusts AI speed based on the gap to the player. Use sparingly — heavy rubber banding feels unfair.

```csharp
float gapSeconds = (playerRaceDistance - aiRaceDistance) / aiSpeed;
float gapFactor = Mathf.Clamp(gapSeconds / maxGapSeconds, -1f, 1f);
float speedMultiplier = 1f + (gapFactor * rubberBandStrength);
// rubberBandStrength = 0.1 means +/- 10% max speed adjustment
```

**Guidelines:**
- Cap adjustment at +/-10% — beyond this, players notice and feel cheated
- Only apply rubber banding to AI behind the player (catch-up), not AI ahead (slow-down), unless the gap is extreme
- Disable rubber banding on the final lap or final sector for dramatic finishes
- Never apply rubber banding in time trial or practice modes

### Ghost Car System

Record vehicle state at fixed intervals (e.g., every FixedUpdate) during a lap:

```csharp
struct GhostFrame
{
    float timestamp;
    Vector3 position;
    Quaternion rotation;
    // Optional: wheel rotations, suspension compression for visual fidelity
}
```

Playback: interpolate between the two nearest frames based on current lap time. Use a transparent or holographic material on the ghost vehicle. Store ghost data as a binary file for persistence across sessions.

---

## Camera Systems

### Chase Camera (Cinemachine)

Use `ThirdPersonFollow` as the body component:

> **Unity 6 / Cinemachine 3.x:** `CinemachineVirtualCamera` is now `CinemachineCamera`, `CinemachineThirdPersonFollow` is now `ThirdPersonFollow`, `CinemachineBlendListCamera` is now `CinemachineSequencerCamera`, and the namespace changed from `using Cinemachine` to `using Unity.Cinemachine`. Cinemachine 2.x reaches end of support in Unity 6.1.

```
CinemachineCamera:
  Body: ThirdPersonFollow
    ShoulderOffset: (0, 0.15, 0)    // RC scale: slightly above
    CameraDistance: 0.8              // RC scale: close follow
    Damping: (0.5, 0.3, 0.5)        // smooth but responsive
  Aim: Composer
    TrackedObjectOffset: (0, 0, 0.3) // look ahead of vehicle
    Damping: (2, 2)
```

### Speed-Based FOV

Increase field of view with speed to create a sense of velocity:

```csharp
float speedNormalized = currentSpeed / maxSpeed;
float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedNormalized);
// baseFOV = 60, maxFOV = 80-90
cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(
    cinemachineCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);
```

Smooth the transition with Lerp — instant FOV changes are nauseating.

### Camera Shake on Collision

Use `CinemachineImpulseSource` for physics-driven camera shake:

```csharp
[SerializeField] CinemachineImpulseSource impulseSource;

void OnCollisionEnter(Collision collision)
{
    float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;
    float normalizedForce = Mathf.Clamp01(impactForce / maxImpactForce);
    impulseSource.GenerateImpulse(normalizedForce);
}
```

Add a `CinemachineImpulseListener` extension to the camera to receive the impulse.

### Bumper / Hood Camera

Parent the camera directly to the vehicle transform with no damping:

```csharp
// Position: front of vehicle, slightly above hood
// Rotation: locked to vehicle forward
// No Cinemachine needed — direct transform parenting
```

This gives the most immersive but also the most jarring view. Consider adding a very slight stabilization on the pitch axis to reduce nausea from suspension bounce.

### Replay Cameras

Pre-place `CinemachineCamera` components around the track at strategic positions (corners, jumps, straights). During replay:

1. Evaluate which camera has the best view of the vehicle (distance + angle scoring)
2. Set that camera's `Priority` highest
3. Cinemachine handles blending between cameras automatically

Use `CinemachineSequencerCamera` for scripted sequences (start, finish, podium).

---

## Input Handling

### Unity Input System Setup

Use the new Input System with action maps:

```
ActionMap: Vehicle
  Throttle    - Gamepad: RightTrigger, Keyboard: W/UpArrow
  Brake       - Gamepad: LeftTrigger, Keyboard: S/DownArrow
  Steer       - Gamepad: LeftStick X, Keyboard: A-D/LeftRight
  Handbrake   - Gamepad: ButtonSouth, Keyboard: Space
```

Apply processors in the Input Action asset:
- `StickDeadzone(min=0.125, max=0.925)` on gamepad stick axes
- `AxisDeadzone(min=0.05, max=0.95)` on trigger axes

### Steering Curve (Power Curve)

Raw stick input is too sensitive near center and not sensitive enough near extremes. Apply a power curve:

```csharp
float ProcessSteeringInput(float rawInput)
{
    float sign = Mathf.Sign(rawInput);
    float magnitude = Mathf.Abs(rawInput);
    float curved = Mathf.Pow(magnitude, 1.5f); // exponent > 1 = less sensitive near center
    return sign * curved;
}
```

**Exponent guide:**
- `1.0` — linear (no curve)
- `1.5` — moderate curve, good for most racing games
- `2.0` — aggressive curve, good for high-speed vehicles where precision matters
- `0.5-0.8` — inverse curve, more sensitive near center (unusual, for slow vehicles)

### Speed-Sensitive Steering Reduction

Reduce maximum steering angle at high speed to prevent snap oversteer:

```csharp
float speedFactor = currentSpeed / maxSpeed;
float steerLimit = Mathf.Lerp(1f, minSteerAtMaxSpeed, speedFactor);
// minSteerAtMaxSpeed = 0.3-0.5 (30-50% of max steering at top speed)
float finalSteer = processedInput * maxSteerAngle * steerLimit;
```

### Keyboard Interpolation (Binary to Analog)

Keyboard inputs are binary (0 or 1). Interpolate to simulate analog input:

```csharp
float targetSteer = keyboardSteerInput; // -1, 0, or 1
currentSteer = Mathf.MoveTowards(currentSteer, targetSteer, steerSpeed * Time.deltaTime);
// steerSpeed = 3-5 for responsive feel, 1-2 for heavy/realistic
```

Apply the same interpolation to throttle and brake for smooth acceleration curves on keyboard.

### Read in Update, Apply in FixedUpdate

```csharp
// Cache input every frame (Update runs at display refresh rate)
void Update()
{
    cachedThrottle = throttleAction.ReadValue<float>();
    cachedSteer = steerAction.ReadValue<float>();
    cachedBrake = brakeAction.ReadValue<float>();
}

// Apply forces at fixed physics rate
void FixedUpdate()
{
    ApplyMotorForce(cachedThrottle);
    ApplySteering(cachedSteer);
    ApplyBraking(cachedBrake);
}
```

**Why:** Input polling in FixedUpdate can miss button presses (FixedUpdate runs at 50Hz by default, Update at 60-240Hz). Always read input in Update, buffer it, and consume in FixedUpdate.

---

## Multiplayer

### Architecture: State Sync + Client Prediction

PhysX (Unity's physics engine) is non-deterministic across platforms and even across runs on the same machine. Deterministic lockstep is not viable for Unity racing games. Use state synchronization with client-side prediction instead.

**Authority model:**
- Each client is authoritative over their own vehicle's physics
- The server validates (anti-cheat) and relays state to other clients
- Remote vehicles are interpolated representations, not locally simulated

### NetworkTransform for Remote Vehicles

Use Unity Netcode's `NetworkTransform` or a custom sync component:

```csharp
struct VehicleNetState : INetworkSerializable
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public float steerAngle;
    public float timestamp;
}
```

**Sync only the vehicle body state** — do not sync per-wheel suspension compression, tire slip, or other derived values. Reconstruct visual wheel state on the receiving client from the body state.

### Snapshot Interpolation with Dead-Reckoning

Buffer the last 3-5 received states. Render remote vehicles at a time slightly in the past (interpolation delay, typically 50-100ms):

```csharp
// Interpolate between two buffered states
float renderTime = NetworkTime - interpolationDelay;
VehicleNetState a = FindStateBefore(renderTime);
VehicleNetState b = FindStateAfter(renderTime);
float t = (renderTime - a.timestamp) / (b.timestamp - a.timestamp);
transform.position = Vector3.Lerp(a.position, b.position, t);
transform.rotation = Quaternion.Slerp(a.rotation, b.rotation, t);
```

**Dead-reckoning fallback:** If no new state arrives (packet loss), extrapolate using the last known velocity:

```csharp
float timeSinceLastState = NetworkTime - lastState.timestamp;
Vector3 predicted = lastState.position + lastState.velocity * timeSinceLastState;
```

### Collision Handling

Remote vehicle collisions are the hardest part of networked racing:

- **Option A (simple):** Disable physics collisions between remote vehicles. Use overlap detection for visual/audio effects only. Acceptable for casual games.
- **Option B (authoritative):** Server resolves collisions and sends corrections. Higher latency, more accurate. Required for competitive games.
- **Option C (hybrid):** Local client predicts collision response, server corrects within a tolerance window. Best feel, most complex to implement.

---

## RC-Specific Considerations

### 1/10 Scale Physics

RC vehicles at 1/10 scale have fundamentally different physics characteristics than full-size cars:

| Property | Full-Size Car | 1/10 RC Car | Ratio |
|----------|--------------|-------------|-------|
| Mass | 1500 kg | 1.5-2.5 kg | 1/1000 |
| Length | 4.5 m | 0.45 m | 1/10 |
| Moment of inertia | ~2500 kg*m^2 | ~0.025 kg*m^2 | 1/100,000 |
| Suspension frequency | 1-2 Hz | 8-15 Hz | 5-10x |
| Corner speed | 30-60 m/s | 3-8 m/s | 1/10 |
| Aero downforce | Significant | Minimal (except 1/8 on-road) | N/A |

**Key implication:** RC cars respond to inputs and disturbances much faster than full-size cars. Suspension oscillates at higher frequencies. The simulation timestep must be small enough to capture this — use `Time.fixedDeltaTime = 0.005f` (200Hz) or smaller for RC physics.

### ESC (Electronic Speed Controller) Behavior

The ESC is the interface between the receiver and the motor. It has distinct behavior modes:

1. **Forward throttle curve:** Typically non-linear. Many ESCs have a "punch" setting that controls initial acceleration aggressiveness.
2. **Drag brake:** When throttle is released (neutral), the ESC applies a configurable braking force (0-100%). This simulates engine braking.
3. **Two-stage braking:**
   - From forward motion: first trigger pull to neutral = drag brake. Second pull past neutral = proportional braking.
   - From stopped: trigger past neutral = reverse (with optional delay for safety).
4. **Reverse lockout:** Some race ESCs disable reverse entirely. The trigger below neutral is full brake only.

```csharp
float ComputeMotorCommand(float throttleInput, float currentSpeed, ESCConfig config)
{
    if (throttleInput > config.deadzone)
    {
        // Forward: apply throttle curve
        return config.punchCurve.Evaluate(throttleInput);
    }
    else if (throttleInput < -config.deadzone)
    {
        if (currentSpeed > config.reverseThreshold)
        {
            // Moving forward + brake input = proportional brake
            return throttleInput * config.brakeStrength;
        }
        else
        {
            // Stopped or slow + brake input = reverse (if enabled)
            return config.reverseEnabled ? throttleInput * config.reverseStrength : 0f;
        }
    }
    else
    {
        // Neutral = drag brake
        return -Mathf.Sign(currentSpeed) * config.dragBrakeStrength;
    }
}
```

### Differential Types

The differential distributes motor torque between the left and right driven wheels:

| Type | Behavior | RC Use Case |
|------|----------|-------------|
| **Open (spool)** | Equal speed to both wheels (locked) | Oval racing, simple setups |
| **Ball differential** | Allows speed difference, adjustable via tension | General racing, most common |
| **Gear differential** | Smooth action, adjustable via silicone oil viscosity | High-grip surfaces, consistent feel |
| **One-way** | Locks on acceleration, freewheels on decel | Front axle of 4WD, reduces push on entry |

**Implementation:**
```csharp
// Simplified ball diff model
float diffRatio = 0.7f; // 0 = open, 1 = locked (spool)
float speedDiff = leftWheelSpeed - rightWheelSpeed;
float lockingTorque = speedDiff * diffRatio * diffStiffness;
leftWheelTorque = motorTorque * 0.5f - lockingTorque;
rightWheelTorque = motorTorque * 0.5f + lockingTorque;
```

### Suspension Geometry

RC suspension geometry affects handling at a fundamental level:

- **Camber:** Angle of wheel relative to vertical. Negative camber (top of wheel inward) improves cornering grip. RC typical: -1 to -3 degrees.
- **Toe:** Angle of wheel relative to centerline. Toe-in (fronts pointing inward) improves straight-line stability. Toe-out improves turn-in. RC typical: 0 to 2 degrees toe-in front, 1-3 degrees toe-in rear.
- **Caster:** Forward/backward tilt of the steering axis. More caster = more self-centering and camber gain in turns. RC typical: 15-30 degrees.
- **Kick-up:** Rear suspension arm angle. Affects anti-squat under acceleration.
- **Anti-squat:** Percentage of weight transfer resisted by suspension geometry. Higher = less squat under acceleration, more rear traction.

These can be implemented as static parameters that modify the tire contact patch normal and the effective camber angle used in the tire model.

### Tire Compounds

RC tires vary dramatically in grip characteristics:

| Compound | Grip Level | Wear Rate | Surface |
|----------|-----------|-----------|---------|
| **Foam (shore 25-42)** | Very high | High | Carpet, asphalt (on-road) |
| **Rubber (soft)** | High | Moderate | Clay, astroturf |
| **Rubber (medium)** | Medium | Low | Asphalt, hard-pack |
| **Rubber (hard)** | Low | Very low | Abrasive surfaces |
| **Pin/spike** | High (loose) | High | Dirt, loose surfaces |

In the tire model, compound affects:
- `D` (peak grip coefficient) in the Pacejka formula
- Thermal model: softer compounds heat up and degrade faster
- Surface interaction: grip multiplier varies per compound-surface combination

---

## Related Skills

| Skill | Relevance |
|-------|-----------|
| `unity-physics-3d` | Rigidbody forces, collision layers, PhysX configuration |
| `unity-input-system` | Input System setup, action maps, processors, device handling |
| `unity-camera-systems` | Cinemachine setup, virtual cameras, blending, impulse |
| `unity-networking` | Netcode, NetworkTransform, RPCs, state sync patterns |
| `unity-splines` | Spline creation, evaluation, extrusion for track geometry |
| `unity-ui-toolkit` | HUD elements: speedometer, lap counter, position display |
| `unity-audio` | Engine sound (RPM-based pitch), tire squeal, collision SFX |
| `reverse-engineering` | Analyzing reference games for physics and handling feel |
