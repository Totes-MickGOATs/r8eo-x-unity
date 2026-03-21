# 12. Profiling Workflow

> Part of the `unity-graphics-pipeline` skill. See [SKILL.md](SKILL.md) for the overview.

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

