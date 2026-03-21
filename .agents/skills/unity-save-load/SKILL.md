---
name: unity-save-load
description: Unity Save/Load and Data Persistence
---


# Unity Save/Load and Data Persistence

Use this skill when implementing save/load systems, persisting game state to disk, managing PlayerPrefs, or building serialization pipelines for game data.

## PlayerPrefs — Simple Key-Value Storage

PlayerPrefs stores small amounts of data in OS-specific locations (registry on Windows, plist on macOS). Use for **settings only**, not game saves.

```csharp
// Writing
PlayerPrefs.SetInt("HighScore", 9999);
PlayerPrefs.SetFloat("MasterVolume", 0.8f);
PlayerPrefs.SetString("PlayerName", "Totes");
PlayerPrefs.Save(); // flush to disk (automatic on Application.Quit)

// Reading (with defaults)
int highScore = PlayerPrefs.GetInt("HighScore", 0);
float volume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
string name = PlayerPrefs.GetString("PlayerName", "Player");

// Checking existence
if (PlayerPrefs.HasKey("MasterVolume")) { /* ... */ }

// Deleting
PlayerPrefs.DeleteKey("MasterVolume");
PlayerPrefs.DeleteAll(); // nuclear option — settings reset
```

**Limitations:**
- Not secure (plain text in registry/plist)
- No structured data (no arrays, no objects)
- Platform-specific storage locations
- Do NOT use for save games — use JSON/binary files instead

## Resources Folder

`Resources.Load` is synchronous and convenient but has downsides:

```csharp
// Load from Assets/Resources/
var prefab = Resources.Load<GameObject>("Prefabs/Enemy");
var texture = Resources.Load<Texture2D>("Textures/icon");
var all = Resources.LoadAll<AudioClip>("Audio/SFX");
```

**Why to avoid Resources:**
- ALL assets in Resources/ are included in the build, even if unused
- Cannot be unloaded individually
- Causes longer build times
- No async loading
- Prefer Addressables for anything beyond prototyping

## StreamingAssets

Files in `Assets/StreamingAssets/` are included in the build as-is (not processed by Unity). Useful for raw config files, databases, or pre-built data.

```csharp
// Read a file from StreamingAssets
string path = Path.Combine(Application.streamingAssetsPath, "config.json");

// Platform differences:
// Windows/Mac/Linux: file:// path, can use File.ReadAllText
// Android: jar:// path, must use UnityWebRequest
// WebGL: http:// path, must use UnityWebRequest

#if UNITY_ANDROID && !UNITY_EDITOR
    // Android requires UnityWebRequest
    using var www = UnityWebRequest.Get(path);
    yield return www.SendWebRequest();
    string json = www.downloadHandler.text;
#else
    string json = File.ReadAllText(path);
#endif
```

## Summary — When to Use What

| Method | Use Case | Pros | Cons |
|--------|----------|------|------|
| **PlayerPrefs** | Settings (volume, quality) | Simple, cross-platform | Not secure, no structured data |
| **JsonUtility** | Simple save data, no Dictionary | Fast, zero dependencies | Limited type support |
| **Newtonsoft.Json** | Complex save data, Dictionary, polymorphism | Full-featured | External dependency |
| **Custom binary** | Performance-critical or large saves | Small files, fast | Manual maintenance |
| **Addressables** | Asset loading (prefabs, textures, audio) | Async, memory-managed, CDN support | Setup complexity |
| **Resources** | Quick prototyping only | Simple API | Build bloat, no async |
| **StreamingAssets** | Raw files (configs, databases) | Unprocessed, platform-native | Platform-specific loading |
| **ScriptableObject** | Game data templates (items, enemies) | Editor-friendly, reusable | Not for mutable runtime state |


## Topic Pages

- [JSON Serialization](skill-json-serialization.md)
- [Cloud Saves](skill-cloud-saves.md)

