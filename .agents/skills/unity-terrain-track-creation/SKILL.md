# Unity Terrain & Track Creation for RC Racing

> Lazy-loaded reference for building RC racing tracks and terrain in Unity.

---

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

## Track Layout with Unity Splines

Use the **Unity Splines** package (`com.unity.splines`) for track centerline definition.

### Setup

```
// Package: com.unity.splines (add via Package Manager)
// Create: GameObject > Spline > Draw Splines Tool
```

### Spline Configuration

- **Closed loop:** Enable `Closed` on the SplineContainer for circuit tracks.
- **Knot tangent modes:** Use `Auto Smooth` for flowing curves, `Bezier` for precise control of entry/exit angles.
- **Knot count:** 20-40 knots for a typical 100-150m RC circuit. Fewer knots = smoother curves.
- **Width:** Store track width as SplineData (float) attached to knots — allows variable-width sections.

### RC Track Dimensions

| Parameter | Range | Notes |
|-----------|-------|-------|
| Track width | 3-6 m | 3m minimum for 1/10 scale; 5-6m for multi-class racing |
| Total loop length | 80-200 m | 80m for technical tracks, 200m for speed circuits |
| Jump height | 0.3-1.2 m | 0.3m = small kicker, 1.2m = large tabletop |
| Banking angle | 5-20 degrees | 5 for gentle sweepers, 15-20 for high-speed berms |
| Straight length | 10-30 m | Longer straights for speed; short for technical |
| Minimum corner radius | 3-5 m | Tighter = more technical, slower |

---

## Jump Types

| Jump | Description | Construction |
|------|-------------|--------------|
| **Tabletop** | Flat top between launch and landing faces | Trapezoidal cross-section, 2-4m long top |
| **Double** | Launch face, gap, landing face | Two separate ramps with airspace between |
| **Triple** | Three consecutive jumps in rhythm | Evenly spaced at vehicle-speed intervals |
| **Rhythm section** | Series of small bumps/rollers | 0.2-0.4m height, 1-2m spacing |
| **Step-on / Step-off** | Elevated plateau with ramp up and down | Flat top 3-5m long, gradual entry/exit |

### Jump Face Angles

- Launch face: 25-40 degrees from horizontal
- Landing face: 15-30 degrees (shallower than launch to absorb impact)
- Tabletop top surface: flat (0 degrees) or slight crown (2-3 degrees for drainage visual)

---

## Racing Line Theory

### Corner Types

| Type | Technique | When to Use |
|------|-----------|-------------|
| **Late apex** | Wide entry, clip apex past midpoint, early power | Default for corners leading onto straights |
| **Early apex** | Turn in early, apex before midpoint | Defensive line, or before a braking zone |
| **Decreasing radius** | Tightening corner, apex near exit | Requires progressive braking through corner |
| **Chicane** | Two linked opposite-direction turns | Straight-line the middle, sacrifice one entry |

### Line Width Utilization

- Entry: use full track width, approach from outside edge
- Apex: clip the inside edge (within 0.5m of boundary)
- Exit: let the car drift to the outside edge under acceleration

---

## EasyRoads3D Pro Integration

**EasyRoads3D Pro** ($45 Asset Store) provides spline-based track mesh generation with terrain deformation.

### Key Features for RC Tracks

- Spline-drawn road mesh with automatic terrain conform
- Banking/superelevation per control point
- Terrain deformation: road surface cuts into or builds up terrain automatically
- Intersection system for pit lane entries
- UV-mapped mesh for custom surface materials

### Workflow

1. Draw spline path for track centerline
2. Set road type (width, material, banking per point)
3. Terrain auto-deforms to match road elevation
4. Apply custom materials for racing surface
5. Add curbing, rumble strips as secondary road types

---

## Surface Detection at Runtime

### Terrain Splatmap Sampling

