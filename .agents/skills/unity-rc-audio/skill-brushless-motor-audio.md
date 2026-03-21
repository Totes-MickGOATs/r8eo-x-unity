# Brushless Motor Audio

> Part of the `unity-rc-audio` skill. See [SKILL.md](SKILL.md) for the overview.

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

