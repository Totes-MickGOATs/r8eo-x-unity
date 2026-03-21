---
name: unity-shaders
description: Unity Shaders
---


# Unity Shaders

Use this skill when writing custom HLSL shaders, building Shader Graph materials, creating compute shaders, or implementing custom render features in Unity.

## Shader Graph Overview

Visual node-based shader authoring available in URP and HDRP.

### Creating a Shader Graph

```
Assets > Create > Shader Graph > URP:
  - Lit Shader Graph — standard PBR (most common)
  - Unlit Shader Graph — no lighting, flat output
  - Sprite Lit/Unlit — 2D sprite shaders
  - Decal Shader Graph — surface decals (URP 12+)

HDRP equivalents available under HDRP submenu.
```

### Graph Structure

```
Properties (left panel):
  - Exposed parameters visible in Material Inspector
  - Types: Float, Color, Texture2D, Vector2/3/4, Boolean, Gradient

Vertex Stage:
  - Runs per vertex
  - Outputs: Position, Normal, Tangent
  - Use for: displacement, wind animation, vertex color

Fragment Stage:
  - Runs per pixel
  - Outputs: Base Color, Normal, Metallic, Smoothness, Emission, Alpha, AO
  - Use for: texturing, lighting calculations, effects

Node connections flow left → right.
Preview each node by clicking the preview icon.
```

### Properties

```csharp
// Exposed shader properties map to material API
material.SetFloat("_DissolveAmount", 0.5f);
material.SetColor("_FresnelColor", Color.cyan);
material.SetTexture("_MainTex", myTexture);

// Property reference names: set in Shader Graph property settings
// Use underscore prefix convention: _PropertyName
```

## Sub-Graphs

Reusable shader modules shared across multiple Shader Graphs.

### Creating

```
Assets > Create > Shader Graph > Sub Graph
  - Define inputs (properties) and outputs
  - Build node logic
  - Use in any Shader Graph via the Sub Graph node
```

### Example Sub-Graphs

```
UV_ScrollAndRotate:
  Inputs: UV, ScrollSpeed (Vector2), RotationSpeed (Float)
  Output: Modified UV

FresnelWithPulse:
  Inputs: Power, PulseSpeed, MinIntensity, MaxIntensity
  Output: Pulsing fresnel value

HeightBlend:
  Inputs: Texture A, Texture B, Height A, Height B, Blend Factor
  Output: Height-aware blended color (prevents mushy transitions)
```

## Shader Graph vs Custom HLSL — Decision Guide

| Criteria | Shader Graph | Custom HLSL |
|----------|-------------|-------------|
| Artist-friendly | Yes | No |
| Iteration speed | Fast (visual preview) | Slower (compile + test) |
| Custom lighting models | Limited (Custom Function) | Full control |
| Multi-pass shaders | No | Yes |
| Compute shaders | No | Yes |
| SRP Batcher | Automatic | Manual CBUFFER setup |
| Version control | Poor (binary .shadergraph) | Good (text .shader/.hlsl) |
| Performance ceiling | Slightly lower (generated code) | Highest |

**Recommendation:** Start with Shader Graph. Move to custom HLSL when you hit Shader Graph limits (custom lighting, multi-pass, compute, extreme optimization).


## Topic Pages

- [Common Shader Graph Patterns](skill-common-shader-graph-patterns.md)
- [Custom Function Nodes](skill-custom-function-nodes.md)

