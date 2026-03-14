# Unity Save & Settings Persistence

Patterns for player settings, preferences, career data, and save file architecture in Unity.

## Modular File Layout

Separate concerns into individual files rather than one monolithic save:

| File | Contents |
|------|----------|
| `settings.json` | Graphics, audio, gameplay preferences |
| `bindings.json` | Input rebinding overrides |
| `profile.json` | Player name, stats, cosmetic selections |
| `career.json` | Track progress, currency, XP, unlocks |
| `leaderboards/` | Per-track leaderboard files |
| `ghosts/` | Per-track ghost replay data |

Store in `Application.persistentDataPath`. Each file loads and saves independently — a corrupt leaderboard file does not destroy settings.

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

## Schema Versioning

Every save file must have `schemaVersion` at the root:

```json
{
    "schemaVersion": 3,
    "masterVolume": 0.8,
    "resolution": { "width": 1920, "height": 1080 }
}
```

On load, run a migration pipeline:

```csharp
while (data.schemaVersion < CurrentVersion)
{
    switch (data.schemaVersion)
    {
        case 1: MigrateV1ToV2(data); break;
        case 2: MigrateV2ToV3(data); break;
    }
    data.schemaVersion++;
}
```

Each migration method is a pure function that transforms the data in place. Never skip versions — always migrate sequentially.

## Atomic Writes

Never write directly to the live file — a crash mid-write destroys the save:

```csharp
public static void AtomicWrite(string path, string json)
{
    string tmp = path + ".tmp";
    string bak = path + ".bak";

    File.WriteAllText(tmp, json);            // 1. Write to .tmp
    if (File.Exists(path))
        File.Replace(tmp, path, bak);        // 2. Atomic replace, old -> .bak
    else
        File.Move(tmp, path);                // 2b. First write, just rename
}
```

On load, if the primary file is missing or corrupt, fall back to `.bak`.

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

## Audio Settings

Use `AudioMixer` exposed parameters for volume control:

```csharp
// Expose parameters in AudioMixer: "MasterVolume", "SFXVolume", "MusicVolume"

// Convert linear 0-1 slider to dB (logarithmic)
float dB = Mathf.Log10(Mathf.Max(linearValue, 0.0001f)) * 20f;
mixer.SetFloat("MasterVolume", dB);

// Save the LINEAR value (0-1), convert to dB on load
```

- Linear 0.0 maps to -80 dB (effectively silent)
- Linear 1.0 maps to 0 dB (full volume)
- Never save dB values — save the linear slider position

## Keybind Persistence

Use the Input System's built-in serialization — do NOT manually serialize individual bindings:

```csharp
// Save — the ONLY correct API
string overrides = playerInput.actions.SaveBindingOverridesAsJson();
File.WriteAllText(bindingsPath, overrides);

// Load — the ONLY correct API
string json = File.ReadAllText(bindingsPath);
playerInput.actions.LoadBindingOverridesFromJson(json);
```

### Composite Rebinding (WASD)

Composite bindings (like WASD movement) have parts. Target them by binding index:

```csharp
// For a Vector2 composite "Move" with Up/Down/Left/Right parts:
// Index 0 = the composite itself (skip this)
// Index 1 = Up part
// Index 2 = Down part
// Index 3 = Left part
// Index 4 = Right part

action.PerformInteractiveRebinding()
    .WithTargetBinding(2)  // rebind the "Down" part
    .Start();
```

Always skip index 0 (the composite node) when listing rebindable parts in the UI.

## Player Profiles

Use a single profile with stats — not an array of profiles:

```csharp
[Serializable]
public class PlayerProfile
{
    public string playerName;
    public int totalRaces;
    public float totalPlayTime;
    public Dictionary<string, TrackCareerData> trackProgress; // keyed by track ID
    // WARNING: Requires Newtonsoft.Json or ISerializationCallbackReceiver (see Career Save section)
}
```

## Career Save Structure

> **CRITICAL: `JsonUtility` cannot serialize `Dictionary<K,V>`.** The examples below use dictionaries for the correct data model, but you MUST use one of these serialization approaches:
>
> 1. **Newtonsoft.Json** (recommended) -- install `com.unity.nuget.newtonsoft-json` from Unity Package Manager. Use `JsonConvert.SerializeObject()` / `DeserializeObject<T>()` instead of `JsonUtility`.
> 2. **`ISerializationCallbackReceiver`** -- wrap the dictionary with parallel `List<string>` keys and `List<T>` values, synced in `OnBeforeSerialize`/`OnAfterDeserialize`. More boilerplate but avoids the external dependency.
>
> `JsonUtility` will silently produce empty `{}` for dictionary fields, causing data loss.

Use dictionaries keyed by track/vehicle ID — not arrays:

```csharp
[Serializable]
public class CareerData
{
    public int schemaVersion;
    public int currency;
    public int xp;
    public List<string> unlockedVehicles;
    public List<string> unlockedTracks;
    public Dictionary<string, TrackProgress> tracks; // key = track ID
    // WARNING: Requires Newtonsoft.Json or ISerializationCallbackReceiver -- see note above
}

[Serializable]
public class TrackProgress
{
    public float bestTime;
    public int timesCompleted;
    public List<string> medalsEarned;
}
```

**Why dictionaries?** Adding new tracks to the game does not break existing saves. Array-indexed saves break when you insert or reorder tracks.

## Leaderboards

Per-track leaderboard files with a 10-entry cap:

```csharp
[Serializable]
public class TrackLeaderboard
{
    public int schemaVersion;
    public string trackId;
    public List<LeaderboardEntry> entries; // max 10, sorted ascending by time
}

[Serializable]
public class LeaderboardEntry
{
    public float time;
    public string vehicleId;
    public string date;
    public string ghostFileName; // reference to ghosts/ directory
}
```

Insertion strategy:
1. Add new entry to list
2. Sort by time ascending
3. Trim to 10 entries
4. If a ghost reference was removed by trim, delete the ghost file

## Data Integrity

Wrap save files in a checksum envelope:

```csharp
[Serializable]
public class SaveEnvelope<T>
{
    public string checksum; // SHA-256 of the payload JSON
    public T payload;
}
```

On save: serialize payload to JSON, compute SHA-256, wrap in envelope, write.
On load: deserialize envelope, compute SHA-256 of payload JSON, compare to stored checksum.

If checksum mismatch:
1. Log warning
2. Attempt to load `.bak` file
3. If `.bak` also fails, reset to defaults and notify player

## Cloud Saves

### Steam Auto-Cloud (Simplest)

Configure in Steamworks partner dashboard — Steam automatically syncs `Application.persistentDataPath` files. Zero code required. Conflict resolution: Steam's default (usually last-write-wins).

### Unity Gaming Services Cloud Save

Cross-platform support. Use `CloudSaveService.Instance.Data.Player`:

```csharp
// Save
var data = new Dictionary<string, object> { { "settings", settingsJson } };
await CloudSaveService.Instance.Data.Player.SaveAsync(data);

// Load
var keys = new HashSet<string> { "settings" };
var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
```

Conflict resolution: last-write-wins (simplest for settings/preferences).

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
