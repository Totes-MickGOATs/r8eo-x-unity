---
name: unity-rc-audio
description: Unity RC Audio
---


# Unity RC Audio

Use this skill when implementing audio for an RC car racing game. RC cars sound nothing like full-size vehicles -- brushless motors produce a high-pitched whine, not engine rumble. Covers motor audio synthesis, RPM crossfade, ESC/servo/tire/impact sounds, AudioMixer architecture, Doppler configuration, opponent audio LOD, and compression settings.

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

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-audio-systems`** | General Unity audio architecture patterns |
| **`unity-physics-3d`** | Physics data (collision impulse, surface detection) that drives audio |
| **`unity-performance-optimization`** | AudioSource pooling, LOD strategies, memory budgets |


## Topic Pages

- [Brushless Motor Audio](skill-brushless-motor-audio.md)
- [Tire / Surface Audio](skill-tire-surface-audio.md)
- [Opponent LOD Audio](skill-opponent-lod-audio.md)
- [Implementation Priority](skill-implementation-priority.md)
- [Servo Audio](skill-servo-audio.md)
- [Doppler Effect](skill-doppler-effect.md)

