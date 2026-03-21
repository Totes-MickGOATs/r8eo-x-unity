---
name: unity-3d-lighting
description: Unity 3D Lighting
---


# Unity 3D Lighting

Use this skill when configuring lights, shadows, global illumination, or baked lighting in Unity across URP and HDRP render pipelines.

## Global Illumination

### Baked GI

```
Lighting window > Mixed Lighting:
  - Baked Global Illumination: enabled
  - Lighting Mode: Baked Indirect (recommended)

Bounced light is computed during bake and stored in lightmaps.
Dynamic objects receive bounced light via Light Probes.
```

### Enlighten Realtime GI (Legacy)

```
Deprecated in Unity 2024+. Do not use for new projects.
Was: runtime-updated indirect lighting that responded to moving lights.
Replace with: baked GI + Light Probes, or HDRP with Screen Space GI.
```

## Environment Lighting

### Skybox

```
Lighting window > Environment:
  - Source: Skybox
  - Assign a Skybox material (Procedural, Cubemap, Panoramic, or 6-sided)
  - Intensity Multiplier: scales ambient contribution

// Runtime skybox change
RenderSettings.skybox = newSkyboxMaterial;
DynamicGI.UpdateEnvironment();  // Re-evaluate ambient lighting
```

### Gradient / Color

```
Simpler than skybox, no texture required:
  - Gradient: separate sky/equator/ground colors
  - Color: single flat ambient color
```

## Emissive Materials

Materials with emission can contribute to baked GI:

```csharp
// Enable emission on material
material.EnableKeyword("_EMISSION");
material.SetColor("_EmissionColor", Color.yellow * 5f);

// Mark object as static for GI contribution
// In Inspector: Static > Contribute GI
// Emission will bake into nearby lightmaps
```

Set **Emission > Global Illumination** to **Baked** on the material for it to affect lightmaps.

## Performance Tips

1. **Limit realtime shadow-casting lights** — 1 directional + 2-3 additional max
2. **Bake everything you can** — baked lighting is free at runtime
3. **Use Light Probe Proxy Volumes** for large dynamic objects instead of single probe sampling
4. **Shadow distance** is the single biggest shadow performance lever — keep it as low as acceptable
5. **Reduce shadow resolution** on less important lights
6. **Disable shadows on small/distant lights** — `light.shadows = LightShadows.None`
7. **Forward+** (URP 2022.2+) if you need many lights without deferred rendering
8. **Lightmap compression** halves VRAM usage with minimal visual impact
9. **Avoid overlapping realtime Reflection Probes** — each one re-renders the scene
10. **Use LOD Groups** to reduce triangle count for shadow map rendering at distance


## Topic Pages

- [Light Types](skill-light-types.md)
- [Shadow Settings](skill-shadow-settings.md)
- [Post-Processing (Volume System)](skill-post-processing-volume-system.md)

