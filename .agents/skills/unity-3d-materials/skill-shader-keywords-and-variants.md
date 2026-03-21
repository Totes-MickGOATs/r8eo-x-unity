# Shader Keywords and Variants

> Part of the `unity-3d-materials` skill. See [SKILL.md](SKILL.md) for the overview.

## Shader Keywords and Variants

### multi_compile vs shader_feature

```hlsl
// multi_compile — ALL combinations compiled, always available
#pragma multi_compile _ _RAIN_ON

// shader_feature — only combinations used by materials in build are compiled
// Unused variants are stripped — smaller build, but runtime enabling may fail
#pragma shader_feature _DETAIL_ON

// Rule of thumb:
// - shader_feature for material-driven toggles (artist sets in Inspector)
// - multi_compile for runtime/global toggles (code enables/disables)
```

### Enabling Keywords at Runtime

```csharp
// Global (affects all shaders)
Shader.EnableKeyword("_RAIN_ON");
Shader.DisableKeyword("_RAIN_ON");

// Per-material
material.EnableKeyword("_DETAIL_ON");
material.DisableKeyword("_DETAIL_ON");
```

### Variant Stripping

```
Too many keywords = combinatorial explosion of shader variants.
  - Each keyword doubles variant count
  - 10 keywords = 1024 variants per shader (most unused)

Solutions:
  - Use shader_feature (strips unused at build time)
  - Implement IPreprocessShaders to strip manually
  - Keep keywords under 10 per shader
  - Use local keywords (#pragma shader_feature_local) to avoid global limit
```

## Common Material Patterns

### Terrain Splatmapping

```
4 terrain layers blended via control map (RGBA = weights):
  - Each layer: albedo + normal + mask (metallic/AO/smoothness)
  - Unity Terrain handles this automatically with Terrain Layers
  - Custom: sample control map, lerp between 4 textures by weight
```

### Vertex Color Blending

```csharp
// Paint vertex colors in Polybrush or DCC tool
// Shader reads vertex color to blend textures
// R = snow amount, G = moss, B = dirt (example)

// In Shader Graph:
// Vertex Color node → Split → use channels as lerp factors
```

### Triplanar Mapping

```
Projects textures from 3 axes (X, Y, Z) based on surface normal.
  - Eliminates UV stretching on cliffs/overhangs
  - 3x texture samples — use for terrain/rocks, not everything
  - In Shader Graph: Triplanar node (built-in)
```

### Dissolve Effect

```
Shader Graph approach:
  1. Sample noise texture (Simplex, Voronoi)
  2. Compare noise value to _DissolveAmount threshold
  3. Clip pixels below threshold (Alpha Clip)
  4. Add emission at dissolve edge (Step + narrow band)
  5. Animate _DissolveAmount from 0 to 1 over time
```

## Performance

### Batching Rules

```
Static Batching:
  - Combines static meshes sharing the same material
  - Mark as Batching Static in Inspector
  - One draw call per unique material

Dynamic Batching:
  - Automatic for small meshes (< 300 vertices with position+normal+UV)
  - Same material required
  - Usually not worth relying on — SRP Batcher is better

SRP Batcher (URP/HDRP):
  - Groups draw calls by shader (not material)
  - Materials using the same shader batch even with different properties
  - Enabled in URP/HDRP Asset settings
  - Shader must be SRP Batcher compatible (use CBUFFER macros)
```

### GPU Instancing

```csharp
// Enable on material: material.enableInstancing = true
// Or check "Enable GPU Instancing" in material Inspector
// Combined with MaterialPropertyBlock for per-instance variation
// All instances must share the same mesh and material
```

### Texture Atlasing

```
Combine multiple textures into one atlas:
  - Reduces material count → more batching
  - UV mapping points to atlas sub-regions
  - Tools: Texture Packer, Unity Sprite Atlas (2D), custom atlas builder
```

### LOD Material Swaps

```
LOD Group with simpler materials at distance:
  - LOD0: full PBR (normal, detail, emission)
  - LOD1: simplified (no detail map, lower-res textures)
  - LOD2: unlit or vertex-lit single color
  - Reduces shader complexity and texture bandwidth at distance
```

