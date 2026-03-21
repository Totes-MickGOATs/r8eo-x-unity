# unity-graphics-pipeline/

Unity 6 URP rendering pipeline configuration, GPU optimization, lighting setup, and profiling for racing games.

## Files

| File | Contents |
|------|----------|
| `SKILL.md` | 12 pipeline topics with configuration values, code examples, and profiling workflow |

## Topics Covered

1. URP (Not HDRP) — pipeline selection and configuration
2. GPU Resident Drawer — indirect draw calls, replaces manual instancing
3. Adaptive Probe Volumes — automatic probe placement, sky occlusion
4. Render Graph — mandatory Unity 6 render pass API
5. Shader Graph Terrain — native terrain material authoring, stochastic sampling
6. LOD Strategy — racing-specific thresholds and impostors
7. Shadow Cascades — 3-cascade setup for RC track scale
8. Post-Processing Budget — per-effect GPU time allocation
9. Draw Call Budget — targets and SRP Batcher + GRD synergy
10. Texture Streaming — VRAM budgets and priority settings
11. VFX Graph Instancing — per-wheel effects with GPU batching
12. Profiling Workflow — structured GPU debugging process

## Related Skills

| Skill | Relationship |
|-------|-------------|
| `unity-3d-lighting` | Light placement and baking details |
| `unity-3d-materials` | Material authoring in Shader Graph |
| `unity-performance-optimization` | CPU-side optimization (GC, pooling, batching) |
| `unity-shaders` | Custom HLSL and Shader Graph authoring |
| `unity-game-feel` | Post-processing as game feel (uses Volumes configured here) |
| `skill-3-adaptive-probe-volumes-apv.md` | 3. Adaptive Probe Volumes (APV) |
| `skill-7-shadow-cascades.md` | 7. Shadow Cascades |
| `skill-9-draw-call-budget.md` | 9. Draw Call Budget |
| `skill-11-vfx-graph-instancing.md` | 11. VFX Graph Instancing |
| `skill-12-profiling-workflow.md` | 12. Profiling Workflow |
| `skill-2-gpu-resident-drawer-grd.md` | 2. GPU Resident Drawer (GRD) |
| `skill-8-post-processing-budget.md` | 8. Post-Processing Budget |
| `skill-5-shader-graph-terrain.md` | 5. Shader Graph Terrain |
