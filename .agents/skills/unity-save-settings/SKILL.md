---
name: unity-save-settings
description: Unity Save & Settings Persistence
---


# Unity Save & Settings Persistence

Use this skill when implementing player settings persistence, preferences files, career save data, or modular save file architecture in Unity.

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



## Topic Pages

- [Keybind Persistence](skill-keybind-persistence.md)
- [Career Save Structure](skill-career-save-structure.md)
- [Auto-Save Triggers](skill-auto-save-triggers.md)
- [Leaderboards](skill-leaderboards.md)
- [Schema Versioning](skill-schema-versioning.md)
- [Graphics Settings](skill-graphics-settings.md)
- [Settings Persistence: JsonUtility + ScriptableObject Defaults](skill-settings-persistence-jsonutility-scriptableobject-defaults.md)

