---
name: unity-particles-vfx
description: Unity Particles and VFX
---


# Unity Particles and VFX

Use this skill when creating particle effects, configuring the Shuriken Particle System, building VFX Graph effects, or optimizing visual effects performance.

## Particle System (Shuriken) Overview

Unity's CPU-based particle system. Each ParticleSystem component has a set of modules
that control emission, movement, appearance, and lifetime.

### Module Overview

| Module | Purpose | Key Properties |
|--------|---------|----------------|
| **Main** | Lifetime, speed, size, gravity, simulation space | Duration, Start Lifetime, Start Speed, Start Size, Gravity Modifier |
| **Emission** | How many particles spawn | Rate Over Time, Rate Over Distance, Bursts |
| **Shape** | Where particles spawn from | Shape type, radius, angle, emit from |
| **Velocity Over Lifetime** | Modify velocity after spawn | Linear/Orbital/Radial velocity curves |
| **Limit Velocity Over Lifetime** | Cap speed, apply drag | Speed, Dampen |
| **Inherit Velocity** | Inherit emitter movement | Mode (Initial/Current), Multiplier |
| **Force Over Lifetime** | Apply forces (wind, gravity) | XYZ force curves |
| **Color Over Lifetime** | Fade in/out, color shift | Gradient |
| **Size Over Lifetime** | Grow/shrink | Curve |
| **Rotation Over Lifetime** | Spin particles | Angular velocity |
| **Noise** | Turbulence, organic motion | Strength, Frequency, Scroll Speed |
| **Collision** | Bounce off surfaces | World/Planes, Bounce, Lifetime Loss |
| **Trigger** | Detect entry/exit from colliders | Inside/Outside/Enter/Exit callbacks |
| **Sub-Emitters** | Spawn child particles | On Birth/Death/Collision/Trigger |
| **Texture Sheet Animation** | Flipbook animation | Grid mode, rows, columns, frame curve |
| **Renderer** | How particles look | Billboard, Stretched, Mesh, Trail, Material |

## Shape Module

Controls where particles originate:

| Shape | Emit From | Use Case |
|-------|-----------|----------|
| Sphere | Volume / Surface / Edge | Explosions, magic orbs |
| Hemisphere | Half sphere | Ground bursts |
| Cone | Base / Volume / Shell | Torch fire, spotlight dust |
| Box | Volume / Surface / Edge | Snow, rain (wide area) |
| Mesh | Vertices / Edges / Triangles | Emitting from a character mesh |
| Circle | Edge / Ring | Ground ring effects |
| Edge | Along a line | Waterfall edge |
| Sprite | Sprite shape | 2D effects |

```
// Fire torch shape:
Shape: Cone
  Angle: 15
  Radius: 0.3
  Emit from: Base
  Randomize Direction: 0.1
```

## Emission Module

### Rate Modes

```
Rate Over Time: 50         // 50 particles per second (continuous)
Rate Over Distance: 10     // 10 particles per unit moved (tire marks, footprints)
```

### Bursts

Emit a batch at a specific time:

```
Bursts:
  Time: 0.0,  Count: 30,  Cycles: 1,  Interval: 0,  Probability: 1.0
  Time: 0.5,  Count: 15,  Cycles: 3,  Interval: 0.2, Probability: 0.8
```

## Texture Sheet Animation

Animate particles using a sprite sheet:

```
Texture Sheet Animation:
  Mode: Grid
  Tiles: 4 x 4              // 16 frames
  Animation: Whole Sheet
  Frame Over Time: Curve (0 to 1 over lifetime)
  Start Frame: Random Between 0 and 3   // Variety
  Cycles: 1
```

Useful for: cartoon smoke puffs, explosions, fire, stylized effects.

## VFX Graph vs Particle System

| Criteria | Particle System (Shuriken) | VFX Graph |
|----------|---------------------------|-----------|
| Execution | CPU | GPU (compute shaders) |
| Particle Count | Thousands | Millions |
| Render Pipeline | Built-in, URP, HDRP | URP, HDRP only |
| Authoring | Inspector modules | Node graph |
| Collision | Built-in world collision | Simplified (planes, spheres) |
| SubEmitters | Full support | Event-based (different model) |
| Physics Interaction | OnParticleCollision messages | No direct physics callbacks |
| Script Control | Extensive API | Exposed properties + events |

**Use Particle System when:** You need CPU-side collision callbacks, script-driven emission, small-to-medium particle counts, or Built-in Render Pipeline support.

**Use VFX Graph when:** You need massive particle counts (100K+), GPU-driven simulation, complex behaviors via visual programming, or HDRP integration.



## Topic Pages

- [Sub-Emitters](skill-sub-emitters.md)
- [Renderer Module](skill-renderer-module.md)
- [Common Effect Recipes](skill-common-effect-recipes.md)

