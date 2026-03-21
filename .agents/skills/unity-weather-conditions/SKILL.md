---
name: unity-weather-conditions
description: Unity Weather & Track Conditions for RC Racing
---


# Unity Weather & Track Conditions for RC Racing

Use this skill when implementing dynamic weather systems and track condition modeling for an RC racing simulator. Covers weather state machines, global wetness, rain/dust/mud effects, wind physics, time of day, temperature models, track evolution, and wet surface shaders.

## When to Use

- Implementing a weather state machine (clear, rain, drying transitions)
- Adding wet-track physics (grip reduction, aquaplaning, mud drag)
- Creating rain, dust, or mud particle effects
- Integrating wind forces with vehicle physics and particles
- Building time-of-day or temperature systems that affect racing
- Modeling track evolution (rubber buildup, marbles, oil spills)

## When NOT to Use

- General particle system setup -- use `unity-particles-vfx`
- Shader authoring fundamentals -- use `unity-shaders` or `unity-3d-materials`
- Physics engine configuration (solver, timestep, layers) -- use `unity-physics-tuning`
- Networking weather state sync -- use `unity-networking`

---

## Global Wetness as Single Source of Truth

A single `_GlobalWetness` float (0 = bone dry, 1 = fully saturated) drives all weather-dependent systems.

```csharp
// Set globally -- all shaders and scripts can read it
Shader.SetGlobalFloat("_GlobalWetness", currentWetness);
```

### Consumers

| System | How It Uses Wetness |
|--------|-------------------|
| Tire physics | `grip *= (1f - wetness * surfaceConfig.wetGripReduction)` |
| Shaders | Increase smoothness, darken albedo, enable ripple normals |
| Dust particles | `emission = speed * (1f - wetness)` |
| Rain particles | `emission = wetness * maxRainParticles` |
| Audio | Blend in rain ambience, tire splash sounds |
| AI | Reduce target speed, increase following distance |

---

## Wet Material Shaders

### Shader Properties

```hlsl
// Add to existing surface shader or Shader Graph
float _GlobalWetness;   // Set via Shader.SetGlobalFloat
float _DirtAmount;      // Per-material

// Wetness effect:
// 1. Increase smoothness (wet surfaces are shinier)
float wetSmoothness = lerp(baseSmoothness, 0.95, _GlobalWetness);

// 2. Darken albedo (wet surfaces absorb more light)
float3 wetAlbedo = baseAlbedo * lerp(1.0, 0.6, _GlobalWetness);

// 3. Ripple normal map (animated water surface)
float2 rippleUV = worldPos.xz * 2.0 + _Time.y * float2(0.1, 0.15);
float3 rippleNormal = UnpackNormal(tex2D(_RippleNormalMap, rippleUV));
float3 finalNormal = lerp(baseNormal, rippleNormal, _GlobalWetness * 0.5);
```

---

## Integration with SurfaceConfig

Extend existing surface configuration with wetness data:

```csharp
[CreateAssetMenu(menuName = "Racing/Surface Config")]
public class SurfaceConfig : ScriptableObject
{
    [Header("Base Physics")]
    public float baseGrip;
    public float rollingResistance;

    [Header("Weather Modifiers")]
    [Range(0f, 1f)] public float wetGripReduction;     // How much grip drops when wet
    [Range(0f, 1f)] public float mudDragCoefficient;    // Drag in wet conditions
    public float dustEmissionRate;                       // Dust per unit speed
    public float waterAbsorptionRate;                    // How fast surface dries

    public float GetEffectiveGrip(float wetness)
    {
        return baseGrip * (1f - wetGripReduction * Mathf.Sqrt(wetness));
    }
}
```

---

## Implementation Priority

| Priority | Feature | Effort | Impact |
|----------|---------|--------|--------|
| **P1** | Global wetness + grip reduction | Low | High -- core gameplay mechanic |
| **P1** | Weather state machine (Clear/Rain) | Medium | High -- drives all weather systems |
| **P1** | Rain particle system | Low | High -- essential visual feedback |
| **P2** | Wet surface shaders | Medium | Medium -- visual polish |
| **P2** | Dust system with moisture suppression | Low | Medium -- immersion |
| **P2** | Wind physics on vehicle | Low | Medium -- adds depth to racing |
| **P3** | Time of day lighting | Medium | Medium -- atmosphere |
| **P3** | Tire temperature model | Medium | Medium -- simulation depth |
| **P3** | Track evolution (rubber/marbles) | High | Medium -- advanced racing mechanic |
| **P4** | Mud dynamics with dirt accumulation | Medium | Low -- specific to off-road tracks |
| **P4** | Battery cold performance | Low | Low -- niche realism |
| **P4** | Dynamic quality adjustment | Medium | Low -- optimization polish |

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-physics-3d`** | General physics foundations that grip/drag calculations build on |
| **`unity-physics-tuning`** | PhysX configuration for the vehicle that weather modifiers apply to |
| **`unity-particles-vfx`** | Particle system fundamentals for rain, dust, and mud effects |
| **`unity-3d-materials`** | Shader and material basics for wet surface rendering |


## Topic Pages

- [Weather State Machine](skill-weather-state-machine.md)
- [Rain System](skill-rain-system.md)
- [Performance Considerations](skill-performance-considerations.md)

