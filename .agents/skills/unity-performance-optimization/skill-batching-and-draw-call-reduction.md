# Batching and Draw Call Reduction

> Part of the `unity-performance-optimization` skill. See [SKILL.md](SKILL.md) for the overview.

## Batching and Draw Call Reduction

Every draw call has CPU overhead. Reducing draw calls is often the biggest performance win.

### Static Batching

Combines static meshes at build time into larger meshes per material.

- Mark non-moving objects as **Static** in Inspector (or just "Batching Static")
- Increases memory (stores combined mesh) but reduces draw calls dramatically
- Objects must share the same material

### Dynamic Batching

Automatically batches small moving meshes at runtime.

- Only works for meshes under 300 vertices (with specific attribute constraints)
- Not reliable — many conditions can break it
- Generally prefer GPU Instancing or SRP Batcher instead

### GPU Instancing

Renders multiple copies of the same mesh+material in one draw call.

```csharp
// Enable on material: Inspector > Enable GPU Instancing checkbox
// Or in code:
material.enableInstancing = true;
```

Works with: same mesh, same material, different transforms and per-instance properties.

```csharp
// Per-instance properties via MaterialPropertyBlock (does NOT break batching)
MaterialPropertyBlock props = new MaterialPropertyBlock();
props.SetColor("_Color", Random.ColorHSV());
renderer.SetPropertyBlock(props);
```

### GPU Resident Drawer (Unity 6)

> **Unity 6:** The GPU Resident Drawer automatically uses `BatchRendererGroup` to instance meshes on the GPU. Enable it in the URP Asset under **Rendering > GPU Resident Drawer**. When active, it supersedes manual Static Batching and GPU Instancing for most meshes -- the rendering pipeline handles instancing automatically. Objects must use compatible shaders (standard URP Lit/Unlit). For RC racing, this is a significant draw call reduction for repetitive track elements (barriers, cones, fences) with zero setup cost.

### SRP Batcher (URP/HDRP)

The SRP Batcher reduces CPU overhead of draw calls by caching shader variant states.

- Enabled by default in URP/HDRP
- Works with any mesh, as long as the shader is SRP Batcher compatible
- Check compatibility: select shader > Inspector shows "SRP Batcher: compatible"
- To make a shader compatible: use CBUFFER for all material properties

### Material Sharing

```csharp
// WRONG — creates a unique material instance, breaks batching
renderer.material.color = Color.red; // .material creates a clone

// CORRECT for read — doesn't create a clone
Color c = renderer.sharedMaterial.color;

// CORRECT for per-instance variation — use MaterialPropertyBlock
var block = new MaterialPropertyBlock();
block.SetColor("_BaseColor", Color.red);
renderer.SetPropertyBlock(block);
```

### Texture Atlases

Combine multiple small textures into one atlas. Objects using different sprites from the same atlas can batch together.

- Unity's Sprite Atlas (2D)
- Manual atlas for 3D: pack textures in DCC tool, UV-map accordingly
- TextMeshPro uses font atlas automatically

