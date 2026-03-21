---
name: unity-scene-management
description: Unity Scene Management
---


# Unity Scene Management

Use this skill when loading, unloading, or organizing scenes, implementing additive scene architectures, or building scene transition flows in Unity.

## DontDestroyOnLoad

Prevents a GameObject from being destroyed when a new scene loads (in Single mode).

```csharp
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    private void Awake()
    {
        // Singleton guard -- prevents duplicates when returning to a scene
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

**Pitfalls:**
- Objects in DontDestroyOnLoad cannot reference scene objects (they get destroyed)
- If the scene containing the singleton is loaded again, a duplicate spawns -- the guard above is mandatory
- DontDestroyOnLoad objects live in a special hidden scene (visible in Hierarchy under "DontDestroyOnLoad")
- With additive scene loading + bootstrapper pattern, you often do not need DontDestroyOnLoad at all -- the boot scene itself persists

## Build Settings Management

Scenes must be in File -> Build Settings to be loaded by name or index.

```csharp
// Check if a scene is in build settings (editor utility)
#if UNITY_EDITOR
public static bool IsSceneInBuild(string sceneName)
{
    foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
    {
        if (scene.enabled && scene.path.Contains(sceneName))
            return true;
    }
    return false;
}
#endif
```

**CI/automation tip:** Validate that all referenced scenes are in Build Settings as part of your build validation step. Missing scenes cause runtime errors with no compile-time warning.

## Multi-Scene Editing in the Editor

Unity supports having multiple scenes open simultaneously in the editor. This is useful for the bootstrapper pattern.

```
Hierarchy:
  _Boot (loaded)
    GameBootstrapper
    AudioManager
  _UI (loaded, additive)
    Canvas
    EventSystem
  Level_01 (loaded, additive, active)
    Terrain
    Player
    Enemies
```

**Editor workflow:**
1. Open `_Boot` scene
2. Drag `_UI` and `Level_01` into Hierarchy (or File -> Open Scene Additive)
3. Right-click `Level_01` -> Set Active Scene
4. Save All (Ctrl+Shift+S) saves each scene independently

**Gotcha:** Objects dragged between scenes in the editor change scene ownership. This can silently break references. Always verify an object's scene in the Inspector header.

## Scene Loading Architecture Decision Guide

| Game Type | Recommended Pattern |
|-----------|-------------------|
| Simple (1-5 scenes) | `LoadSceneAsync` with Single mode + loading screen |
| Medium (menu + levels) | Bootstrapper + additive loading |
| Large (open world) | Addressables + streaming + additive loading |
| Prototype / game jam | Synchronous loading is fine |

## Common Mistakes

1. **Loading by string without validation** -- scene renames cause silent failures
2. **Not handling async properly** -- scene activates before UI is ready
3. **DontDestroyOnLoad without singleton guard** -- duplicate managers on scene reload
4. **Forgetting to set active scene** -- lighting and Instantiate go to wrong scene
5. **Referencing scene objects from persistent objects** -- references become null after scene unload
6. **Not adding scenes to Build Settings** -- works in editor, fails in build


## Topic Pages

- [Scene Loading Basics](skill-scene-loading-basics.md)
- [Scene References Without Strings](skill-scene-references-without-strings.md)
- [Scene Lifecycle Events](skill-scene-lifecycle-events.md)

