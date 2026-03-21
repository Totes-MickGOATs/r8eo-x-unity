# Terrain Setup for RC Scale

> Part of the `unity-terrain-track-creation` skill. See [SKILL.md](SKILL.md) for the overview.

## Terrain Setup for RC Scale

RC tracks are small-scale environments. A 200x200m terrain covers a full-size RC facility.

| Setting | Recommended Value | Rationale |
|---------|-------------------|-----------|
| Terrain size | 200 x 200 m | Full RC facility with paddock area |
| Heightmap resolution | 1025 x 1025 | Power-of-two-plus-one requirement; sufficient detail at RC scale |
| Splatmap resolution | 512-1024 | Higher = sharper surface type transitions; 512 adequate for 200m |
| Max terrain layers | 4 | Single-pass GPU rendering; exceeding 4 adds a second pass |
| Height range | 10-20 m | RC tracks are relatively flat; 20m allows hills and elevated sections |

### Heightmap Rules

- Resolution MUST be `2^n + 1` (e.g., 257, 513, 1025). Non-conforming values cause import errors.
- Use 16-bit RAW or PNG for heightmap import/export. 8-bit loses vertical precision.
- The stamp brush is ideal for creating jump faces — paint repeatable shapes at consistent heights.

---

