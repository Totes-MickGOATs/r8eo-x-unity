# Unity RC Audio

Use this skill when implementing audio for an RC car racing game. RC cars sound nothing like full-size vehicles -- brushless motors produce a high-pitched whine, not engine rumble. Covers motor audio synthesis, RPM crossfade, ESC/servo/tire/impact sounds, AudioMixer architecture, Doppler configuration, opponent audio LOD, and compression settings.

---

## Brushless Motor Audio

RC cars use brushless DC (BLDC) motors, not internal combustion engines. The primary sound is electromagnetic commutation — a high-pitched whine that scales linearly with RPM.

### Commutation Frequency

```
frequency_hz = (pole_pairs * RPM) / 60
```

- Typical RC motors: 2-4 pole pairs
- Operating range: **500 Hz to 10,000 Hz** (audible whine, not rumble)
- The relationship between RPM and pitch is **linear** — no gear shifts, no combustion cycles
- At idle/low speed: faint electrical hum around 500-800 Hz
- At full throttle: piercing whine at 6,000-10,000 Hz

### Sensored vs Sensorless

| Type | Startup Sound | Low-Speed Behavior |
|------|--------------|-------------------|
| **Sensored** | Smooth, quiet startup | Clean tone even at crawl speed |
| **Sensorless** | Brief cogging/stutter on start | Rough tone below ~10% throttle, then smooths out |

Most racing setups use sensored motors. For realism, add a brief stutter sound on initial throttle application for sensorless configurations.

### What Brushless Motors Are NOT

- **NOT ICE** — no combustion, no exhaust note, no intake roar
- **NOT turbine** — no spool-up lag (though ESC ramp rate creates a slight delay)
- **NOT diesel** — no knocking, no turbo whistle
- Do not layer any combustion-style audio. The motor is purely electromagnetic.

---

## RPM Crossfade System

The most effective approach for motor audio is sampling real RC motors at fixed RPM points and crossfading between them.

### Architecture

Record or source **3-5 loops** at fixed RPM points:

| Layer | RPM Range | Character |
|-------|-----------|-----------|
| `motor_idle` | 0-15% | Low electrical hum, slight buzz |
| `motor_low` | 15-35% | Rising whine, still quiet |
| `motor_mid` | 35-60% | Prominent whine, smooth |
| `motor_high` | 60-85% | Loud, intense whine |
| `motor_max` | 85-100% | Screaming high pitch, slight harmonic distortion |

### Crossfade Implementation

Use `AnimationCurve` assets to map `normalizedRPM` (0.0-1.0) to both pitch and volume for each layer:

```csharp
[System.Serializable]
public class MotorAudioLayer
{
    public AudioSource source;
    public AudioClip clip;
    public AnimationCurve volumeCurve; // X: normalizedRPM, Y: volume (0-1)
    public AnimationCurve pitchCurve;  // X: normalizedRPM, Y: pitch (0.5-2.0)
}

public class RCMotorAudio : MonoBehaviour
{
    [SerializeField] private MotorAudioLayer[] layers;

    /// <summary>
    /// Call every frame with the current motor RPM normalized to 0-1.
    /// </summary>
    public void UpdateMotorAudio(float normalizedRPM)
    {
        normalizedRPM = Mathf.Clamp01(normalizedRPM);

        for (int i = 0; i < layers.Length; i++)
        {
            MotorAudioLayer layer = layers[i];
            layer.source.volume = layer.volumeCurve.Evaluate(normalizedRPM);
            layer.source.pitch = layer.pitchCurve.Evaluate(normalizedRPM);
        }
    }
}
```

### Motor Strain Layer

Add an additional audio layer for high-load conditions (full throttle at low speed, climbing steep inclines):

- Lower-pitched buzz/growl overlaid on the main motor whine
- Volume driven by `throttleInput * (1.0f - normalizedSpeed)` — maximum at full throttle with no movement
- Represents the motor drawing high current under load
- Slight pitch wobble (0.98-1.02 random modulation) adds realism

---

## ESC Sounds

The Electronic Speed Controller produces several distinctive sounds.

### Startup Melody

When the ESC initializes (car spawns or powers on), it plays a short musical sequence:

```csharp
// Play once on spawn — do NOT loop
audioSource.PlayOneShot(escStartupMelody);
```

- Typically 1-3 seconds of ascending/descending tones
- Indicates throttle calibration completion
- Iconic sound that RC enthusiasts recognize immediately
- Play via `PlayOneShot` on the motor AudioSource, or a dedicated one-shot source

