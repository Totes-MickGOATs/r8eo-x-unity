# Track-Side Objects

> Part of the `unity-terrain-track-creation` skill. See [SKILL.md](SKILL.md) for the overview.

## Track-Side Objects

### Modular Kit Approach

Build a reusable kit of track-side objects:

| Category | Objects | LOD Strategy |
|----------|---------|--------------|
| Barriers | Jersey barriers, tire stacks, hay bales | LOD0 + LOD1 + billboard |
| Fencing | Chain-link panels, catch fence, rope line | LOD0 + LOD1 (remove mesh detail) |
| Signage | Corner markers, sponsor boards, lap counter | LOD0 + billboard |
| Furniture | Pit tables, driver stands, canopy tents | LOD0 + LOD1 |

### Instancing

- Enable **GPU Instancing** on all track-side object materials.
- Use `Graphics.DrawMeshInstanced` for repeated objects (tire stacks, barrier segments).
- Group static objects under a parent with `StaticBatchingUtility.Combine()` at load time.

---

