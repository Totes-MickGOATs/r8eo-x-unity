# Common Shader Graph Patterns

> Part of the `unity-shaders` skill. See [SKILL.md](SKILL.md) for the overview.

## Common Shader Graph Patterns

### Dissolve Effect

```
Nodes:
  1. Sample Texture 2D (noise texture) → R channel
  2. Property: _DissolveAmount (Float, Range 0-1)
  3. Step(noise, _DissolveAmount) → Alpha (with Alpha Clip enabled)
  4. Edge glow: Smoothstep narrow band → multiply by emission color → Emission

Graph Settings:
  - Surface Type: Opaque
  - Alpha Clipping: enabled
```

### Hologram

```
Nodes:
  1. Screen Position → frac(y * _ScanlineCount) → Step → scanlines
  2. Fresnel Effect (power ~3) → rim glow
  3. Multiply scanlines + rim → Emission
  4. Time → sin(time * _FlickerSpeed) → remap 0.5-1.0 → Alpha

Graph Settings:
  - Surface Type: Transparent
  - Render Face: Both
  - Blend: Additive
```

### Water Surface

```
Vertex Stage:
  1. UV + Time → GradientNoise → vertex Y displacement (waves)
  2. Two noise layers at different scales/speeds → sum

Fragment Stage:
  1. Normal from Noise: two scrolling normal maps blended
  2. Base Color: deep + shallow color lerped by depth (Scene Depth - Fragment Depth)
  3. Smoothness: high (0.9+)
  4. Fresnel → lerp between water color and reflection tint
  5. Alpha: depth-based edge fade
```

### Toon / Cel Shading

```
Nodes:
  1. Normal Vector (World) → Dot Product with Main Light Direction
  2. Smoothstep with narrow range → quantized lighting bands
  3. Sample Ramp Texture (1D gradient) for artistic control
  4. Outline: second pass with vertex extrusion along normals (or use Render Feature)

Alternative: Custom Lighting node (requires Custom Function)
```

### Force Field / Shield

```
Nodes:
  1. Fresnel Effect (power ~2-4) → rim visibility
  2. UV scrolling hex pattern → multiply with fresnel
  3. Intersection: Scene Depth vs Fragment Depth → thin edge highlight
  4. Time-based pulse: sin(time) modulating intensity
  5. All combined → Emission (additive transparent surface)

Graph Settings:
  - Surface Type: Transparent
  - Blend: Additive
  - Render Face: Both
  - ZWrite: Off
```

### Triplanar Mapping

```
Nodes:
  1. Position (World) → split into XY, XZ, YZ plane UVs
  2. Sample Texture 2D × 3 (one per plane)
  3. Normal Vector (World) → absolute → power (sharpness) → weights
  4. Blend three samples by weights

Or use built-in Triplanar node (simpler but less control).
```

