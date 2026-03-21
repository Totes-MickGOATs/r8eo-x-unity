---
name: unity-3d-materials
description: Unity 3D Materials and Shaders
---


# Unity 3D Materials and Shaders

Use this skill when creating or configuring materials, assigning textures, or setting up shader properties on 3D objects in Unity.

## HDRP Lit Shader

Extended PBR features beyond URP.

### Additional Features

| Feature | Purpose |
|---------|---------|
| Subsurface Scattering | Light passing through translucent materials (skin, wax, leaves) |
| Translucency | Back-lit thin surfaces (ears, curtains) |
| Coat Mask | Clear-coat layer (car paint, lacquered wood) |
| Anisotropy | Direction-dependent reflections (brushed metal, hair) |
| Iridescence | Color shift based on viewing angle (soap bubbles, oil) |
| Detail Map | Micro-surface detail at close range |

### HDRP Material Types

```
Lit — standard PBR (most objects)
Unlit — no lighting, flat color (UI elements, custom lighting)
Decal — projected surface detail
Fabric — cotton, silk, velvet (custom shading model)
Hair — strand-based anisotropic shading
Eye — specialized eye rendering with caustics
StackLit — advanced layered materials (coat + base)
```

## PBR Workflow

### Metallic Workflow (Unity Default)

```
Base Color (RGB): albedo — the color of the surface
  - Metals: reflectance color (gold = yellow, copper = orange)
  - Non-metals: diffuse color (plastic, wood, fabric)

Metallic (Float 0-1):
  - 0: dielectric (non-metal) — most things
  - 1: conductor (metal) — pure metals only
  - In-between: rare (dirty/painted metal)

Smoothness (Float 0-1):
  - 0: completely rough (concrete, chalk)
  - 1: perfectly smooth (mirror, chrome)
```

### Specular Workflow (Alternative)

```
Use "Lit (Specular)" shader variant.
  - Specular Color (RGB): replaces Metallic
  - More control for non-standard materials
  - Most teams prefer Metallic workflow (simpler)
```

### Texture Authoring Guidelines

```
Albedo:
  - NO lighting information (no shadows, no highlights)
  - Consistent brightness (50-240 sRGB range)
  - Pure white and pure black are physically impossible

Normal Map:
  - Encode surface detail, not shape (that's geometry)
  - Flat = (128, 128, 255) in tangent space
  - Use Substance/xNormal/Marmoset for baking

Metallic/Smoothness:
  - Pack into one texture: RGB = metallic, A = smoothness
  - Most surfaces are 0 or 1 metallic (no gradients)
  - Smoothness varies widely (fingerprints, wear, dirt)

Ambient Occlusion:
  - Baked cavity shadows (crevices, folds)
  - Grayscale: white = fully lit, black = fully occluded
  - Do NOT bake directional shadows (only ambient)
```

## Material Variants (Unity 2022+)

Parent-child material relationships. Change the parent; children inherit unless overridden.

```
Create: right-click material > Create > Material Variant
  - Override individual properties
  - Non-overridden properties track the parent
  - Useful for: color variations of the same base material
  - Example: BaseWood → DarkWood, LightWood, WetWood
```

```csharp
// Check if property is overridden
bool isOverridden = material.IsPropertyOverriden("_BaseColor"); // Note Unity API typo

// Reset override (revert to parent value)
material.RevertPropertyOverride("_BaseColor");
```

## Material Debugging

```csharp
// List all properties on a material
Shader shader = material.shader;
int count = shader.GetPropertyCount();
for (int i = 0; i < count; i++)
{
    string name = shader.GetPropertyName(i);
    ShaderPropertyType type = shader.GetPropertyType(i);
    Debug.Log($"{name}: {type}");
}

// Check enabled keywords
foreach (string keyword in material.enabledKeywords)
    Debug.Log($"Keyword: {keyword}");
```

### Frame Debugger

```
Window > Analysis > Frame Debugger
  - Step through every draw call
  - See which material/shader is used
  - Identify batching breaks (different material = new batch)
  - Check shader properties per draw call
```


## Topic Pages

- [Material Fundamentals](skill-material-fundamentals.md)
- [Shader Graph](skill-shader-graph.md)
- [Shader Keywords and Variants](skill-shader-keywords-and-variants.md)

