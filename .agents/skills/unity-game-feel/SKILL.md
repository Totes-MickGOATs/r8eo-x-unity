---
name: unity-game-feel
description: Unity Game Feel
---


# Unity Game Feel

Use this skill when adding camera juice, controller haptics, screen effects, audio feedback, or visual polish to make the RC racing game feel responsive and satisfying.

---

## Implementation Priority

Order by RC-specific impact. Implement top-down.

| Priority | Technique | Category | RC Impact |
|----------|-----------|----------|-----------|
| 1 | Suspension Compression Visuals | Visual | Defines RC identity — visible suspension IS the genre |
| 2 | Input Response Curves | Control | Precision feel at sticks, prevents twitchy oversteer |
| 3 | Landing Impact (Multi-Layered) | Compound | Peak excitement moments on jumps |
| 4 | Camera Shake on Impact | Camera | Communicates collision severity instantly |
| 5 | Speed-Based FOV | Camera | Subconscious speed perception |
| 6 | Audio as Game Feel | Audio | 40-50% of speed communication at RC scale |
| 7 | Controller Haptics | Tactile | Surface and engine feedback through hands |
| 8 | Tire Smoke Scaling | Visual | Drift satisfaction and slip feedback |
| 9 | Screen Effects at Speed | Visual | Cinematic speed polish |
| 10 | Speed Lines / Radial Blur | Visual | Additional speed communication layer |
| 11 | Time Manipulation | Temporal | Dramatic airtime moments |
| 12 | Minimap Juice | UI | Spatial awareness polish |

---

## When to Use This Skill

- Adding "juice" or "feel" to an existing gameplay mechanic
- Players report the game feels "flat" or "unresponsive" despite correct physics
- Implementing camera follow systems for a racing game
- Connecting input devices (gamepads) to tactile feedback
- Creating speed perception without changing actual vehicle physics
- Building landing/collision/drift feedback systems
- Tuning post-processing for a racing game

## When NOT to Use This Skill

- Fixing actual physics bugs (use `unity-physics-3d` or `unity-physics-tuning`)
- Designing the vehicle physics model (use `unity-physics-tuning`)
- Building the core input system (use `unity-input-system`)
- Creating the audio engine synthesis (use `unity-rc-audio` for detailed motor audio)
- Implementing replay or ghost systems (use `unity-replay-ghost`)
- Optimizing performance of effects (use `unity-performance-optimization`)

---

## Related Skills

| Skill | Relationship |
|-------|-------------|
| `unity-camera-systems` | Cinemachine setup, virtual cameras, freelook — this skill adds juice ON TOP of camera systems |
| `unity-rc-audio` | Detailed brushless motor synthesis, AudioMixer routing — this skill covers audio AS game feel |
| `unity-input-system` | Input System configuration, action maps — this skill applies curves AFTER input processing |
| `unity-particles-vfx` | Particle System and VFX Graph fundamentals — this skill covers specific RC racing effects |
| `unity-physics-tuning` | PhysX configuration for RC — game feel builds on top of correct physics |
| `unity-performance-optimization` | When effects need optimization — budget your juice |
| `unity-graphics-pipeline` | URP post-processing setup — this skill uses Volumes configured by the pipeline |


## Topic Pages

- [1. Speed-Based FOV](skill-1-speed-based-fov.md)

