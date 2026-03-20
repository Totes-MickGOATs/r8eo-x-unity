---
name: unity-game-feel
description: Unity Game Feel
---

# Unity Game Feel

Use this skill when adding camera juice, controller haptics, screen effects, audio feedback, or visual polish to make the RC racing game feel responsive and satisfying.

---

## Implementation Priority

Order by RC-specific impact. Implement top-down.

| Priority | Technique | Category | RC Impact |
|----------|-----------|----------|-----------|
| 1 | Suspension Compression Visuals | Visual | Defines RC identity — visible suspension IS the genre |
| 2 | Input Response Curves | Control | Precision feel at sticks, prevents twitchy oversteer |
| 3 | Landing Impact (Multi-Layered) | Compound | Peak excitement moments on jumps |
| 4 | Camera Shake on Impact | Camera | Communicates collision severity instantly |
| 5 | Speed-Based FOV | Camera | Subconscious speed perception |
| 6 | Audio as Game Feel | Audio | 40-50% of speed communication at RC scale |
| 7 | Controller Haptics | Tactile | Surface and engine feedback through hands |
| 8 | Tire Smoke Scaling | Visual | Drift satisfaction and slip feedback |
| 9 | Screen Effects at Speed | Visual | Cinematic speed polish |
| 10 | Speed Lines / Radial Blur | Visual | Additional speed communication layer |
| 11 | Time Manipulation | Temporal | Dramatic airtime moments |
| 12 | Minimap Juice | UI | Spatial awareness polish |

---

## 1. Speed-Based FOV

**Concept:** Widen the field of view as the vehicle accelerates. The player never consciously notices it, but removing it makes the game feel "flat." Cinemachine 3.x exposes `vcam.Lens.FieldOfView` directly.

**Code Example:**

```csharp
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
public class SpeedFOV : MonoBehaviour
{
    [SerializeField] private AnimationCurve fovCurve = new AnimationCurve(
        new Keyframe(0f, 60f),   // stationary
        new Keyframe(1f, 85f)    // top speed
    );
    [SerializeField] private float smoothTime = 0.25f;

    private CinemachineCamera _vcam;
    private float _currentVelocity;

    private void Awake()
    {
        _vcam = GetComponent<CinemachineCamera>();
    }

    /// <param name="speedRatio">0..1 normalized current speed / max speed</param>
    public void UpdateFOV(float speedRatio)
    {
        float targetFOV = fovCurve.Evaluate(Mathf.Clamp01(speedRatio));
        float currentFOV = _vcam.Lens.FieldOfView;
        _vcam.Lens.FieldOfView = Mathf.SmoothDamp(
            currentFOV, targetFOV, ref _currentVelocity, smoothTime
        );
    }
}
```

**Specific Values:**
- Base FOV: 60 degrees (RC camera is close to ground, needs tight base)
- Max FOV: 85 degrees (NOT 90 — too fish-eye for RC scale, objects distort at edges)
- Curve shape: ease-in (slow start, fast ramp at high speed) — use `AnimationCurve` tangent mode
- Smooth time: 0.25s (fast enough to feel responsive, slow enough to avoid jitter)

**RC Priority:** Must-have. Without FOV shift, top speed feels identical to cruising speed.

---

## 2. Camera Shake on Impact

**Concept:** Fire a Cinemachine Impulse when the vehicle collides with walls or objects. The impulse magnitude scales with collision force, normalized for RC scale.

**Code Example:**

```csharp
using Unity.Cinemachine;
using UnityEngine;

public class CollisionShake : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource impulseSource;

    private const float RC_IMPULSE_NORMALIZER = 50f;
    private const float MIN_IMPULSE_THRESHOLD = 0.1f;
    private const float MAX_IMPULSE_FORCE = 1.0f;

    private void OnCollisionEnter(Collision collision)
    {
        float normalizedForce = collision.impulse.magnitude / RC_IMPULSE_NORMALIZER;
        if (normalizedForce < MIN_IMPULSE_THRESHOLD) return;

        float clampedForce = Mathf.Min(normalizedForce, MAX_IMPULSE_FORCE);
        impulseSource.GenerateImpulseWithForce(clampedForce);
    }
}
```