### Brake Engagement

When electronic braking activates:

- Brief high-frequency squeal/chirp
- Duration proportional to braking intensity
- Distinct from tire squeal — this is electromagnetic, not friction

### Battery Voltage Alerts

Low voltage cutoff (LVC) produces warning beeps:

- Repeating beep pattern when battery drops below threshold
- Gameplay trigger: warn player to pit or battery performance degrades
- Simple sine wave beeps, 1-2 kHz, 100-200ms per beep

---

## Servo Audio

The steering servo produces a characteristic whine when turning.

### Characteristics

- **Frequency**: proportional to steering input rate and mechanical load
- **Duration**: continuous while servo is moving, fades when stationary
- **Character**: high-pitched electric whine, similar to motor but quieter and more mechanical

### AudioSource Configuration

| Parameter | Value |
|-----------|-------|
| Clip | 0.5-1.0 second servo whine loop |
| Spatial Blend | 0.9 (mostly 3D, slight 2D presence) |
| Min Distance | 0.5m |
| Max Distance | 5m |
| Loop | Yes |
| Volume | Driven by `Mathf.Abs(steeringInputDelta)` |

### Implementation Notes

- Volume scales with steering input **rate of change**, not absolute position
- When the servo reaches its target angle and stops, volume fades to zero over ~100ms
- Under load (wheels against a wall, fighting terrain), pitch drops slightly and volume increases
- Servo sound is subtle — it should be audible in quiet moments but masked by motor at speed

---

## Tire / Surface Audio

RC tire audio differs significantly from full-size car audio. The tires are small, lightweight rubber or foam compounds on miniature rims.

### Per-Surface Characteristics

| Surface | Sound Character | Base Volume | Pitch Range |
|---------|----------------|-------------|-------------|
| **Dirt** | Crunchy, gritty, granular | 0.7 | 0.9-1.1 |
| **Asphalt** | Smooth hiss, light buzz | 0.5 | 1.0-1.2 |
| **Grass** | Swishing, soft rustling | 0.6 | 0.8-1.0 |
| **Gravel** | Scattering, rattling pebbles | 0.8 | 0.7-1.1 |
| **Sand** | Muffled crunch, spraying | 0.6 | 0.8-0.9 |

### Slip-Dependent Scrub

When tires slip (lateral or longitudinal), add a scrub/squeal layer:

- Volume proportional to slip ratio: `Mathf.InverseLerp(0.1f, 0.8f, slipRatio)`
- RC tire squeal is **thin and high-pitched** — imagine a small rubber eraser on a desk
- **NEVER use full-size car tire squeal recordings** — they are too deep, too resonant, too loud
- Pitch-shift real car tire samples **+4 to +6 semitones** if you must use them as a starting point

### Multiple Contact Points

For realistic tire audio, consider separate sources per axle (front/rear) rather than a single source:

- Front tires: more prominent during cornering (lateral slip)
- Rear tires: more prominent during acceleration/braking (longitudinal slip)
- Minimum: one source for rolling, one for slip. Ideal: per-axle sources.

---

## Impact Sounds

RC car bodies are thin Lexan (polycarbonate) shells over rigid chassis. Impacts sound hollow and plastic, never metallic.

### Lexan Body Impacts

- **Sound character**: hollow clack, thwack, plastic snap — NOT metallic clang or crash
- **Lightweight**: RC cars weigh 2-5 kg — impacts are quick and snappy, not heavy thuds
- **Body flex**: Lexan flexes on impact, producing a distinctive wobble/vibration after the initial hit

### Implementation

```csharp
public class RCImpactAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] impactClips; // 3-5 variations minimum
    [SerializeField] private AudioSource impactSource;
    [SerializeField] private float minImpulse = 2f;
    [SerializeField] private float maxImpulse = 20f;

    private void OnCollisionEnter(Collision collision)
    {
        float impulse = collision.impulse.magnitude;
        if (impulse < minImpulse) return;

        float volumeScale = Mathf.InverseLerp(minImpulse, maxImpulse, impulse);
        AudioClip clip = impactClips[Random.Range(0, impactClips.Length)];
        impactSource.PlayOneShot(clip, volumeScale);
    }
}
```

### AudioRandomContainer

> **Unity 6:** `AudioRandomContainer` is a standard feature (no longer experimental). It is an `AudioResource`, the new base type accepted by `AudioSource.resource` (which supplements the older `AudioSource.clip`). You can assign an `AudioRandomContainer` directly to an `AudioSource.resource` field -- the source will automatically handle clip variation without any scripting. This is the preferred approach for impact sounds, footsteps, and any one-shot with multiple variations.

