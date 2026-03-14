# unity-rc-audio

Audio design skill for RC car racing games in Unity. Covers brushless motor sound synthesis, RPM crossfade systems, ESC sounds, servo audio, surface-dependent tire audio, impact sounds, environmental audio, and AudioMixer architecture.

## Files

| File | Role |
|------|------|
| `SKILL.md` | Full reference: motor audio, RPM crossfade, ESC/servo/tire/impact sounds, mixer setup, compression settings, implementation priority |

## Key Topics

- Brushless motor commutation frequency formula and RPM-to-pitch mapping
- 3-5 layer RPM crossfade system with AnimationCurve control
- Per-surface tire audio characteristics and slip-dependent scrub
- Lexan body impact sounds (not metallic) with AudioSource pooling
- AudioMixer group hierarchy and snapshot management
- Doppler configuration (disable on player, enable on opponents)
- Opponent audio LOD by distance
- Audio compression and import settings per clip type
- Free CC0 sound sources for RC audio

## Related Skills

- `unity-audio-systems` — General Unity audio architecture patterns
- `unity-physics-3d` — Physics data (collision impulse, surface detection) that drives audio
- `unity-performance-optimization` — AudioSource pooling, LOD strategies, memory budgets
