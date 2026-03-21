# Career Save Structure

> Part of the `unity-save-settings` skill. See [SKILL.md](SKILL.md) for the overview.

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

