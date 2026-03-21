# Heightmap Workflows

> Part of the `unity-terrain-track-creation` skill. See [SKILL.md](SKILL.md) for the overview.

## Heightmap Workflows

### In-Editor Sculpting

Best for quick iteration. Use Unity's terrain tools:
- **Raise/Lower:** Broad terrain shaping
- **Smooth:** Remove harsh edges on jump transitions
- **Stamp:** Repeatable jump profiles — create a stamp brush from a jump cross-section
- **Set Height:** Flatten areas for pit lane, start/finish straight

### External Tools

For larger or more realistic terrains:
- **World Machine** — node-based erosion and terrain generation, export as 16-bit RAW
- **Gaea** — similar to World Machine with GPU-accelerated erosion
- Import workflow: export heightmap as 16-bit RAW at terrain resolution, import via Terrain Settings > Import Raw

### Stamp Tool for Jumps

1. Create a small heightmap (65x65) representing the jump cross-section
2. Import as terrain brush stamp
3. Paint jumps at consistent height by stamping along the track spline
4. Smooth transitions between stamped areas and surrounding terrain

---

