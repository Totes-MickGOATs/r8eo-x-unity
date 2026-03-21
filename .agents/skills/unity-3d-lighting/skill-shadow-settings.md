# Shadow Settings

> Part of the `unity-3d-lighting` skill. See [SKILL.md](SKILL.md) for the overview.

## Shadow Settings

### Cascade Shadows (Directional Light)

```
URP Asset > Lighting > Main Light:
  - Shadow Resolution: 1024-4096 per cascade
  - Cascade Count: 2 (mobile), 4 (desktop)
  - Shadow Distance: 50-150m (balance quality vs coverage)
  - Last Border: 0.1 (fade out at max distance)

Cascade splits control quality distribution:
  - Front-loaded: better near-camera quality (default, recommended)
  - Even: uniform quality at all distances
```

### Shadow Bias

```
Depth Bias: pushes shadow map away from surface
  - Too low: shadow acne (self-shadowing artifacts)
  - Too high: peter-panning (shadows detach from objects)
  - Start at 1.0, adjust per-light

Normal Bias: pushes along surface normal
  - Helps with thin geometry shadow artifacts
  - Start at 1.0
```

### Per-Light Shadow Settings

```csharp
Light light = GetComponent<Light>();
light.shadows = LightShadows.Soft;      // None, Hard, Soft
light.shadowResolution = LightShadowResolution.High;
light.shadowBias = 0.05f;
light.shadowNormalBias = 0.4f;
light.shadowNearPlane = 0.2f;
```

