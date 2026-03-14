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

## 2. GPU Resident Drawer (GRD)

Unity 6's GPU Resident Drawer replaces manual GPU instancing, static batching, and most SRP Batcher concerns. It keeps mesh data GPU-resident and uses indirect draw calls.

**How to Enable:**
1. URP Asset > Rendering > GPU Resident Drawer: **Enabled**
2. Project Settings > Player > Static Batching: **Disabled** (conflicts with GRD)
3. Keep `BatchRendererGroup` shader variants enabled in URP Asset

**What GRD Supersedes:**
- Manual GPU Instancing (`Graphics.DrawMeshInstanced`) — GRD does this automatically
- Static Batching — GRD handles static geometry more efficiently
- MaterialPropertyBlock instancing workarounds — GRD batches per-instance properties natively

**What GRD Does NOT Replace:**
- SRP Batcher — GRD and SRP Batcher are **complementary**, enable both
- Dynamic Batching — still useful for small meshes (<300 verts) on low-end hardware
- Custom compute draw calls — GRD handles standard MeshRenderer only

**Racing-Specific Notes:**
- Track-side objects (barriers, signs, cones) benefit enormously — hundreds of identical meshes
- Vehicle meshes are typically unique — GRD helps less here, but doesn't hurt
- Vegetation (grass, bushes) should use GRD instead of manual instancing

**Verification:**
- Frame Debugger: look for `BatchRendererGroup.DrawIndirect` calls
- Rendering Debugger: GPU Resident Drawer stats panel shows batch counts

---

## 3. Adaptive Probe Volumes (APV)

APV replaces the legacy Light Probe Group workflow with automatic probe placement and sky occlusion support. Essential for day/night cycles or dynamic weather on the track.

**Setup:**
1. Project Settings > Graphics > Lighting: Enable **Adaptive Probe Volumes**
2. Add an `Adaptive Probe Volumes` component to the scene
3. Configure baking settings:
   - Min Subdivision Level: 1-2m for track surfaces (captures road-to-grass transitions)
   - Max Subdivision Level: 4-8m for open sky areas
   - Dilation: Enabled (fills gaps near geometry boundaries)

**Sky Occlusion:**
- Enable for day/night lighting without rebaking
- APV stores sky visibility per probe
- Runtime sky color changes propagate through probes automatically
- Essential for time-of-day racing (dawn, noon, dusk, night)

**Racing-Specific Configuration:**
- Dense probes along the track surface (1m subdivision) — captures shadows from bridges, tunnels, tree canopy
- Sparse probes in open sky areas (4-8m) — no detail needed above the track
- Place probe volumes per track section (tunnel volume, open area volume)
- Streaming: Enable for large tracks — loads probe data per camera position

**Migration from Light Probe Groups:**
- Remove all `Light Probe Group` components
- APV auto-generates equivalent coverage during bake
- Bake time may increase by 20-40% but runtime quality is higher

---

## 4. Render Graph

Render Graph is **mandatory** in Unity 6 URP. All custom render passes must use the new `RecordRenderGraph` API instead of the legacy `Execute` method.

**Key Changes from Legacy:**
- `ScriptableRenderPass.Execute()` is deprecated — use `RecordRenderGraph()`
- Resources are declared as handles (`TextureHandle`, `BufferHandle`)
- The graph automatically manages resource lifetimes and barriers
- Passes that don't produce used outputs are automatically culled

