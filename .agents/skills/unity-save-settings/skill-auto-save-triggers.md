# Auto-Save Triggers

> Part of the `unity-save-settings` skill. See [SKILL.md](SKILL.md) for the overview.

## Auto-Save Triggers

Save at these moments — do not save every frame:

| Trigger | What to Save | Notes |
|---------|-------------|-------|
| After race complete | Career, leaderboard, ghost | Immediate |
| Settings panel close | Settings, bindings | 500ms debounce (user may toggle rapidly) |
| `OnApplicationPause(true)` | Everything dirty | Mobile/desktop alt-tab |
| `OnApplicationQuit` | Everything dirty | Final save before exit |

Debounce settings saves to avoid disk thrashing when the user is adjusting sliders:

```csharp
private float _settingsSaveTimer = -1f;

public void MarkSettingsDirty()
{
    _settingsSaveTimer = 0.5f; // 500ms debounce
}

void Update()
{
    if (_settingsSaveTimer > 0f)
    {
        _settingsSaveTimer -= Time.unscaledDeltaTime;
        if (_settingsSaveTimer <= 0f)
        {
            SaveSettings();
            _settingsSaveTimer = -1f;
        }
    }
}
```
