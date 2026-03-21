# Post-Processing (Volume System)

> Part of the `unity-3d-lighting` skill. See [SKILL.md](SKILL.md) for the overview.

## Post-Processing (Volume System)

URP and HDRP use the Volume framework for post-processing.

### Setup

```
1. Create empty GameObject
2. Add Volume component
3. Set Mode: Global (affects entire scene) or Local (with collider trigger)
4. Create new Volume Profile (or assign existing)
5. Add Override: choose effect
```

### Common Effects

```csharp
// Bloom — glow on bright areas
Bloom bloom;
volume.profile.TryGet(out bloom);
bloom.threshold.value = 1.0f;
bloom.intensity.value = 0.5f;
bloom.scatter.value = 0.7f;

// Color Grading (Tonemapping)
ColorAdjustments colorAdj;
volume.profile.TryGet(out colorAdj);
colorAdj.postExposure.value = 0.5f;
colorAdj.contrast.value = 10f;
colorAdj.saturation.value = 10f;

// SSAO (Screen Space Ambient Occlusion) — URP via Renderer Feature
// HDRP: built-in override in Volume

// SSR (Screen Space Reflections) — HDRP only via Volume override
```

### Volume Blending

```
Multiple volumes can overlap and blend:
  - Weight: 0-1, controls influence
  - Priority: higher priority overrides lower
  - Blend Distance (Local volumes): gradual transition zone

Use Case: enter a cave → local volume darkens exposure, adds fog
```

## Lighting Recipes

### Outdoor Daytime

```
1. Single Directional Light (Mixed, Baked Indirect)
   - Rotation: (50, -30, 0) for classic sun angle
   - Color: warm white (color temperature ~6500K)
   - Intensity: 1.0-2.0

2. Skybox: procedural or HDRI
3. Environment Lighting: Skybox source
4. Baked GI with Light Probes for dynamic objects
5. Cascaded shadows: 4 cascades, 150m distance
```

### Indoor Scene

```
1. Mixed lighting for main lights (ceiling fixtures)
2. Baked GI for light bouncing off walls
3. Light Probes: dense grid, especially near doors/windows
4. Reflection Probes: one per room, box projection
5. Area lights (baked) for soft window light
6. Post-processing: slight bloom, SSAO for depth
```

### Night Scene

```
1. Directional light at very low intensity (moonlight, blue tint)
2. Point/spot lights as key lighting (warm, high contrast)
3. Emissive materials for neon signs, windows
4. Volumetric fog (HDRP) or particle fog (URP)
5. Post-processing: high bloom, vignette, low exposure
```