**Writing a Custom Pass:**

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class CustomSpeedBlurPass : ScriptableRenderPass
{
    private class PassData
    {
        public TextureHandle source;
        public TextureHandle destination;
        public float blurStrength;
    }

    public override void RecordRenderGraph(
        RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(
            "Speed Blur", out var passData))
        {
            passData.source = resourceData.activeColorTexture;
            passData.blurStrength = 0.5f;

            var desc = renderGraph.GetTextureDesc(passData.source);
            passData.destination = renderGraph.CreateTexture(desc);

            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.SetRenderAttachment(passData.destination, 0);

            builder.SetRenderFunc(
                (PassData data, RasterGraphContext ctx) =>
                {
                    // Blit with blur material
                });
        }
    }
}
```

**Debugging:**
- Window > Analysis > **Render Graph Viewer** — visualizes the full pass graph
- Shows resource lifetimes, pass dependencies, and culled passes
- Use this FIRST when debugging rendering issues, before Frame Debugger

---

## 5. Shader Graph Terrain

Unity 6.3 introduces native Shader Graph support for terrain materials, eliminating the need for third-party terrain shaders like MicroSplat for basic terrain authoring.

**What's New:**
- Terrain Lit material can be authored in Shader Graph
- Per-layer blending is exposed as Shader Graph nodes
- Stochastic sampling available as a node (anti-tiling without MicroSplat)
- Height-based blending with configurable transitions

**Stochastic Sampling:**
- Eliminates visible tiling on large terrain surfaces
- Unity 6.3 provides a built-in stochastic sampling node
- Apply per terrain layer — dirt and gravel benefit most, asphalt less so
- Performance cost: ~10% more texture samples per layer, negligible on modern GPUs

**Racing-Specific Setup:**
- Layer 0: Base dirt/mud (stochastic sampling ON)
- Layer 1: Track surface — asphalt or packed dirt (stochastic optional)
- Layer 2: Grass/vegetation border (stochastic ON)
- Layer 3: Special surfaces — gravel traps, puddle zones (stochastic ON)
- Use terrain layer splatmaps to mark surface types for physics (tire grip lookup)

**Performance Note:**
- Limit to 4 terrain layers per tile (one splatmap) — each additional splatmap doubles terrain draw calls
- Use layer blending sharpness of 0.8-1.0 for clean track edge transitions

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

## 7. Shadow Cascades

Shadow quality is critical for racing — shadows communicate road surface detail, time of day, and object proximity. Misconfigured shadows cause shimmer, peter-panning, or wasted resolution.

**Recommended Configuration (3 Cascades):**

| Cascade | Distance | Resolution | Purpose |
|---------|----------|------------|---------|
| 0 | 5m | 2048 | Vehicle + immediate surroundings |
| 1 | 20m | 1024 | Track surface ahead |
| 2 | 50m | 512 | Distant scenery, buildings |

**Why 3, Not 4:**
- 4th cascade adds minimal quality for 25% more shadow map memory
- RC tracks are smaller than full-size racing tracks — 50m covers most of the visible track
- Saved budget goes to higher resolution in cascade 0

**Shadow Settings:**
- Shadow Resolution: 2048 (cascade 0), matching URP Asset settings
- Depth Bias: 0.5-1.0 (prevents shadow acne on flat track surfaces)
- Normal Bias: 0.3-0.5 (prevents peter-panning on small RC-scale objects)
- Soft Shadows: Enabled with PCF 5x5 (visually important for ground contact shadows)
- Screen Space Shadows: Enable if GPU budget allows (~0.3ms) — reduces shadow aliasing

**Racing-Specific Issues:**
- Shadow cascade transitions visible on flat track surfaces — enable cascade blending (10% border)
- Fast camera movement causes shadow shimmer — enable Stable Fit shadow projection
- Small RC-scale objects (antenna, wing mounts) may lose shadows — increase shadow resolution or exclude from shadow casting

---

## 8. Post-Processing Budget

Allocate post-processing effects within a strict per-frame GPU budget. Racing games need consistent frame times — a 2ms spike causes visible stutter at 60fps.

**Effect Budgets:**

| Effect | Budget | Notes |
|--------|--------|-------|
| Color Grading (LUT) | < 0.1ms | Essentially free, always enable |
| Bloom | 0.8-1.5ms | Use 4-5 iterations, threshold 0.9-1.1 |
| SSAO (half-res) | 0.5ms | Half-resolution + bilateral blur |
| Motion Blur | 0.5-1.0ms | Per-object preferred over camera motion blur |
| Tonemapping | < 0.1ms | ACES or Neutral, negligible cost |
| Chromatic Aberration | < 0.1ms | Speed-driven only (see game-feel skill) |
| Vignette | < 0.05ms | Trivial cost, always available |
| Film Grain | < 0.1ms | Optional, adds cinematic grit |
| **Total target** | **< 3.0ms** | At 60fps, post leaves 13.6ms for everything else |

**Effects to AVOID in Racing:**
- Depth of Field — obscures track obstacles, confuses player about focus
- Screen Space Reflections at full-res — too expensive for racing frame budgets
- Panini Projection — distorts track geometry, disorienting at speed

**Volume Strategy:**
- Global Volume: base color grading + tonemapping + vignette (always on)
- Speed Volume: bloom intensity + chromatic aberration + motion blur (weight driven by speed)
- Impact Volume: brief intensity spike on collision (weight driven by impact force, auto-decay)

---

## 9. Draw Call Budget

Target draw call counts for a racing game at 60fps on mid-range hardware.

**Targets:**

| Metric | Budget | How to Measure |
|--------|--------|----------------|
| Visible Renderers | < 1000 | Rendering Debugger > Visible Objects |
| SetPass Calls | < 500 | Frame Debugger > SetPass count |
| GPU Render Time | < 5ms | Profiler > GPU Usage |
| Triangles | < 2M visible | Stats window or Profiler |

**How SRP Batcher and GRD Work Together:**
- SRP Batcher reduces SetPass calls by batching materials with the same shader
- GRD reduces draw calls by batching identical meshes with indirect draws
- They are **complementary** — enable both in the URP Asset
- SRP Batcher handles material-heavy scenes, GRD handles geometry-heavy scenes
- A racing track is both: many materials (track, barriers, signs) AND many instances (cones, fences, grass)

**Common Draw Call Sinks in Racing:**
- Trackside objects with unique materials — consolidate into texture atlases
- Particle systems — each system is a draw call, use sub-emitters instead of separate systems
- UI elements — use UI Toolkit (batches better than Canvas) or atlased sprites
- Terrain — each splatmap layer adds a draw call per terrain tile

**Optimization Workflow:**
1. Open Frame Debugger — sort by SetPass calls
2. Identify top 10 most expensive batches
3. Merge materials (texture atlas), enable GRD, add LODs
4. Re-profile — verify batch count dropped

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

## 11. VFX Graph Instancing

Unity 6 supports instanced VFX Graph effects, allowing a single VFX asset to drive multiple emission points (e.g., one per wheel) with automatic GPU batching.

**Setup for Per-Wheel Effects:**

```csharp
using UnityEngine;
using UnityEngine.VFX;

