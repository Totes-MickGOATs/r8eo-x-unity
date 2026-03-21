# Scene Lifecycle Events

> Part of the `unity-scene-management` skill. See [SKILL.md](SKILL.md) for the overview.

## Scene Lifecycle Events

```csharp
// Subscribe to scene events globally
private void OnEnable()
{
    SceneManager.sceneLoaded += OnSceneLoaded;
    SceneManager.sceneUnloaded += OnSceneUnloaded;
    SceneManager.activeSceneChanged += OnActiveSceneChanged;
}

private void OnDisable()
{
    SceneManager.sceneLoaded -= OnSceneLoaded;
    SceneManager.sceneUnloaded -= OnSceneUnloaded;
    SceneManager.activeSceneChanged -= OnActiveSceneChanged;
}

private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    Debug.Log($"Loaded: {scene.name} (mode: {mode})");
}

private void OnSceneUnloaded(Scene scene)
{
    Debug.Log($"Unloaded: {scene.name}");
}

private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
{
    Debug.Log($"Active scene: {oldScene.name} -> {newScene.name}");
}
```

