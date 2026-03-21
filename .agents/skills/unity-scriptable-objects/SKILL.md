---
name: unity-scriptable-objects
description: Unity ScriptableObject Architecture
---


# Unity ScriptableObject Architecture

Use this skill when designing data-driven systems with ScriptableObjects for configuration assets, event channels, shared runtime state, or runtime sets.

## Use Cases Overview

| Pattern | Purpose | Example |
|---------|---------|---------|
| **Config Data** | Read-only tuning values | Weapon stats, enemy definitions, level configs |
| **Event Channels** | Decoupled event bus | `OnPlayerDied`, `OnScoreChanged`, `OnLevelComplete` |
| **Shared Variables** | Runtime-writable state visible to multiple systems | Player health, score, current weapon |
| **Runtime Sets** | Track active objects without Find/singleton | Active enemies, active projectiles, spawn points |
| **Enum Replacement** | Extensible type system | Damage types, surface types, AI states |

## Nested SOs and Serialization

```csharp
// SO referencing other SOs -- works great
[CreateAssetMenu(menuName = "Game/Character Class")]
public class CharacterClass : ScriptableObject
{
    public string ClassName;
    public WeaponData StartingWeapon;       // Reference to another SO -- fine
    public List<AbilityData> Abilities;     // List of SO references -- fine
}

// GOTCHA: Creating SOs at runtime
// SOs created via ScriptableObject.CreateInstance() are NOT saved to disk.
// They exist only in memory and are lost when play mode ends.
var tempConfig = ScriptableObject.CreateInstance<WeaponData>();
// This is fine for runtime-only data, but don't expect persistence.

// GOTCHA: Nested SOs as sub-assets
// If you create a SO and add it to another SO via AssetDatabase.AddObjectToAsset,
// it becomes a sub-asset. This is editor-only and requires careful management.
```

## Editor-Only vs Runtime Data

```csharp
[CreateAssetMenu(menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Runtime Data")]
    [SerializeField] private string _levelName;
    [SerializeField] private int _sceneIndex;
    [SerializeField] private int _requiredStars;

    [Header("Editor-Only Metadata")]
    #if UNITY_EDITOR
    [TextArea(3, 10)]
    [SerializeField] private string _designNotes;
    [SerializeField] private bool _isPlaytested;
    #endif

    public string LevelName => _levelName;
    public int SceneIndex => _sceneIndex;
    public int RequiredStars => _requiredStars;
}
```

## When NOT to Use ScriptableObjects

| Situation | Better Alternative | Reason |
|-----------|--------------------|--------|
| Per-instance mutable state | MonoBehaviour field | SOs are shared; mutating one affects all references |
| Large datasets (1000+ items) | JSON, SQLite, Addressables | Editor slows down with many SO assets |
| User-generated content | JSON/binary serialization | SOs require AssetDatabase (editor-only) |
| Save game data | JSON/binary file | SOs don't persist runtime changes to disk |
| Temporary runtime data | Plain C# class | No need for Unity serialization overhead |
| Configuration that changes per-build | Build scripts / defines | SOs are baked into builds |

## Testing with ScriptableObjects

```csharp
// SOs are easy to create in tests -- no scene or GameObject needed
[Test]
public void WeaponDamage_WithArmorReduction_CalculatesCorrectly()
{
    // Arrange
    var fireDamage = ScriptableObject.CreateInstance<DamageType>();
    var config = ScriptableObject.CreateInstance<WeaponData>();
    // Set fields via reflection or make them public for tests

    // Act & Assert
    // ...

    // Cleanup (prevent memory leak in tests)
    Object.DestroyImmediate(fireDamage);
    Object.DestroyImmediate(config);
}
```

## SO Lifecycle Gotchas

1. **OnEnable runs in editor** -- SOs call `OnEnable` when loaded in the editor, not just in play mode. Guard runtime-only logic with `if (Application.isPlaying)`.

2. **Shared state persists in editor** -- If an SO's runtime value is changed during play mode AND the field is serialized, the change persists after exiting play mode. Use `[System.NonSerialized]` for runtime-only state.

3. **OnDisable/OnDestroy timing** -- SOs are destroyed when no longer referenced or on domain reload. Don't assume they exist forever.

4. **Addressables** -- For large projects, load SOs via Addressables instead of direct references to reduce memory footprint and enable asset bundles.


## Topic Pages

- [What Are ScriptableObjects?](skill-what-are-scriptableobjects.md)
- [Config Data Pattern](skill-config-data-pattern.md)
- [Database Pattern](skill-database-pattern.md)

