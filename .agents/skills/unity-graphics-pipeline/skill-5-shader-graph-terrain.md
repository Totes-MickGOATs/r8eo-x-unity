# 5. Shader Graph Terrain

> Part of the `unity-graphics-pipeline` skill. See [SKILL.md](SKILL.md) for the overview.

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

