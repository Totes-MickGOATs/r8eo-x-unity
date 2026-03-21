# Performance Considerations

> Part of the `unity-weather-conditions` skill. See [SKILL.md](SKILL.md) for the overview.

## Performance Considerations

### Particle LOD by Distance

```csharp
void UpdateParticleLOD(float distanceToCamera)
{
    var emission = particleSystem.emission;
    if (distanceToCamera > 50f)
    {
        emission.rateOverTimeMultiplier = 0f; // Too far, disable
    }
    else if (distanceToCamera > 25f)
    {
        emission.rateOverTimeMultiplier = 0.25f; // Reduced
    }
    else
    {
        emission.rateOverTimeMultiplier = 1f; // Full quality
    }
}
```

### Shader LOD Tiers

```csharp
// Quality settings drive shader complexity
if (QualitySettings.GetQualityLevel() < 2) // Low
{
    Shader.DisableKeyword("_RIPPLE_NORMALS");
    Shader.DisableKeyword("_TRACK_EVOLUTION");
}
```

### Dynamic Quality Adjustment

```csharp
// If frame time exceeds budget, reduce weather effects
if (Time.unscaledDeltaTime > targetFrameTime * 1.2f)
{
    ReduceParticleCount(0.5f);
    DisableRippleNormals();
}
```

---

