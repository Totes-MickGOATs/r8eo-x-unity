# Shader Graph

> Part of the `unity-3d-materials` skill. See [SKILL.md](SKILL.md) for the overview.

## Shader Graph

Visual shader authoring for URP and HDRP.

### Creating a Shader Graph

```
Assets > Create > Shader Graph > URP > Lit Shader Graph
  - Opens visual node editor
  - Connect nodes from left to right
  - Output to Master Stack (Vertex and Fragment stages)
```

### Master Stack Outputs

**Fragment stage (pixel-level):**

| Output | Type | Purpose |
|--------|------|---------|
| Base Color | Color (RGB) | Surface albedo |
| Normal (Tangent Space) | Vector3 | Per-pixel normal perturbation |
| Metallic | Float | 0 = dielectric, 1 = metal |
| Smoothness | Float | 0 = rough, 1 = mirror |
| Emission | Color (RGB) | Self-illumination |
| Ambient Occlusion | Float | Cavity darkening |
| Alpha | Float | Transparency |

**Vertex stage (vertex-level):**

| Output | Type | Purpose |
|--------|------|---------|
| Position | Vector3 | Vertex displacement (wind, waves) |
| Normal | Vector3 | Modified vertex normal |
| Tangent | Vector3 | Modified vertex tangent |

### Common Node Patterns

```
Time-based animation:
  Time node → Multiply (speed) → Add (UV) → Sample Texture 2D

Fresnel/rim lighting:
  Fresnel Effect node → Multiply (color) → Add to Emission

World-space triplanar:
  Position (World) → Triplanar node → Base Color

Dissolve effect:
  Sample Noise → Step (threshold) → Alpha + Emission at edge
```

### Sub-Graphs

Reusable shader modules. Create via **Assets > Create > Shader Graph > Sub Graph**.

```
Use for:
  - Shared noise functions across multiple shaders
  - UV manipulation utilities
  - Common blending operations
  - Team-standardized patterns
```

### Custom Function Nodes

Write HLSL code callable from Shader Graph:

```hlsl
// MyCustomFunction.hlsl
void MyBlend_float(float3 A, float3 B, float T, out float3 Out)
{
    Out = lerp(A, B, saturate(T));
}
```

In Shader Graph: Add **Custom Function** node, set Source to File, reference `.hlsl` file.

## Material Property Blocks

Per-renderer overrides without creating new material instances. GPU instancing friendly.

```csharp
// Ideal for: changing color per instance while keeping batching
private MaterialPropertyBlock _propBlock;
private Renderer _renderer;

void Awake()
{
    _propBlock = new MaterialPropertyBlock();
    _renderer = GetComponent<Renderer>();
}

void SetColor(Color color)
{
    _renderer.GetPropertyBlock(_propBlock);
    _propBlock.SetColor("_BaseColor", color);
    _renderer.SetPropertyBlock(_propBlock);
}

void SetDamageAmount(float damage)
{
    _renderer.GetPropertyBlock(_propBlock);
    _propBlock.SetFloat("_DamageAmount", damage);
    _renderer.SetPropertyBlock(_propBlock);
}
```

**When to use PropertyBlock vs material instance:**

| Scenario | Use |
|----------|-----|
| Unique color per unit (1000 soldiers) | PropertyBlock |
| Fundamentally different shader settings | Material instance |
| Animating a property per object | PropertyBlock |
| Changing textures per object | Material instance (breaks batching anyway) |
| GPU instancing with variation | PropertyBlock (required) |

## Texture Setup

### Import Settings

| Setting | Purpose | Recommended |
|---------|---------|-------------|
| Texture Type | Normal Map, Default, Sprite, etc. | Match usage |
| sRGB | Color space | ON for albedo/emission, OFF for data (normal, metallic) |
| Max Size | Resolution limit | 2048 for hero assets, 512-1024 for props |
| Compression | Quality vs size | Normal Quality for most, High for hero |
| Generate Mip Maps | LOD for textures | ON for 3D, OFF for UI/sprites |
| Filter Mode | Sampling | Bilinear (default), Trilinear (mipmaps), Point (pixel art) |
| Wrap Mode | Edge behavior | Repeat (tiling), Clamp (UI/skybox) |

### Compression Formats

| Platform | Format | Notes |
|----------|--------|-------|
| Desktop | BC7 (DXT5 fallback) | Best quality-to-size ratio |
| Android | ASTC 6x6 | Good balance; ETC2 as fallback |
| iOS | ASTC 6x6 | Standard for modern iOS |
| Normal Maps | BC5 (desktop), ASTC | 2-channel compression, better quality |

### Normal Map Encoding

```
Unity default: tangent-space, OpenGL Y+ (green up)
  - If importing DirectX normals (green down): check "Flip Green Channel" in import settings
  - Texture Type MUST be set to "Normal map" for correct compression and sRGB handling
```