- Create via **Assets > Create > Audio > Audio Random Container**
- Load 3-5 impact variations into the container
- Configure: random or sequential mode, avoid-repeat count, volume/pitch randomization ranges
- Assign to `AudioSource.resource` in the Inspector or via code:

```csharp
[SerializeField] private AudioResource impactContainer; // Assign AudioRandomContainer asset
audioSource.resource = impactContainer;
audioSource.Play(); // Automatically picks a variation
```

### AudioSource Pooling

**Never use `AudioSource.PlayClipAtPoint`** — it creates and destroys GameObjects every call, generating garbage.

Instead, pool AudioSources:

- Pre-instantiate 4-8 AudioSource components on a pooling object
- Round-robin through them for one-shot playback
- Reset pitch/volume before each play
- Recycle sources whose clips have finished playing

---

## Environmental Audio

### Wind Rush

Wind audio scales with vehicle speed:

```csharp
float speedRatio = currentSpeed / maxSpeed;
windSource.volume = Mathf.Pow(speedRatio, 2f) * maxWindVolume; // Quadratic scaling
windSource.pitch = 0.8f + (speedRatio * 0.4f); // 0.8 to 1.2
```

- **Start threshold**: begin wind audio at ~30% of max speed (below this, inaudible)
- **Spatial blend**: 0.0 (fully 2D) — wind is the player's experience, not positional
- **Character**: broad white/pink noise with slight pitch modulation

### Outdoor Ambient

- Light ambient bed: distant nature sounds, wind through vegetation
- Keep it subtle — RC racing is typically outdoors in parks, dirt lots, or tracks
- Volume should duck during high-speed sections when motor/wind dominate

### Reverb Zones

If the track includes tunnels, underpasses, or enclosed areas:

- Use Unity `AudioReverbZone` components placed at tunnel entrances
- Preset: `Hallway` or `StoneCorridor` for short tunnels, `Cave` for longer ones
- Reverb dramatically enhances the motor whine in enclosed spaces — players notice and appreciate this

---

## AudioMixer Architecture

### Group Hierarchy

```
Master
├── Music
├── SFX
│   ├── Vehicle
│   │   ├── Motor
│   │   ├── Servo
│   │   └── Tires
│   ├── Impact
│   └── Environment
│       ├── Wind
│       └── Ambient
└── UI
```

### Mixer Snapshots

| Snapshot | Use Case | Key Settings |
|----------|----------|-------------|
| **Racing** | Normal gameplay | All groups at designed levels |
| **Paused** | Pause menu open | Lowpass filter on SFX (cutoff ~800 Hz), reduce SFX volume -10dB, Music at full |
| **Menu** | Main menu / garage | SFX muted, Music at full, UI at full |
| **Replay** | Watching replay | Full SFX, Music reduced -6dB |

### Volume Slider Conversion

Human perception of loudness is logarithmic. Always convert linear slider values (0-1) to decibels:

```csharp
public static float LinearToDecibel(float linear)
{
    if (linear <= 0.0001f) return -80f; // Effectively silent
    return 20f * Mathf.Log10(linear);
}

// Usage: mixer.SetFloat("MasterVolume", LinearToDecibel(sliderValue));
```

Never expose raw decibel values to players. Always use a 0-100% or 0-1 linear scale in the UI.

---

## AudioSource Setup Table

Reference table for configuring each sound source:

| Source | Clip Type | Spatial Blend | Min Distance | Max Distance | Loop | Priority |
|--------|-----------|--------------|-------------|-------------|------|----------|
| Motor | Sampled loops | 0.95 | 1m | 20m | Yes | 0 (highest) |
| Servo | Short loop | 0.9 | 0.5m | 5m | Yes | 64 |
| Tires (roll) | Surface loops | 1.0 | 0.3m | 10m | Yes | 32 |
| Tires (slip) | Scrub loop | 1.0 | 0.3m | 10m | Yes | 32 |
| Wind | Noise loop | 0.0 (2D) | — | — | Yes | 128 |
| Impact | One-shots | 1.0 | 0.5m | 15m | No | 16 |
| ESC Melody | One-shot | 0.8 | 1m | 10m | No | 64 |
| Ambient | Long loop | 0.0 (2D) | — | — | Yes | 200 |

---

## Doppler Effect

### Player's Own Car

**DISABLE Doppler on the player's own car** — `dopplerLevel = 0.0f`.

