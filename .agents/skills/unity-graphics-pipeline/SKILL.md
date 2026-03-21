---
name: unity-graphics-pipeline
description: Unity Graphics Pipeline
---


# Unity Graphics Pipeline

Use this skill when configuring the Unity 6 URP rendering pipeline, optimizing draw calls, setting up lighting, or profiling GPU performance for a racing game.

---

## 1. URP (Not HDRP)

Unity 6 URP is the correct choice for a racing game targeting a wide hardware range. HDRP's extra fidelity costs 15-30% performance with minimal visible benefit at RC racing camera distances.

**Unity 6 URP now includes features that previously required HDRP:**
- Screen Space Reflections (SSR)
- Screen Space Ambient Occlusion (SSAO)
- Decal Projectors (forward and deferred)
- Adaptive Probe Volumes (APV)
- GPU Resident Drawer (GRD)
- Render Graph (mandatory)

**Pipeline Configuration:**
- Rendering Path: **Deferred** (better for many lights, lower SetPass calls)
- Depth Texture: Enabled (required for SSAO, motion blur)
- Opaque Texture: Enabled (required for refraction, distortion effects)
- HDR: Enabled (required for bloom, tonemapping)
- Anti-Aliasing: MSAA 2x or TAA (TAA preferred for temporal stability at speed)

**Why Deferred for Racing:**
- Track environments have many localized lights (brake lights, headlights, pit lane)
- Deferred decouples lighting cost from light count
- Forward+ is viable but Deferred handles worst-case light overlap better

---

## 6. LOD Strategy

Racing games have unique LOD requirements: the camera moves fast, objects enter and leave view rapidly, and pop-in is highly noticeable along the track edge.

**Racing-Specific LOD Thresholds:**
- LOD0 → LOD1: Screen size 0.15 (closer transition than default 0.3)
- LOD1 → LOD2: Screen size 0.08
- LOD2 → Cull: Screen size 0.03
- Crossfade width: 0.05-0.1 (dithered, NOT animated — animation is visible at speed)

**Why Tighter Thresholds:**
- At racing speeds, an object goes from "too far to see" to "right next to camera" in <1 second
- Default LOD thresholds cause visible pops at the edge of the track
- Tighter thresholds keep higher-detail meshes visible longer in the peripheral view

**Impostor LODs:**
- Use billboard impostors for distant trackside objects (trees, buildings, spectators)
- Generate 8-12 view angles for each impostor
- Switch to impostor at LOD2 (screen size < 0.08)
- Unity's `LODGroup` supports impostor LODs natively

**Vehicle LODs:**
- Player vehicle: always LOD0 (never reduce player car detail)
- AI opponents: LOD0 within 15m, LOD1 at 15-40m, LOD2 at 40m+
- Wheel detail is critical — separate LODGroup for wheels with tighter thresholds

---

## 10. Texture Streaming

Texture streaming loads mip levels on demand, reducing VRAM pressure on large tracks with many unique textures.

**Configuration:**
- Project Settings > Quality > Texture Streaming: **Enabled**
- Memory Budget: 60-70% of target GPU VRAM
  - 6GB card: 1.5GB texture budget (leaves 2.5GB for render targets, buffers, meshes)
  - 4GB card: 1.0GB texture budget
- Max Level Reduction: 2 (allows dropping up to 2 mip levels under pressure)

**Priority Settings:**
- Vehicle textures: Streaming Priority = 1 (highest — player stares at the car)
- Track surface: Streaming Priority = 0 (default — large area, distance LOD handles quality)
- Skybox / distant scenery: Streaming Priority = -1 (lowest — always distant)

**Mip Bias:**
- Global Mip Bias: -0.5 (slightly sharper than default, compensates for streaming latency)
- Per-material override for critical textures (vehicle livery, cockpit gauges)

**Racing-Specific Issues:**
- Fast camera movement can outrun mip streaming — increase `streamingMipmapMaxLevelReduction` to 1 for track textures
- Pre-warm textures at race start: load the track's texture set during the loading screen
- Avoid texture streaming on particle textures — they're small and always needed

---

## When to Use This Skill

- Setting up or configuring URP for a new project
- Diagnosing draw call or GPU performance issues
- Configuring lighting (APV, shadow cascades, probes)
- Writing custom render passes with Render Graph
- Setting up terrain rendering with Shader Graph
- Optimizing texture memory usage
- Profiling and hitting frame time targets

## When NOT to Use This Skill

- Writing gameplay code (use `unity-architecture-patterns`)
- Designing game feel effects (use `unity-game-feel` — it references this skill for Volume setup)
- Creating materials or shaders (use `unity-3d-materials` and `unity-shaders`)
- Setting up lighting artistically (use `unity-3d-lighting` for light placement and mood)
- Building terrain geometry (use `unity-terrain-track-creation`)
- General performance optimization beyond rendering (use `unity-performance-optimization`)

---

## Related Skills

| Skill | Relationship |
|-------|-------------|
| `unity-3d-lighting` | Light placement, mood, baking — this skill covers the pipeline that renders them |
| `unity-3d-materials` | Material authoring in Shader Graph — this skill covers how materials are batched and rendered |
| `unity-performance-optimization` | CPU-side optimization (GC, pooling) — this skill covers GPU-side optimization |
| `unity-shaders` | Custom HLSL and Shader Graph — this skill covers how shaders integrate with URP |
| `unity-game-feel` | Post-processing as game feel — this skill covers the Volume and pipeline configuration |
| `unity-terrain-track-creation` | Terrain layout and splines — this skill covers terrain rendering performance |


## Topic Pages

- [3. Adaptive Probe Volumes (APV)](skill-3-adaptive-probe-volumes-apv.md)
- [7. Shadow Cascades](skill-7-shadow-cascades.md)
- [9. Draw Call Budget](skill-9-draw-call-budget.md)
- [11. VFX Graph Instancing](skill-11-vfx-graph-instancing.md)
- [12. Profiling Workflow](skill-12-profiling-workflow.md)
- [2. GPU Resident Drawer (GRD)](skill-2-gpu-resident-drawer-grd.md)
- [8. Post-Processing Budget](skill-8-post-processing-budget.md)
- [5. Shader Graph Terrain](skill-5-shader-graph-terrain.md)