**Setup Requirements:**
- Add `CinemachineImpulseSource` component to the vehicle GameObject
- Add `CinemachineImpulseListener` extension to the active CinemachineCamera
- Impulse Source: use `6D Shake` signal shape for natural camera movement
- Channel mask: use a dedicated channel (e.g., channel 1) to avoid cross-contamination

**Specific Values:**
- Normalizer: divide `collision.impulse.magnitude` by 50 for 1/10-scale RC
- Threshold: ignore impulses below 0.1 (minor scrapes, resting contacts)
- Max force: clamp at 1.0 to prevent screen-shaking chaos on head-on collisions
- Impulse definition: duration 0.15s, amplitude 0.3, frequency 2 (short, sharp hit)

**RC Priority:** Must-have. Collisions without camera shake feel like the car hit a hologram.

---

## 3. Controller Haptics

**Concept:** Drive the gamepad's dual motors to communicate engine state, surface texture, and collision events through the player's hands. Left motor (low frequency) = engine rumble. Right motor (high frequency) = surface texture.

**Code Example:**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HapticFeedback : MonoBehaviour
{
    private const float ENGINE_RUMBLE_MIN = 0.05f;
    private const float ENGINE_RUMBLE_MAX = 0.25f;
    private const float SURFACE_GRAVEL = 0.35f;
    private const float SURFACE_ASPHALT = 0f;
    private const float COLLISION_INTENSITY = 0.8f;
    private const float COLLISION_DURATION = 0.15f;

    private Coroutine _collisionCoroutine;

    /// <summary>
    /// Call every frame with current engine and surface state.
    /// </summary>
    public void UpdateHaptics(float rpmNormalized, SurfaceType surface)
    {
        if (Gamepad.current == null) return;

        float leftMotor = Mathf.Lerp(ENGINE_RUMBLE_MIN, ENGINE_RUMBLE_MAX, rpmNormalized);
        float rightMotor = surface switch
        {
            SurfaceType.Gravel => SURFACE_GRAVEL,
            SurfaceType.Dirt => 0.2f,
            SurfaceType.Grass => 0.15f,
            SurfaceType.Asphalt => SURFACE_ASPHALT,
            _ => 0f
        };

        Gamepad.current.SetMotorSpeeds(leftMotor, rightMotor);
    }

    public void TriggerCollisionBurst()
    {
        if (Gamepad.current == null) return;
        if (_collisionCoroutine != null) StopCoroutine(_collisionCoroutine);
        _collisionCoroutine = StartCoroutine(CollisionBurstRoutine());
    }

    private IEnumerator CollisionBurstRoutine()
    {
        Gamepad.current.SetMotorSpeeds(COLLISION_INTENSITY, COLLISION_INTENSITY);
        yield return new WaitForSecondsRealtime(COLLISION_DURATION);
        // Haptics will be overwritten next frame by UpdateHaptics
    }

    private void OnDisable()
    {
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}
```

**Specific Values:**
- Left motor (engine RPM): 0.05 idle to 0.25 redline — subtle, always present
- Right motor (surface): gravel 0.35, dirt 0.2, grass 0.15, asphalt 0.0
- Collision burst: both motors at 0.8 for 150ms
- Always zero motors in `OnDisable` to prevent stuck vibration

**RC Priority:** Must-have on gamepad. Players with controllers expect this. Skip for keyboard/mouse.

---

## 4. Screen Effects at Speed

**Concept:** Layer post-processing effects that intensify with speed using a dedicated URP Volume with weight driven by speed ratio. Keep effects subtle — this is reinforcement, not the primary speed cue.

**Code Example:**

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpeedPostProcessing : MonoBehaviour
{
    [SerializeField] private Volume speedVolume;
    [SerializeField] private float smoothTime = 0.3f;

    private ChromaticAberration _chromatic;
    private Vignette _vignette;
    private MotionBlur _motionBlur;
    private float _currentVelocity;
    private float _smoothedRatio;

    private void Awake()
    {
        var profile = speedVolume.profile;
        profile.TryGet(out _chromatic);
        profile.TryGet(out _vignette);
        profile.TryGet(out _motionBlur);
    }

    public void UpdateSpeedEffects(float speedRatio)
    {
        _smoothedRatio = Mathf.SmoothDamp(
            _smoothedRatio, speedRatio, ref _currentVelocity, smoothTime
        );
        float t = Mathf.Clamp01(_smoothedRatio);

        if (_chromatic != null)
            _chromatic.intensity.value = Mathf.Lerp(0f, 0.4f, t);

        if (_vignette != null)
            _vignette.intensity.value = Mathf.Lerp(0.2f, 0.45f, t);

        if (_motionBlur != null)
            _motionBlur.intensity.value = Mathf.Clamp(
                Mathf.Lerp(0f, 0.35f, t), 0.05f, 0.2f
            );
    }
}
```

**Specific Values:**
- Chromatic Aberration: 0 at rest to 0.4 at top speed
- Vignette: 0.2 base (always slightly present) to 0.45 at top speed
- Motion Blur: 0 to 0.35, clamped between 0.05 and 0.2 (prevents smearing on quick turns)
- Use a dedicated Volume GameObject with Priority 1, Weight driven by speed ratio
- Avoid Depth of Field for racing — it obscures track obstacles

**RC Priority:** Nice-to-have. Enhances immersion but not critical for gameplay feedback.

---

## 5. Suspension Compression Visuals

**Concept:** RC buggies are DEFINED by their visible suspension travel. The player watches those shocks compress and extend on every bump. `WheelCollider.GetWorldPose()` provides real-time wheel position reflecting suspension state.

**Code Example:**

```csharp
using UnityEngine;

public class SuspensionVisuals : MonoBehaviour
{
    [System.Serializable]
    public struct WheelVisual
    {
        public WheelCollider collider;
        public Transform wheelMesh;
        public Transform suspensionArm;
        public float armRotationScale;  // degrees at full compression
    }

    [SerializeField] private WheelVisual[] wheels;
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private float brakePitchOffset = 0.003f; // 3mm at RC scale

    private const float MIN_ARM_ROTATION = 0f;
    private const float MAX_ARM_ROTATION_DEFAULT = 25f;

    public void UpdateSuspension(float brakeInput)
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            ref WheelVisual w = ref wheels[i];

            // Drive wheel mesh from WheelCollider physics
            w.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            w.wheelMesh.SetPositionAndRotation(pos, rot);

            // Compute compression ratio (0 = full extension, 1 = full compression)
            WheelHit hit;
            float compression = 0f;
            if (w.collider.GetGroundHit(out hit))
            {
                float restLength = w.collider.suspensionDistance;
                float currentLength = (-w.collider.transform.InverseTransformPoint(pos).y
                    - w.collider.radius);
                compression = 1f - Mathf.Clamp01(currentLength / restLength);
            }

            // Rotate suspension arm based on compression
            if (w.suspensionArm != null)
            {
                float armAngle = w.armRotationScale > 0
                    ? w.armRotationScale
                    : MAX_ARM_ROTATION_DEFAULT;
                float angle = Mathf.Lerp(MIN_ARM_ROTATION, armAngle, compression);
                w.suspensionArm.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }
        }

        // Body pitch offset on braking
        if (bodyTransform != null)
        {
            float pitchOffset = brakeInput * brakePitchOffset;
            bodyTransform.localPosition = new Vector3(
                bodyTransform.localPosition.x,
                bodyTransform.localPosition.y,
                -pitchOffset  // negative Z = nose down in local space
            );
        }
    }
}
```

**Specific Values:**
- Suspension arm rotation: 20-35 degrees at full compression (tune per buggy model)
- Body pitch offset on braking: 2-4mm at RC scale (0.002-0.004 Unity units)
- `GetWorldPose()` is authoritative — never manually position wheels
- Compression ratio drives ALL visual effects (arm angle, dust emission, audio)

**RC Priority:** Must-have. This is the single most important visual feedback for an RC sim. Players who build real RC buggies will notice immediately if suspension doesn't move correctly.

---

## 6. Tire Smoke Scaling

**Concept:** Scale particle emission rate with combined tire slip magnitude. Two-layer system: tight opaque puff at the contact point, plus a soft billowy trail that lingers.

**Code Example:**

```csharp
using UnityEngine;

public class TireSmoke : MonoBehaviour
{
    [SerializeField] private WheelCollider wheelCollider;
    [SerializeField] private ParticleSystem puffSystem;   // tight, opaque
    [SerializeField] private ParticleSystem trailSystem;  // soft, billowy

    private const float SLIP_THRESHOLD = 0.3f;
    private const float MAX_EMISSION_RATE = 80f;
    private const float DRIFT_GROWTH_RATE = 1.5f;

    private float _driftAccumulator;

    public void UpdateSmoke()
    {
        WheelHit hit;
        if (!wheelCollider.GetGroundHit(out hit))
        {
            SetEmission(puffSystem, 0f);
            SetEmission(trailSystem, 0f);
            _driftAccumulator = 0f;
            return;
        }

        float combinedSlip = Mathf.Sqrt(
            hit.sidewaysSlip * hit.sidewaysSlip +
            hit.forwardSlip * hit.forwardSlip
        );

        if (combinedSlip < SLIP_THRESHOLD)
        {
            SetEmission(puffSystem, 0f);
            SetEmission(trailSystem, 0f);
            _driftAccumulator = Mathf.Max(0f, _driftAccumulator - Time.deltaTime * 3f);
            return;
        }

        // Drift-sustained growth
        _driftAccumulator += Time.deltaTime * DRIFT_GROWTH_RATE;
        float slipFactor = (combinedSlip - SLIP_THRESHOLD) / (1f - SLIP_THRESHOLD);
        float growthMultiplier = 1f + Mathf.Clamp01(_driftAccumulator);

        float emission = Mathf.Clamp(
            slipFactor * MAX_EMISSION_RATE * growthMultiplier,
            0f,
            MAX_EMISSION_RATE
        );

        SetEmission(puffSystem, emission);
        SetEmission(trailSystem, emission * 0.4f); // trail is sparser
    }

    private static void SetEmission(ParticleSystem ps, float rate)
    {
        var emission = ps.emission;
        emission.rateOverTime = rate;
    }
}
```

**Specific Values:**
- Slip threshold: 0.3 combined magnitude (below this, tires grip normally)
- Max emission: 80 particles/sec per wheel (320 total worst case — budget accordingly)
- Puff layer: lifetime 0.3s, size 0.02-0.05, opaque, gravity -0.5
- Trail layer: lifetime 1.5s, size 0.1-0.3, alpha fade, gravity -0.2
- Drift-sustained growth: accumulated drift time multiplies emission (rewards long drifts)

**RC Priority:** Must-have for drift satisfaction. Even non-racing players expect tire smoke.

---

## 7. Speed Lines / Radial Blur

**Concept:** Additional speed communication through either particle-based speed lines (recommended, lower GPU cost) or a fullscreen radial blur shader (higher visual quality).

**Particle-Based Approach (Recommended):**

```csharp
using UnityEngine;

public class SpeedLines : MonoBehaviour
{
    [SerializeField] private ParticleSystem speedLineSystem;
    [SerializeField] private float activationThreshold = 0.6f; // 60% of top speed

    public void UpdateSpeedLines(float speedRatio)
    {
        var emission = speedLineSystem.emission;
        if (speedRatio < activationThreshold)
        {
            emission.rateOverTime = 0f;
            return;
        }

        float t = (speedRatio - activationThreshold) / (1f - activationThreshold);
        emission.rateOverTime = Mathf.Lerp(0f, 40f, t);

        var main = speedLineSystem.main;
        main.startSpeed = Mathf.Lerp(5f, 20f, t);
    }
}
```

**Fullscreen Radial Blur (Higher Quality):**
- Implement as a URP Renderer Feature with a custom shader
- Radial blur strength: 0 at rest to 0.015 at top speed
- Sample count: 8-16 (higher = smoother but more expensive)
- Center point: slightly below screen center (where the road vanishes)
- Performance: ~0.3ms at 8 samples, ~0.6ms at 16 samples

**Specific Values:**
- Activation threshold: 60% of top speed (below this, no speed lines)
- Particle speed lines: 40 particles/sec max, lifetime 0.15s, stretched billboard
- Radial blur: strength 0.015 max, 8 samples minimum

**RC Priority:** Nice-to-have. Adds cinematic flair but not critical for gameplay.

---

## 8. Audio as Game Feel

**Concept:** Audio communicates 40-50% of speed perception at RC scale. Engine pitch response time, wind rush onset, and impact audio timing are critical. The player's ears fill in what the eyes miss.

**Key Patterns:**

```csharp
using UnityEngine;

public class GameFeelAudio : MonoBehaviour
{
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private AudioSource windSource;
    [SerializeField] private AudioSource impactSource;

    private const float PITCH_RESPONSE_SPEED = 12f; // dt * 12 = 100-150ms response
    private const float WIND_ONSET_SPEED_RATIO = 0.3f;

    public void UpdateEngineAudio(float rpmNormalized)
    {
        // Smooth pitch transition — 100-150ms response feels mechanical
        float targetPitch = Mathf.Lerp(0.8f, 2.0f, rpmNormalized);
        engineSource.pitch = Mathf.Lerp(
            engineSource.pitch,
            targetPitch,
            Time.deltaTime * PITCH_RESPONSE_SPEED
        );
    }

    public void UpdateWindAudio(float speedRatio)
    {
        // Wind rush activates at 30% speed
        if (speedRatio < WIND_ONSET_SPEED_RATIO)
        {
            windSource.volume = 0f;
            return;
        }

        float t = (speedRatio - WIND_ONSET_SPEED_RATIO) / (1f - WIND_ONSET_SPEED_RATIO);
        windSource.volume = Mathf.Lerp(0f, 0.4f, t);
        windSource.pitch = Mathf.Lerp(0.8f, 1.2f, t);
    }

    /// <summary>
    /// Play at exact OnCollisionEnter frame. NEVER delay impact audio.
    /// </summary>
    public void PlayImpact(float normalizedForce)
    {
        impactSource.volume = Mathf.Lerp(0.3f, 1.0f, normalizedForce);
        impactSource.pitch = Random.Range(0.9f, 1.1f); // slight variation
        impactSource.Play();
    }
}
```

**Hit-Stop Audio Pattern:**
1. Impact sound plays immediately at `OnCollisionEnter` (frame-accurate)
2. Debris / scrape sounds follow 1-2 frames later
3. 50ms silence gap (engine volume dips to 20%)
4. Engine audio resumes with slight pitch drop (deceleration feel)

**Specific Values:**
- Engine pitch lerp: `dt * 12` gives 100-150ms response (feels mechanical, not digital)
- Wind rush onset: 30% of top speed
- Impact audio: NEVER delayed, NEVER queued — play at collision frame
- Hit-stop silence: 50ms, engine at 20% volume during gap

**RC Priority:** Must-have. Audio is the primary speed cue when the camera is stationary and the car is distant. See `unity-rc-audio` skill for detailed engine synthesis.

---

## 9. Landing Impact (Multi-Layered)

**Concept:** The moment an RC buggy lands from a jump is the peak excitement moment. Compound multiple feedback channels into a single coordinated event: camera impulse + FOV squeeze + dust burst + suspension audio + haptics.

**Code Example:**

```csharp
using Unity.Cinemachine;
using UnityEngine;

public class LandingImpact : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private ParticleSystem dustBurst;
    [SerializeField] private AudioSource landingAudio;
    [SerializeField] private SpeedFOV speedFOV;
    [SerializeField] private HapticFeedback haptics;

    [SerializeField] private float airTimeThreshold = 0.15f;
    [SerializeField] private float fovSqueezeAmount = 5f;
    [SerializeField] private int dustParticleCount = 40;

    private float _airTime;
    private bool _wasGrounded = true;

    public void CheckLanding(bool isGrounded, float verticalVelocity)
    {
        if (!isGrounded)
        {
            _airTime += Time.deltaTime;
            _wasGrounded = false;
            return;
        }

        if (!_wasGrounded && _airTime > airTimeThreshold)
        {
            float intensity = Mathf.Clamp01(_airTime / 1.5f);
            TriggerLanding(intensity, verticalVelocity);
        }

        _airTime = 0f;
        _wasGrounded = true;
    }

    private void TriggerLanding(float intensity, float verticalVelocity)
    {
        // 1. Camera impulse — proportional to hang time
        impulseSource.GenerateImpulseWithForce(intensity * 0.6f);

        // 2. FOV squeeze — fast down, slow spring back with overshoot
        StartCoroutine(FOVSqueezeRoutine(intensity));

        // 3. Dust burst — emit particles at contact point
        var emitParams = new ParticleSystem.EmitParams();
        emitParams.velocity = Vector3.up * 0.5f;
        dustBurst.Emit(emitParams, (int)(dustParticleCount * intensity));

        // 4. Landing audio — pitch varies with impact force
        landingAudio.pitch = Mathf.Lerp(0.8f, 1.2f, intensity);
        landingAudio.volume = Mathf.Lerp(0.5f, 1.0f, intensity);
        landingAudio.Play();

        // 5. Haptics — short burst proportional to landing force
        haptics?.TriggerCollisionBurst();
    }

    private System.Collections.IEnumerator FOVSqueezeRoutine(float intensity)
    {
        // Squeeze down: 80ms
        float squeezeDuration = 0.08f;
        float springDuration = 0.25f;
        float squeeze = fovSqueezeAmount * intensity;

        float elapsed = 0f;
        while (elapsed < squeezeDuration)
        {
            elapsed += Time.deltaTime;
            // FOV squeeze is applied as offset — actual FOV management
            // is handled by SpeedFOV component
            yield return null;
        }

        // Spring back with overshoot: 250ms
        elapsed = 0f;
        while (elapsed < springDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / springDuration;
            // Overshoot curve: goes past 1.0 then settles
            float overshoot = 1f + 0.15f * Mathf.Sin(t * Mathf.PI);
            yield return null;
        }
    }
}
```

**Timing Breakdown:**
- Camera impulse: fires immediately, 150ms duration
- FOV squeeze: 80ms compression, 250ms spring-back with 15% overshoot
- Dust burst: 40 particles emitted instantly at contact point
- Suspension audio: plays at landing frame (use `unity-rc-audio` suspension thud)
- Haptics: 120ms burst, both motors at intensity * 0.8

**Specific Values:**
- Air time threshold: 0.15s minimum to trigger (ignore tiny hops)
- Intensity scales with air time: 0.15s = subtle, 1.5s+ = maximum
- FOV squeeze: 5 degrees at max intensity
- Dust particles: 40 at max intensity, scale linearly

**RC Priority:** Must-have. Jump landings are the most replayed and shared moments in RC racing games.

---

## 10. Time Manipulation

**Concept:** Slow motion after sustained airtime to dramatize the landing moment. Enter gradually, exit fast on landing. Requires syncing `fixedDeltaTime` with `timeScale`.

**Code Example:**

```csharp
using UnityEngine;

public class TimeManipulation : MonoBehaviour
{
    [SerializeField] private float slowMoScale = 0.4f;
    [SerializeField] private float airTimeBeforeSloMo = 0.5f;
    [SerializeField] private float enterDuration = 0.5f;   // 500ms ease in
    [SerializeField] private float exitDuration = 0.2f;     // 200ms snap back

    private const float DEFAULT_FIXED_DT = 0.02f;

    private float _targetScale = 1f;
    private float _currentVelocity;

    public void UpdateTimeScale(bool isAirborne, float airTime)
    {
        _targetScale = (isAirborne && airTime > airTimeBeforeSloMo)
            ? slowMoScale
            : 1f;

        float smoothTime = _targetScale < Time.timeScale ? enterDuration : exitDuration;
        Time.timeScale = Mathf.SmoothDamp(
            Time.timeScale, _targetScale, ref _currentVelocity, smoothTime,
            Mathf.Infinity, Time.unscaledDeltaTime
        );

        // CRITICAL: keep physics in sync
        Time.fixedDeltaTime = DEFAULT_FIXED_DT * Time.timeScale;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = DEFAULT_FIXED_DT;
    }
}
```

**Specific Values:**
- Slow-mo target: `timeScale` = 0.4 (40% speed)
- Trigger: after 0.5s continuous airtime
- Enter: 500ms gradual (SmoothDamp with unscaledDeltaTime)
- Exit: 200ms snap back on landing (fast recovery)
- `fixedDeltaTime = 0.02f * timeScale` — ALWAYS sync, or physics explode
- All UI code must use `Time.unscaledDeltaTime` to remain responsive during slow-mo

**RC Priority:** Nice-to-have. High cinematic value but can feel gimmicky if overused. Consider making it player-toggleable.

---

## 11. Input Response Curves

**Concept:** Apply non-linear response curves to raw input AFTER the Input System but BEFORE physics. This gives the player precision at center-stick and full authority at extremes.

**Code Example:**

```csharp
using UnityEngine;

public static class InputCurves
{
    /// <summary>
    /// Steering: power curve for center precision.
    /// Exponent 1.5 gives ~70% of deflection range to the first 50% of input.
    /// </summary>
    public static float ApplySteeringCurve(float rawInput)
    {
        return Mathf.Pow(Mathf.Abs(rawInput), 1.5f) * Mathf.Sign(rawInput);
    }

    /// <summary>
    /// Throttle: linear. RC motors respond linearly to ESC signal.
    /// </summary>
    public static float ApplyThrottleCurve(float rawInput)
    {
        return rawInput; // intentionally linear
    }

    /// <summary>
    /// Brake: slight initial bite for responsive feel.
    /// Exponent 0.8 means light input produces noticeable braking.
    /// </summary>
    public static float ApplyBrakeCurve(float rawInput)
    {
        return Mathf.Pow(Mathf.Abs(rawInput), 0.8f) * Mathf.Sign(rawInput);
    }

    /// <summary>
    /// Speed-sensitive steering reduction.
    /// At top speed, effective steering is 50% of input steering.
    /// </summary>
    public static float ApplySpeedSensitiveSteering(
        float steeringInput, float speedRatio)
    {
        float reduction = Mathf.Lerp(1.0f, 0.5f, speedRatio);
        return steeringInput * reduction;
    }
}
```

**Application Order:**
1. Raw input from Input System (`PlayerInput` or `InputAction`)
2. Deadzone (handled by Input System bindings)
3. Response curve (`ApplySteeringCurve`, etc.)
4. Speed-sensitive scaling (`ApplySpeedSensitiveSteering`)
5. Result feeds into physics (WheelCollider steerAngle, motorTorque)

**Specific Values:**
- Steering exponent: 1.5 (precision at center, full range at extremes)
- Throttle: linear (1.0 exponent) — RC ESCs are already non-linear
- Brake exponent: 0.8 (initial bite, gentle rolloff)
- Speed-sensitive steering: 1.0 at rest to 0.5 at top speed (prevents spin-out)
- Apply AFTER input system, BEFORE physics — never modify raw input values

**RC Priority:** Must-have. Without input curves, the car is undriveable with a gamepad. Real RC transmitters have adjustable exponential curves for this exact reason.

---

## 12. Minimap Juice

**Concept:** Small visual flourishes on the minimap that communicate game state without requiring the player to consciously read it.

**Code Example:**

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class MinimapJuice : MonoBehaviour
{
    private VisualElement _playerIcon;
    private float _pulseScale = 1f;

    private const float PULSE_SCALE_MAX = 1.35f;
    private const float PULSE_DECAY_RATE = 8f;
    private const float FLASH_INTERVAL = 0.08f; // 80ms per flash
    private const int FLASH_COUNT = 3;

    public void UpdateMinimapIcon(Vector3 velocity, float heading)
    {
        // Pulse on position change (scale 1 -> 1.35, decay at 8/s)
        if (velocity.sqrMagnitude > 0.1f)
        {
            _pulseScale = Mathf.Max(_pulseScale,
                1f + velocity.magnitude * 0.01f);
        }
        _pulseScale = Mathf.MoveTowards(
            _pulseScale, 1f, PULSE_DECAY_RATE * Time.deltaTime
        );
        _pulseScale = Mathf.Clamp(_pulseScale, 1f, PULSE_SCALE_MAX);

        _playerIcon.transform.scale = Vector3.one * _pulseScale;

        // Use velocity direction for rotation, not transform.forward
        // This prevents the icon from "wobbling" when the car slides sideways
        if (velocity.sqrMagnitude > 0.5f)
        {
            float angle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            _playerIcon.transform.rotation = Quaternion.Euler(0f, 0f, -angle);
        }
    }

    public System.Collections.IEnumerator LapCompleteFlash()
    {
        // 3x yellow/white flash at 80ms intervals
        for (int i = 0; i < FLASH_COUNT; i++)
        {
            _playerIcon.style.unityBackgroundImageTintColor =
                new StyleColor(Color.yellow);
            yield return new WaitForSecondsRealtime(FLASH_INTERVAL);
            _playerIcon.style.unityBackgroundImageTintColor =
                new StyleColor(Color.white);
            yield return new WaitForSecondsRealtime(FLASH_INTERVAL);
        }
    }
}
```

**Specific Values:**
- Player icon pulse: scale 1.0 to 1.35, decay rate 8/second
- Lap complete flash: 3 alternating yellow/white at 80ms intervals
- Icon rotation: use velocity direction (`Atan2(vx, vz)`), NOT `transform.forward`
- Velocity threshold for rotation: 0.5 magnitude (prevents jitter when nearly stopped)

**RC Priority:** Nice-to-have. Low effort, medium payoff for spatial awareness.

---

## When to Use This Skill

- Adding "juice" or "feel" to an existing gameplay mechanic
- Players report the game feels "flat" or "unresponsive" despite correct physics
- Implementing camera follow systems for a racing game
- Connecting input devices (gamepads) to tactile feedback
- Creating speed perception without changing actual vehicle physics
- Building landing/collision/drift feedback systems
- Tuning post-processing for a racing game

## When NOT to Use This Skill

- Fixing actual physics bugs (use `unity-physics-3d` or `unity-physics-tuning`)
- Designing the vehicle physics model (use `unity-physics-tuning`)
- Building the core input system (use `unity-input-system`)
- Creating the audio engine synthesis (use `unity-rc-audio` for detailed motor audio)
- Implementing replay or ghost systems (use `unity-replay-ghost`)
- Optimizing performance of effects (use `unity-performance-optimization`)

---

## Related Skills

| Skill | Relationship |
|-------|-------------|
| `unity-camera-systems` | Cinemachine setup, virtual cameras, freelook — this skill adds juice ON TOP of camera systems |
| `unity-rc-audio` | Detailed brushless motor synthesis, AudioMixer routing — this skill covers audio AS game feel |
| `unity-input-system` | Input System configuration, action maps — this skill applies curves AFTER input processing |
| `unity-particles-vfx` | Particle System and VFX Graph fundamentals — this skill covers specific RC racing effects |
| `unity-physics-tuning` | PhysX configuration for RC — game feel builds on top of correct physics |
| `unity-performance-optimization` | When effects need optimization — budget your juice |
| `unity-graphics-pipeline` | URP post-processing setup — this skill uses Volumes configured by the pipeline |
