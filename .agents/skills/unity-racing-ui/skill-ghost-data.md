# Ghost Data

> Part of the `unity-racing-ui` skill. See [SKILL.md](SKILL.md) for the overview.

## Ghost Data

### Recording Format

Record vehicle state at 30Hz (every other FixedUpdate at 50Hz):

```csharp
struct GhostFrame  // 28 bytes at 30Hz recording rate
{
    float time;           // 4 bytes
    Vector3 position;     // 12 bytes
    Quaternion rotation;  // 16 bytes (or compressed, see replay-ghost skill)
    // Total: 32 bytes with padding
}
```

### Storage Estimate

- 30 frames/sec x 120 sec (2-min lap) = 3,600 frames
- 32 bytes x 3,600 = ~115 KB uncompressed
- With DeflateStream: ~60-80 KB per ghost

### Playback Interpolation

- Use **Hermite spline interpolation** for position to avoid jerky movement between samples.
- Use `Quaternion.Slerp` for rotation between adjacent frames.
- See `unity-replay-ghost` skill for full implementation details.

---