```csharp
// Cache alphamaps — do NOT call every frame
// Sample at 0.1-0.2 second intervals
private float[,,] _cachedAlphamaps;
private float _nextSampleTime;

void UpdateSurfaceType(Vector3 worldPos)
{
    if (Time.time < _nextSampleTime) return;
    _nextSampleTime = Time.time + 0.15f;

    TerrainData td = terrain.terrainData;
    Vector3 terrainPos = worldPos - terrain.transform.position;
    int mapX = Mathf.RoundToInt(terrainPos.x / td.size.x * td.alphamapWidth);
    int mapZ = Mathf.RoundToInt(terrainPos.z / td.size.z * td.alphamapHeight);
    _cachedAlphamaps = td.GetAlphamaps(mapX, mapZ, 1, 1);

    // Find dominant layer
    int dominant = 0;
    float maxWeight = 0f;
    for (int i = 0; i < td.alphamapLayers; i++)
    {
        if (_cachedAlphamaps[0, 0, i] > maxWeight)
        {
            maxWeight = _cachedAlphamaps[0, 0, i];
            dominant = i;
        }
    }
    CurrentSurface = (SurfaceType)dominant;
}
```

### Trigger Zone Overlay

For hard-surface sections (bridges, ramps built with ProBuilder), use trigger colliders with a `SurfaceZone` component. These override terrain splatmap detection when the vehicle is inside the trigger.

### Physics Materials Per Surface

| Surface | Static Friction | Dynamic Friction | Friction Combine |
|---------|----------------|-----------------|-----------------|
| Packed dirt | 0.8 | 0.6 | Average |
| Loose gravel | 0.5 | 0.3 | Minimum |
| Grass | 0.6 | 0.4 | Average |
| Wet packed dirt | 0.5 | 0.35 | Average |
| Concrete/tarmac | 1.0 | 0.8 | Average |

> **WARNING:** Unity WheelCollider ignores the PhysicMaterial on its own collider. Surface friction must be applied via `WheelCollider.sidewaysFriction` and `forwardFriction` stiffness multipliers at runtime based on detected surface type.

---

## Hard Surface Construction

Use **ProBuilder** (built into Unity) for non-terrain track elements:

- Ramps and jump faces with precise angles
- Bridges and elevated sections
- Pit lane surfaces
- Retaining walls and barriers

ProBuilder meshes get their own colliders and PhysicMaterials, bypassing terrain surface detection. Tag them with a `SurfaceZone` trigger for the runtime surface system.

---

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

## Terrain Performance Settings

| Setting | Value | Impact |
|---------|-------|--------|
| Draw Instanced | ON | GPU instancing for terrain patches; significant perf gain |
| Pixel Error | 3 | LOD aggressiveness; lower = more triangles, higher = more popping |
| Base Map Distance | 100-150 m | Distance at which terrain switches to low-res composite texture |
| Detail Distance | 40-60 m | Grass/detail object render distance |
| Tree Distance | 80-120 m | Tree billboard distance (if applicable) |
| Detail Density | 0.4-0.6 | Balance between visual density and draw call count |
| Heightmap Pixel Error | 5-8 | Mesh LOD tolerance; 5 for quality, 8 for performance |

---

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

## Common Mistakes

| Mistake | Consequence | Fix |
|---------|-------------|-----|
| Heightmap not `2^n + 1` | Import fails or silent data corruption | Always use 257, 513, 1025, 2049 |
| More than 4 terrain layers | Second GPU pass, performance halved | Limit to 4 layers; blend wisely |
| WheelCollider + PhysicMaterial | PhysicMaterial silently ignored | Set friction via WheelCollider API at runtime |
| Splatmap sampled every frame | 2-5ms CPU spike per vehicle | Cache and sample at 0.1-0.2s intervals |
| Terrain Draw Instanced OFF | Massive draw call count | Always enable in Terrain Settings |
| Jump faces too steep (>45 deg) | Vehicles clip through or bounce erratically | Keep launch face under 40 degrees |
| No LOD on track-side objects | Draw calls explode with 100+ barriers | LOD Group on every repeated object |
| Lightmap UV overlap on terrain | Black splotches in baked lighting | Terrain auto-generates lightmap UVs; don't manually UV |
