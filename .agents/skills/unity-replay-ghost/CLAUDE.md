# unity-replay-ghost/

State-based replay recording and ghost playback system for RC racing.

## Files

| File | Role |
|------|------|
| `SKILL.md` | Full technical reference: GhostFrame struct, compression pipeline, playback interpolation, Cinemachine cameras, time scrubbing, async I/O, anti-cheat, networking |

## Key Topics

- **State recording** -- PhysX is non-deterministic; record transforms, not inputs
- **GhostFrame** -- 48-byte blittable struct at 50 Hz (432 KB/3 min raw)
- **Compression** -- Quantization, delta encoding, DeflateStream (432 KB to ~75 KB)
- **Storage** -- BinaryWriter `.r8ghost` files with int32 version header + JSON metadata sidecar
- **Playback** -- Kinematic ghost, Hermite position interpolation, Quaternion.Slerp
- **Ghost material** -- Transparent (30-50% alpha), no colliders, no Rigidbody
- **Cameras** -- Cinemachine chase/bumper/trackside/dolly/free/overhead with priority switching
- **Time scrubbing** -- Direct `_playbackTime` set, binary search frame lookup, no Physics.Simulate
- **Async I/O** -- `Awaitable.BackgroundThreadAsync` for save/load
- **Anti-cheat** -- Max position delta, duration validation, session nonce, SHA-256 checksum

## Related Skills

| Skill | Relationship |
|-------|-------------|
| `unity-camera-systems` | Cinemachine setup, virtual camera configuration, camera blending |
| `unity-save-load` | Persistence patterns, binary serialization, file I/O conventions |
| `unity-performance-optimization` | Profiling, GC-free patterns, async threading |
