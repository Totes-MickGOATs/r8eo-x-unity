# Terrain Layers

> Part of the `unity-terrain-track-creation` skill. See [SKILL.md](SKILL.md) for the overview.

## Terrain Layers

Use exactly 4 layers to stay in a single GPU pass:

| Layer | Material | Tiling | Use |
|-------|----------|--------|-----|
| Base dirt | Dry brown earth | 8-12 m | Default ground, off-track areas |
| Packed racing line | Compacted dark soil | 6-10 m | Main track surface, highest grip |
| Gravel | Loose stones | 4-8 m | Track shoulders, drainage, runoff |
| Grass | Short turf | 6-10 m | Infield, spectator areas, margins |

### Tiling Elimination

Terrain textures tile visibly at RC camera distances. Solutions ranked by quality:

1. **MicroSplat Anti-Tiling** ($12 Asset Store) — procedural detail, macro variation, stochastic sampling in one package. Best cost/quality ratio.
2. **Macro variation map** — overlay a low-frequency color variation texture at 50-100m tiling to break repetition.
3. **Stochastic sampling** — custom shader that randomizes UV offsets per tile. Eliminates seams but costs ALU.
4. **Detail objects** — scatter small rocks, debris, tire marks as GPU-instanced meshes to visually break tiling.

---

