# Graphics Settings

> Part of the `unity-save-settings` skill. See [SKILL.md](SKILL.md) for the overview.

## Graphics Settings

Layer approach: preset first, then individual overrides.

```csharp
// Apply quality preset (Low=0, Medium=1, High=2, Ultra=3)
QualitySettings.SetQualityLevel(presetIndex, applyExpensiveChanges: true);

// Override individual settings on top of the preset
QualitySettings.shadowResolution = saved.shadowResolution;
QualitySettings.antiAliasing = saved.antiAliasing;

// Resolution
Screen.SetResolution(saved.width, saved.height, saved.fullScreenMode, saved.refreshRate);
```

### VSync Interaction

**Critical:** When `QualitySettings.vSyncCount > 0`, Unity ignores `Application.targetFrameRate` entirely.

- If VSync is ON: disable the FPS limit slider in UI, set `targetFrameRate = -1`
- If VSync is OFF: enable the FPS limit slider, apply `targetFrameRate` from settings
- Always set `vSyncCount` before `targetFrameRate`

