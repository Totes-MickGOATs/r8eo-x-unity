# unity-game-feel/

Camera juice, controller haptics, screen effects, input curves, landing impact, and audio feedback techniques for RC racing.

## Files

| File | Contents |
|------|----------|
| `SKILL.md` | 12 game feel techniques with code examples, RC-specific values, and priority table |

## Techniques Covered

1. Speed-Based FOV (Cinemachine 3.x)
2. Camera Shake on Impact (CinemachineImpulseSource)
3. Controller Haptics (Gamepad dual motors)
4. Screen Effects at Speed (URP Volume overrides)
5. Suspension Compression Visuals (WheelCollider pose)
6. Tire Smoke Scaling (slip-driven particles)
7. Speed Lines / Radial Blur (particle or shader)
8. Audio as Game Feel (pitch, wind, hit-stop)
9. Landing Impact (multi-layered compound event)
10. Time Manipulation (slow-mo on airtime)
11. Input Response Curves (steering/throttle/brake)
12. Minimap Juice (pulse, flash, velocity rotation)

## Related Skills

| Skill | Relationship |
|-------|-------------|
| `unity-camera-systems` | Cinemachine fundamentals this skill builds on |
| `unity-rc-audio` | Detailed motor audio synthesis |
| `unity-input-system` | Input System setup before curves are applied |
| `unity-particles-vfx` | Particle System fundamentals for smoke/dust/speed lines |
| `unity-graphics-pipeline` | URP post-processing and Volume configuration |
