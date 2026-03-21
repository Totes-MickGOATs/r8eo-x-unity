# Servo Audio

> Part of the `unity-rc-audio` skill. See [SKILL.md](SKILL.md) for the overview.

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

