# Tire / Surface Audio

> Part of the `unity-rc-audio` skill. See [SKILL.md](SKILL.md) for the overview.

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

