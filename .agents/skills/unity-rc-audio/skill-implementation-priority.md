# Implementation Priority

> Part of the `unity-rc-audio` skill. See [SKILL.md](SKILL.md) for the overview.

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

