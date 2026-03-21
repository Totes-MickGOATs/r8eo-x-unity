# Doppler Effect

> Part of the `unity-rc-audio` skill. See [SKILL.md](SKILL.md) for the overview.

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

