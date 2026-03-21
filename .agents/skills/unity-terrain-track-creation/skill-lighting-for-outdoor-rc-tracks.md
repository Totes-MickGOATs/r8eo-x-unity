# Lighting for Outdoor RC Tracks

> Part of the `unity-terrain-track-creation` skill. See [SKILL.md](SKILL.md) for the overview.

## Lighting for Outdoor RC Tracks

### Light Setup

- **Directional light:** Mixed mode for baked shadows on static objects, real-time on vehicles.
- **Baked GI:** Lightmap the terrain and static track-side objects. Use 10-20 texels/unit for terrain.
- **Adaptive Probe Volumes (APV):** Replace Light Probe Groups in URP/HDRP. Auto-place probes in a volume covering the track. Provides indirect lighting on dynamic objects (vehicles).
- **Reflection probes:** Place at key positions for wet surface reflections. Box projection for enclosed areas (pit lane with canopy).

### Post-Processing

| Effect | Setting | Purpose |
|--------|---------|---------|
| SSAO | Radius 0.3, Intensity 1.5 | Ground contact shadows, depth to terrain features |
| Bloom | Threshold 1.2, Intensity 0.3 | Sun glare, headlight glow for night racing |
| Color grading | Warm lift, cool shadows | Outdoor daylight atmosphere |
| Vignette | Intensity 0.2-0.3 | Focus attention on track center |

---