The camera follows the player car, so relative velocity is near zero. Any Doppler shift is an artifact and sounds wrong (warbling pitch).

### Opponent Cars

Enable Doppler on all opponent vehicle audio sources:

- `dopplerLevel = 1.0f` to `1.5f` (slight exaggeration feels more exciting)
- The classic RC car flyby sound: rising pitch on approach, falling pitch on departure
- Only the motor source needs Doppler — servo and tire sounds are too quiet to matter at opponent distance

### Spectator Camera

If you implement a spectator or trackside camera mode:

- Enable full Doppler on ALL vehicles (including what would be the player's car)
- `dopplerLevel = 1.0f` for realistic trackside experience
- This is when Doppler sounds most dramatic and satisfying

---

## Opponent LOD Audio

For performance, simplify opponent audio based on distance from the listener.

### Distance-Based LOD

```csharp
public class OpponentAudioLOD : MonoBehaviour
{
    [SerializeField] private AudioSource motorSource;
    [SerializeField] private float disableDistance = 25f;
    [SerializeField] private float fullVolumeDistance = 5f;
    [SerializeField] private float fadeStartDistance = 20f;

    private Transform listener;

    private void Update()
    {
        float distance = Vector3.Distance(transform.position, listener.position);

        if (distance > disableDistance)
        {
            motorSource.enabled = false;
            return;
        }

        motorSource.enabled = true;
        motorSource.volume = Mathf.InverseLerp(fadeStartDistance, fullVolumeDistance, distance)
                           * baseVolume;
    }
}
```

### LOD Guidelines

| Distance | Audio Level |
|----------|------------|
| 0-5m | Full detail: motor + servo + tires |
| 5-15m | Motor only, full volume |
| 15-20m | Motor only, fading volume |
| 20-25m | Motor only, minimal volume |
| 25m+ | AudioSource disabled entirely |

- Only **1 AudioSource per opponent** at distance — the motor source
- Servo and tire audio are inaudible beyond 5-10m; disable them early
- With 8+ opponents, this saves significant CPU on audio processing

---

## Compression and Loading Settings

### Audio Import Settings

| Clip Type | Load Type | Compression | Force Mono | Sample Rate |
|-----------|-----------|-------------|------------|-------------|
| Motor loops | Decompress on Load | ADPCM | Yes (3D) | 44100 |
| Servo loop | Decompress on Load | ADPCM | Yes (3D) | 22050 |
| Tire loops | Decompress on Load | ADPCM | Yes (3D) | 22050 |
| Impact clips | Decompress on Load | ADPCM | Yes (3D) | 44100 |
| ESC melody | Decompress on Load | PCM | Yes (3D) | 44100 |
| Wind loop | Decompress on Load | ADPCM | No (2D stereo) | 22050 |
| Music | Streaming | Vorbis (quality 70%) | No (stereo) | 44100 |
| Ambient | Streaming | Vorbis (quality 50%) | No (stereo) | 44100 |

### Key Rules

- **Force to Mono** on ALL 3D spatial sources — stereo 3D audio wastes memory and sounds wrong
- **ADPCM** for short/medium clips: 3.5:1 compression, low CPU decode cost, good for loops
- **PCM** (uncompressed) for very short clips that need perfect fidelity (ESC melody)
- **Vorbis Streaming** for music/ambient: saves memory, slight CPU cost for decode
- **Never stream short clips** — the overhead of streaming is not worth it for clips under 5 seconds
- **Decompress on Load** for gameplay-critical sounds to avoid decode latency

---

## Middleware Decision Guide

| Project Scale | Recommendation | Reasoning |
|--------------|----------------|-----------|
| **Indie / small team** | Unity native AudioMixer | Sufficient for RC racing needs, no licensing cost, simpler pipeline |
| **Dedicated audio designer** | FMOD | Better authoring tools, runtime parameter binding, event-based workflow |
| **AAA / large team** | Wwise | Full-featured but heavy integration cost, overkill for most RC games |

For this project (R8EO-X), Unity's native audio system with AudioMixer is the recommended starting point. The sound design is focused (one vehicle type, limited surface types) and does not require the advanced features of middleware.

If audio complexity grows (multiple vehicle classes, dynamic weather affecting sound, complex music systems), consider migrating to FMOD. The abstraction layer in the code examples above makes this migration straightforward — swap AudioSource calls for FMOD event triggers.

---

## Free CC0 / Creative Commons Sound Sources

| Sound | Source | Notes |
|-------|--------|-------|
| RC brushless motor | Freesound: "RC motor" by noiseloop | Record at multiple RPMs, loop points |
| Servo whine | Freesound: "servo" by plingativator | Short loop, pitch-shift to taste |
| Plastic/Lexan crash | Freesound: "plastic crash" by SwiftVector | Layer 3-5 variations |
| General impacts | Kenney Impact Sounds pack | CC0, game-ready, good variety |
| Bulk SFX library | Sonniss GDC Audio Bundle (annual) | Free yearly pack, thousands of sounds |
| Tire sounds | Freesound: search "small tire" or "RC tire" | Pitch-shift full-size samples up if needed |
| Wind/ambient | Freesound: search "outdoor wind loop" | Trim to seamless loops |

### Recording Your Own

If you have access to a real RC car:

- Record motor at 5 fixed throttle points (idle, 25%, 50%, 75%, 100%) for 10+ seconds each
- Record from 1 meter distance with a directional mic to minimize ambient noise
- Record impacts by dropping the car from increasing heights onto different surfaces
- Record servo by sweeping steering lock-to-lock at different speeds
- These recordings, properly looped, will sound more authentic than any library source

---

## Common Mistakes

1. **Using ICE/car engine sounds on a brushless motor** — Brushless motors whine, they do not rumble. Full-size engine samples sound completely wrong on an RC car regardless of pitch shifting.

2. **Enabling Doppler on the player's own car** — The camera tracks the player, so relative velocity is near zero. Doppler creates distracting pitch wobble with no realism benefit.

3. **Setting maxDistance too large on spatial sources** — An RC car is small and quiet. Motor audio should not be audible beyond 20m. Servo audio beyond 5m is unrealistic.

4. **Using stereo clips for 3D spatial sources** — Unity's 3D spatialization requires mono input. Stereo clips on 3D sources waste memory and produce incorrect spatialization.

5. **Streaming short clips** — Streaming adds decode latency and CPU overhead. Only stream long files (music, ambient beds). Short clips should use Decompress on Load.

6. **No impact sound variation** — Playing the same clip on every collision is immediately noticeable. Always use 3-5 variations with random selection.

7. **Linear volume sliders** — Human hearing is logarithmic. A linear 0-1 slider mapped directly to volume makes the bottom 50% of the slider nearly inaudible. Always convert to decibels.

8. **Single tire source for all surfaces** — Different surfaces sound dramatically different. At minimum, swap clips when the surface changes. Better: crossfade between surface-specific sources.

9. **Forgetting motor strain audio** — A motor at full throttle going uphill sounds different from full throttle at top speed. Load-dependent audio adds significant realism.

10. **PlayClipAtPoint for impacts** — Creates and destroys a GameObject every call, generating garbage and GC pressure. Use a pooled AudioSource system instead.

---

## Implementation Priority

### Phase 1 — Core (Ship-blocking)

Minimum viable audio that makes the game feel like an RC racing game:

- [ ] Motor RPM crossfade system (3 layers minimum)
- [ ] Servo whine tied to steering input
- [ ] Per-surface tire rolling audio (at least dirt + asphalt)
- [ ] Impact sounds with 3+ variations and impulse-based volume
- [ ] AudioMixer with Master/Music/SFX/UI groups
- [ ] Volume sliders with logarithmic conversion

### Phase 2 — Polish

Audio that distinguishes a good RC game from a generic one:

- [ ] 5-layer RPM crossfade with AnimationCurve tuning
- [ ] ESC startup melody on car spawn
- [ ] Wind rush with quadratic speed scaling
- [ ] Tire slip/scrub layer
- [ ] Reverb zones for tunnels/enclosed areas
- [ ] Opponent audio LOD system
- [ ] Mixer snapshots (Racing, Paused, Menu)
- [ ] Doppler on opponents (disabled on player)

### Phase 3 — Excellence

Audio details that RC enthusiasts will notice and appreciate:

- [ ] Motor strain layer (load-dependent)
- [ ] Sensorless motor cogging on startup
- [ ] Battery voltage warning beeps (gameplay integration)
- [ ] Adaptive music system (intensity tied to race position/proximity)
- [ ] Per-particle audio (gravel spray, dust puffs)
- [ ] Spectator camera mode with full Doppler
- [ ] Dynamic EQ based on camera distance to vehicle

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-audio-systems`** | General Unity audio architecture patterns |
| **`unity-physics-3d`** | Physics data (collision impulse, surface detection) that drives audio |
| **`unity-performance-optimization`** | AudioSource pooling, LOD strategies, memory budgets |
