# 9. Draw Call Budget

> Part of the `unity-graphics-pipeline` skill. See [SKILL.md](SKILL.md) for the overview.

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

