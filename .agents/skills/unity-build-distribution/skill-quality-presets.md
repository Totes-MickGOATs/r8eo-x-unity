# Quality Presets

> Part of the `unity-build-distribution` skill. See [SKILL.md](SKILL.md) for the overview.

## Quality Presets

| Setting | Low | Medium | High | Ultra |
|---------|-----|--------|------|-------|
| Texture Quality | Quarter | Half | Full | Full |
| Shadow Resolution | 512 | 1024 | 2048 | 4096 |
| Shadow Distance | 50 | 100 | 150 | 200 |
| Anti-Aliasing | Off | FXAA | SMAA | TAA |
| Ambient Occlusion | Off | SSAO Low | SSAO Med | SSAO High |
| LOD Bias | 0.5 | 1.0 | 1.5 | 2.0 |
| Particle Density | 25% | 50% | 75% | 100% |

### Auto-Detect

```csharp
int DetectQualityLevel()
{
    int vram = SystemInfo.graphicsMemorySize;
    if (vram >= 8192) return 3;      // Ultra
    if (vram >= 4096) return 2;      // High
    if (vram >= 2048) return 1;      // Medium
    return 0;                         // Low
}
```

Run auto-detect on first launch only. Save the result so the player's manual changes persist.

