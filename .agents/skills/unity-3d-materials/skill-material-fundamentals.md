# Material Fundamentals

> Part of the `unity-3d-materials` skill. See [SKILL.md](SKILL.md) for the overview.

## Material Fundamentals

A Material is a Shader + a set of property values. The shader defines what properties exist; the material stores the values.

### Creating Materials

```
Assets > Create > Material
  - Assign a shader (default: URP/Lit or HDRP/Lit)
  - Set texture maps, colors, numeric values
  - Drag onto objects or assign via Mesh Renderer
```

### Material Instances vs Shared Materials

```csharp
// Shared material — editing affects ALL objects using this material
// Use renderer.sharedMaterial for read-only access or intentional global changes
Material shared = renderer.sharedMaterial;

// Instance material — Unity auto-clones when you access .material
// Creates a unique copy for THIS renderer only
// WARNING: creates a new material instance (memory leak if not managed)
Material instance = renderer.material;
instance.color = Color.red;  // Only affects this object

// Cleanup instanced materials
void OnDestroy()
{
    if (renderer.material != null)
        Destroy(renderer.material);
}
```

### Assigning Materials in Code

```csharp
// Single material
renderer.material = newMaterial;

// Multiple materials (sub-meshes)
Material[] mats = renderer.materials;  // Returns copies!
mats[1] = newMaterial;
renderer.materials = mats;

// Shared (no copy)
renderer.sharedMaterial = newMaterial;
```

## URP Lit Shader

The standard PBR shader for Universal Render Pipeline.

### Texture Maps

| Map | Property | Purpose |
|-----|----------|---------|
| Base Map | `_BaseMap` | Albedo/diffuse color + alpha |
| Normal Map | `_BumpMap` | Surface detail via tangent-space normals |
| Metallic Map | `_MetallicGlossMap` | R = metallic, A = smoothness |
| Occlusion Map | `_OcclusionMap` | Baked ambient occlusion (R channel) |
| Emission Map | `_EmissionMap` | Self-illumination color and intensity |
| Detail Map | `_DetailMap` | Tiling detail overlay (albedo + normal) |

### Setting Properties in Code

```csharp
Material mat = renderer.material;

// Color and texture
mat.SetColor("_BaseColor", new Color(0.8f, 0.2f, 0.2f, 1f));
mat.SetTexture("_BaseMap", albedoTexture);

// Metallic-smoothness
mat.SetFloat("_Metallic", 0.9f);
mat.SetFloat("_Smoothness", 0.7f);

// Normal map
mat.SetTexture("_BumpMap", normalTexture);
mat.SetFloat("_BumpScale", 1.5f);  // Normal intensity

// Emission
mat.EnableKeyword("_EMISSION");
mat.SetColor("_EmissionColor", Color.cyan * 3f);
mat.SetTexture("_EmissionMap", emissionTexture);

// Tiling and offset
mat.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
mat.SetTextureOffset("_BaseMap", new Vector2(0.5f, 0f));
```

### Surface Options

```
Surface Type:
  - Opaque — default, no transparency
  - Transparent — alpha blending (see-through)

Render Face:
  - Front — default, backface culling
  - Back — render inside only
  - Both — double-sided (foliage, cloth)

Alpha Clipping:
  - Enable for cutout transparency (leaves, fences)
  - Threshold: alpha value below which pixels are discarded
```

