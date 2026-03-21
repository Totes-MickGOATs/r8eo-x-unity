# Settings Persistence: JsonUtility + ScriptableObject Defaults

> Part of the `unity-save-settings` skill. See [SKILL.md](SKILL.md) for the overview.

## Settings Persistence: JsonUtility + ScriptableObject Defaults

The recommended pattern uses a ScriptableObject asset as the source of defaults:

1. Create a `DefaultSettings.asset` ScriptableObject in `Resources/` with all default values
2. At runtime, `Instantiate()` the SO to get a mutable copy (never modify the asset directly)
3. If a saved JSON file exists, apply `JsonUtility.FromJsonOverwrite(json, instance)` on top
4. The SO fields define the schema — new fields automatically get defaults, removed fields are ignored

```csharp
// Load pattern
var defaults = Resources.Load<GameSettings>("DefaultSettings");
var settings = Instantiate(defaults); // mutable runtime copy
string path = Path.Combine(Application.persistentDataPath, "settings.json");
if (File.Exists(path))
{
    string json = File.ReadAllText(path);
    JsonUtility.FromJsonOverwrite(json, settings);
}
```

This gives you forward-compatible saves: adding new fields to the SO automatically provides defaults for existing save files.