public class WheelVFXInstancing : MonoBehaviour
{
    [SerializeField] private VisualEffect[] wheelEffects; // 4 components, same asset

    public void UpdateWheelEffect(int wheelIndex, float slipMagnitude, Vector3 position)
    {
        var vfx = wheelEffects[wheelIndex];

        vfx.SetFloat("SlipMagnitude", slipMagnitude);
        vfx.SetVector3("EmitPosition", position);

        // VFX Graph handles instancing — these 4 components batch into
        // a single GPU draw call automatically in Unity 6
    }
}
```

**Architecture:**
- Create ONE VFX Graph asset for tire effects (smoke, dirt spray, water splash)
- Add 4 `VisualEffect` components — one per wheel
- All 4 reference the same VFX asset
- Unity 6 auto-batches identical VFX assets into a single indirect draw
- Per-instance properties (position, slip, surface type) are set via `SetFloat`/`SetVector3`

**Exposed Properties in VFX Graph:**
- `SlipMagnitude` (float): drives emission rate and particle size
- `EmitPosition` (Vector3): world-space wheel contact point
- `SurfaceType` (int): selects particle color/texture (0=asphalt, 1=dirt, 2=gravel, 3=grass)
- `VehicleVelocity` (Vector3): inherited velocity for particles

**Performance Benefit:**
- Legacy approach: 4 Particle Systems = 4 draw calls + 4 CPU simulation threads
- VFX Graph instanced: 1 draw call + GPU simulation (shared compute dispatch)
- Savings: ~0.5ms CPU per vehicle at 4 wheels with active effects

---

## 12. Profiling Workflow

A structured approach to finding and fixing GPU performance issues in a racing game. Always profile builds, never the Editor.

**Step-by-Step:**

1. **Build first** — Editor overhead adds 20-40% to frame time, masking real bottlenecks
2. **Profiler > Rendering module** — identify CPU vs GPU bound
   - CPU bound: draw call submission, culling, animation
   - GPU bound: shader complexity, overdraw, fill rate
3. **GPU Usage Profiler** — drill into per-pass timing
   - Shadow pass > 2ms? Reduce cascade count or resolution
   - Opaque pass > 3ms? Too many triangles or expensive shaders
   - Transparent pass > 1ms? Particle overdraw
   - Post-processing > 3ms? Reduce effect count or resolution
4. **Render Graph Viewer** — visualize pass dependencies
   - Look for redundant passes that aren't culled
   - Identify resource lifetime issues (textures held too long)
5. **Frame Debugger** — step through individual draw calls
   - Sort by SetPass to find batching failures
   - Identify materials that break SRP Batcher compatibility

**Racing-Specific Profiling Targets:**

| Scenario | Frame Budget (60fps) | Critical Path |
|----------|---------------------|---------------|
| Straight (fast) | 16.6ms | Motion blur, LOD transitions, streaming |
| Corner (many objects) | 16.6ms | Draw calls, shadow resolution, particles |
| Jump (airborne) | 16.6ms | Sky rendering, distant LODs, VFX burst |
| Pile-up (4+ vehicles) | 16.6ms | Per-vehicle VFX, shadow casters, physics |

**Common Racing Game GPU Issues:**
- Shadow cascade transition shimmer on flat track — enable Stable Fit + cascade blending
- Particle overdraw in dust clouds — use half-res particle rendering
- Terrain shader overdraw with 8+ layers — limit to 4 layers per tile
- Transparent sorting issues with trackside fences — use alpha cutout, not alpha blend

**Tools:**
- Unity Profiler (built-in) — CPU and GPU timing, memory
- Rendering Debugger (URP) — overdraw, LOD visualization, light counts
- Render Graph Viewer — pass dependencies and resource lifetimes
- Frame Debugger — individual draw call inspection
- RenderDoc / Nsight (external) — shader-level profiling, pixel history

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
