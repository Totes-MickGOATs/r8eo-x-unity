# Unity Terrain

> Part of the `unity-3d-world-building` skill. See [SKILL.md](SKILL.md) for the overview.

## Unity Terrain

### Creating Terrain

```
GameObject > 3D Object > Terrain
  - Creates Terrain GameObject with Terrain + Terrain Collider components
  - Default size: 1000x1000 meters (adjust in Terrain Settings)
```

### Heightmap

```
Terrain Settings:
  - Terrain Width/Length: world size in meters
  - Terrain Height: max elevation
  - Heightmap Resolution: 513, 1025, 2049, or 4097
    (power of 2 + 1 — higher = more detail, more memory)

Sculpting tools:
  - Raise/Lower: left-click raise, shift+click lower
  - Set Height: flatten to specific elevation
  - Smooth: average out bumps
  - Stamp: apply height pattern from texture

Import raw heightmap:
  - Terrain Settings > Import Raw
  - Format: RAW 16-bit, byte order: Windows (little-endian)
  - Resolution must match heightmap pixel dimensions
```

### Terrain Layers (Texture Painting)

```
Each terrain layer defines a surface material:
  - Albedo + Normal + Mask (metallic/AO/height/smoothness packed)
  - Tiling Size: how large the texture appears on terrain
  - Tiling Offset: shift texture origin

Adding layers:
  1. Select Terrain > Paint Texture tool
  2. Edit Terrain Layers > Create Layer
  3. Assign textures, set tiling
  4. Paint in Scene view (brush size, opacity, target strength)

Limit: 4 layers per pass without performance penalty.
Each additional set of 4 = another draw pass.
```

### Trees and Details

```
Trees:
  - Terrain > Paint Trees > Edit Trees > Add Tree
  - Assign tree prefab (must have LOD Group for performance)
  - Billboard at distance (auto-generated)
  - Parameters: density, height variation, width variation, color variation

Details (grass, flowers, rocks):
  - Terrain > Paint Details > Edit Details
  - Detail Mesh: 3D mesh instances on terrain
  - Detail Texture: billboarded grass/flower quads
  - Draw Distance: how far details render (50-200m typical)
```

### Terrain Performance

| Setting | Location | Effect |
|---------|----------|--------|
| Pixel Error | Terrain Settings | LOD aggressiveness. Higher = lower quality, better performance. Default 5, try 10-20 for mobile |
| Base Map Distance | Terrain Settings | Distance where terrain switches to low-res composite. 500-1000m for open worlds |
| Draw Instanced | Terrain Settings | GPU instancing for terrain rendering. Always enable |
| Detail Distance | Terrain Settings | Grass/detail render range |
| Detail Density | Terrain Settings | Grass density multiplier |
| Tree Distance | Terrain Settings | Tree render distance |
| Billboard Start | Terrain Settings | Distance where trees become billboards |
| Terrain Holes | Terrain component | Enable to carve holes (caves, tunnels) — slight overhead |

### Multi-Terrain (Large Worlds)

```
For worlds larger than one terrain:
  - Create NxN grid of Terrain tiles
  - Each tile: own heightmap, splatmap, trees
  - Use Terrain Groups for LOD coherence
  - Tools: Unity Terrain Tools package, World Creator, Gaia
```

## ProBuilder

Quick mesh prototyping directly in Unity. Install from Package Manager.

### Core Workflow

```
1. Tools > ProBuilder > ProBuilder Window
2. Create shape: New Shape (cube, cylinder, stairs, arch, etc.)
3. Edit in modes:
   - Object mode: move/rotate/scale whole mesh
   - Vertex mode: move individual vertices
   - Edge mode: select/move edges, insert edge loops
   - Face mode: select/extrude/inset faces
4. UV editing: Auto UV or Manual UV in ProBuilder UV Editor
5. Material assignment: per-face material slots
```

### Common Operations

```
Extrude: select face → Shift+drag or Extrude button
  - Creates new geometry extending from selected face

Inset: select face → Inset
  - Creates smaller face inside, with connecting faces

Boolean: merge two ProBuilder meshes (union, subtract, intersect)
  - Experimental, may need cleanup

Merge: combine multiple ProBuilder objects into one mesh

Detach: separate selected faces into a new object

Subdivide: split selected faces into smaller faces
```

### From Prototype to Final Art

```
1. Block out level with ProBuilder (gray boxes)
2. Playtest and iterate on layout
3. Export to FBX: ProBuilder > Export Mesh
4. Replace with final art meshes (maintain collider shapes)
5. Or: keep ProBuilder meshes with proper materials for simple geometry
```

