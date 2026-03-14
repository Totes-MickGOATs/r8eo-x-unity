# unity-replay-ghost/

Replay and ghost car system for racing — state recording, quantized storage, Hermite interpolation playback, Cinemachine replay cameras, time scrubbing, async I/O, anti-cheat, and ghost sharing.

## Files

| File | Contents |
|------|----------|
| `SKILL.md` | State vs input recording decision, frame structure (48 bytes at 50Hz), BinaryWriter + DeflateStream storage, smallest-three quaternion quantization, bounded position encoding, delta encoding, compression pipeline, ghost car playback with Hermite interpolation, ghost material setup, Cinemachine replay cameras, time scrubbing, async save/load, file versioning and migration, anti-cheat validation, Steam UGC and CDN ghost sharing, ghost car component architecture |

## Relevant Skills

- `unity-camera-systems` — Cinemachine setup, virtual camera configuration, camera blending
- `unity-save-load` — Serialization patterns, persistent data, file I/O
- `unity-performance-optimization` — Async patterns, memory management, compression
