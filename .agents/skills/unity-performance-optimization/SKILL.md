---
name: unity-performance-optimization
description: Unity Performance Optimization
---


# Unity Performance Optimization

Use this skill when profiling frame rate issues, reducing draw calls, optimizing memory usage, or addressing CPU/GPU bottlenecks in Unity.

## Profiling Workflow

1. **Identify the bottleneck** — Is it CPU, GPU, or memory? Check Profiler.
2. **Measure** — Get a baseline number (ms per frame, draw calls, memory).
3. **Optimize** — Apply the appropriate technique.
4. **Verify** — Re-measure. Did it actually improve? By how much?
5. **Repeat** — Go back to step 1.

**Frame budgets:**

| Target FPS | Budget per Frame |
|------------|-----------------|
| 30 fps | 33.3 ms |
| 60 fps | 16.6 ms |
| 90 fps (VR) | 11.1 ms |
| 120 fps | 8.3 ms |

## LOD Groups

Reduce polygon count for distant objects:

```
LOD 0: Full detail    — 0-20% screen height → 5000 tris
LOD 1: Medium detail  — 20-50% → 2000 tris
LOD 2: Low detail     — 50-80% → 500 tris
Culled                 — 80-100% → not rendered
```

Setup: Add LODGroup component, assign mesh renderers to each LOD level.

**Cross-fade:** Enable for smooth transitions (uses dithering). Small GPU cost but looks much better.

**LOD Bias:** Project Settings > Quality > LOD Bias. Higher = use higher LOD longer. 1.0 = default, 2.0 = double quality distance.

## Occlusion Culling

Skip rendering objects hidden behind other objects.

1. Mark occluders (walls, buildings) as **Occluder Static**
2. Mark occludees (everything) as **Occludee Static**
3. Bake: Window > Rendering > Occlusion Culling > Bake
4. Tune cell size for accuracy vs bake time

**When to use:** Dense indoor environments, urban areas with buildings. Less useful for open worlds with few occluders.

**Frustum culling** is free and automatic — only objects in the camera's view are rendered. Occlusion culling is additional.

## Texture Optimization

| Platform | Recommended Format | Notes |
|----------|--------------------|-------|
| Desktop | BC7 (quality) / DXT5 (performance) | BC7 is higher quality at same size |
| Mobile (Android) | ASTC 6x6 (quality) / ETC2 (compatibility) | ASTC preferred for modern devices |
| Mobile (iOS) | ASTC 6x6 | Standard for iOS |
| WebGL | DXT5 / ETC2 | DXT for desktop browsers, ETC2 for mobile |

### Texture Settings

- **Max Size:** Match actual usage. A texture shown as 256px on screen does not need 4096px.
- **Mipmaps:** Enable for 3D objects (reduces aliasing and improves cache performance). Disable for UI sprites.
- **Read/Write Enabled:** OFF unless you need CPU access. Doubles memory.
- **Generate Mipmaps > Streaming:** Enable for large worlds. Loads mip levels on demand.

## Mesh Optimization

- **Polygon budgets:** Set per-project. e.g., hero character 10K-50K tris, environment props 100-2K tris
- **Mesh Compression:** Low/Medium/High in import settings. Reduces disk/download size.
- **Read/Write Enabled:** OFF unless modifying mesh at runtime. Halves mesh memory.
- **Optimize Mesh Data:** Enable in Player Settings. Strips unused vertex attributes.
- **Vertex Compression:** Enable in Player Settings. Uses half-precision where possible.

## Quick Wins Checklist

- [ ] Disable Raycast Target on non-interactive UI elements
- [ ] Split canvases by update frequency
- [ ] Mark non-moving objects as Static
- [ ] Enable GPU Instancing on shared materials
- [ ] Use NonAlloc physics queries
- [ ] Cache GetComponent results
- [ ] Set appropriate texture Max Size
- [ ] Disable Read/Write on textures and meshes you don't modify at runtime
- [ ] Configure collision matrix to skip impossible layer pairs
- [ ] Enable Incremental GC
- [ ] Use object pooling for frequently spawned/despawned objects
- [ ] Disable mipmaps on UI textures
- [ ] Profile before optimizing — measure, don't guess


## Topic Pages

- [Batching and Draw Call Reduction](skill-batching-and-draw-call-reduction.md)
- [Object Pooling](skill-object-pooling.md)
- [Shader Optimization](skill-shader-optimization.md)

