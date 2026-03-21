# Startup Optimization

> Part of the `unity-build-distribution` skill. See [SKILL.md](SKILL.md) for the overview.

## Startup Optimization

1. **Boot scene is minimal:** Logo, loading bar, nothing else
2. **Async scene loading:** `SceneManager.LoadSceneAsync()` for all scene transitions
3. **Addressables preload:** Start loading persistent groups in boot scene
4. **Async upload buffer:** Set `QualitySettings.asyncUploadBufferSize` to 64-128 MB (default 4 MB is too low)
5. **Async upload time slice:** Set `QualitySettings.asyncUploadTimeSlice` to 4-8 ms

```csharp
// Boot scene MonoBehaviour
IEnumerator Start()
{
    // Preload persistent assets
    var handle = Addressables.LoadAssetsAsync<Object>("Persistent_Shared", null);
    yield return handle;

    // Load main menu
    yield return SceneManager.LoadSceneAsync("MainMenu");
}
```

