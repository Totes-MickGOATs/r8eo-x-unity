# Async Scene Loading

> Part of the `unity-racing-ui` skill. See [SKILL.md](SKILL.md) for the overview.

## Async Scene Loading

Use the `allowSceneActivation = false` pattern for loading screens with progress bars:

```csharp
public async Awaitable LoadTrackScene(string sceneName, Slider progressBar)
{
    AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
    op.allowSceneActivation = false;

    while (op.progress < 0.9f)
    {
        // progress stops at 0.9 until allowSceneActivation = true
        progressBar.value = op.progress / 0.9f;
        await Awaitable.NextFrameAsync();
    }

    progressBar.value = 1f;
    // Optional: hold for minimum display time or player input
    await Awaitable.WaitForSecondsAsync(0.5f);

    op.allowSceneActivation = true;
}
```

---

