---
name: unity-terrain-track-creation
description: Unity Terrain & Track Creation for RC Racing
---


# Unity Terrain & Track Creation for RC Racing

Use this skill when building RC racing tracks and terrain in Unity. Covers terrain configuration, terrain layers, spline-based track layout, jump geometry, surface detection, physics materials, ProBuilder hard surfaces, track-side objects, lighting, and performance tuning.

## When to Use

- Setting up Unity Terrain for a 1/10-scale RC environment
- Designing track layout with Unity Splines or EasyRoads3D
- Building jumps, tabletops, rhythm sections, or banked corners
- Configuring terrain layers and eliminating tiling artifacts
- Implementing runtime surface detection via splatmap sampling
- Placing track-side objects (barriers, fencing, signage) with LOD
- Optimizing terrain rendering performance

## When NOT to Use

- General 3D environment construction beyond terrain and tracks -- use `unity-3d-world-building`
- Material and shader authoring fundamentals -- use `unity-3d-materials`
- Physics engine configuration (solver, timestep, collision matrix) -- use `unity-physics-tuning`
- Vehicle physics or tire friction models -- use `unity-physics-tuning`

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

## Hard Surface Construction

Use **ProBuilder** (built into Unity) for non-terrain track elements:

- Ramps and jump faces with precise angles
- Bridges and elevated sections
- Pit lane surfaces
- Retaining walls and barriers

ProBuilder meshes get their own colliders and PhysicsMaterials, bypassing terrain surface detection. Tag them with a `SurfaceZone` trigger for the runtime surface system.

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

## Common Mistakes

| Mistake | Consequence | Fix |
|---------|-------------|-----|
| Heightmap not `2^n + 1` | Import fails or silent data corruption | Always use 257, 513, 1025, 2049 |
| More than 4 terrain layers | Second GPU pass, performance halved | Limit to 4 layers; blend wisely |
| WheelCollider + PhysicsMaterial | PhysicsMaterial silently ignored | Set friction via WheelCollider API at runtime |
| Splatmap sampled every frame | 2-5ms CPU spike per vehicle | Cache and sample at 0.1-0.2s intervals |
| Terrain Draw Instanced OFF | Massive draw call count | Always enable in Terrain Settings |
| Jump faces too steep (>45 deg) | Vehicles clip through or bounce erratically | Keep launch face under 40 degrees |
| No LOD on track-side objects | Draw calls explode with 100+ barriers | LOD Group on every repeated object |
| Lightmap UV overlap on terrain | Black splotches in baked lighting | Terrain auto-generates lightmap UVs; don't manually UV |

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-3d-world-building`** | General 3D environment construction and scene composition |
| **`unity-3d-materials`** | Material setup, shaders, and texture workflows |
| **`unity-physics-3d`** | Physics materials, colliders, and runtime friction systems |


## Topic Pages

- [Track Layout with Unity Splines](skill-track-layout-with-unity-splines.md)
- [Surface Detection at Runtime](skill-surface-detection-at-runtime.md)
- [Heightmap Workflows](skill-heightmap-workflows.md)
- [Terrain Layers](skill-terrain-layers.md)
- [EasyRoads3D Pro Integration](skill-easyroads3d-pro-integration.md)
- [Track-Side Objects](skill-track-side-objects.md)
- [Terrain Setup for RC Scale](skill-terrain-setup-for-rc-scale.md)
- [Lighting for Outdoor RC Tracks](skill-lighting-for-outdoor-rc-tracks.md)

