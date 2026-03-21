# Scene Loading Basics

> Part of the `unity-scene-management` skill. See [SKILL.md](SKILL.md) for the overview.

## Scene Loading Basics

### Synchronous Loading

```csharp
using UnityEngine.SceneManagement;

// Load by name (must be in Build Settings)
SceneManager.LoadScene("Gameplay");

// Load by build index
SceneManager.LoadScene(1);

// Load modes
SceneManager.LoadScene("Gameplay", LoadSceneMode.Single);    // Unloads current scene first (default)
SceneManager.LoadScene("UI_HUD", LoadSceneMode.Additive);    // Keeps current scene, adds new one
```

**Problems with synchronous loading:**
- Freezes the game until the scene is fully loaded
- No loading screen possible
- Causes visible frame hitches

Use synchronous loading only for tiny scenes or initial boot.

### Asynchronous Loading

```csharp
public class SceneLoader : MonoBehaviour
{
    public async void LoadScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Optional: prevent the scene from activating immediately
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            // Progress goes from 0 to 0.9, then jumps to 1.0 on activation
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log($"Loading: {progress:P0}");

            // Activate when ready (progress reaches 0.9)
            if (asyncLoad.progress >= 0.9f)
            {
                // Wait for player input, minimum time, or just activate
                asyncLoad.allowSceneActivation = true;
            }

            await System.Threading.Tasks.Task.Yield();
        }
    }
}
```

**Progress quirk:** `asyncLoad.progress` maxes at `0.9f` when `allowSceneActivation = false`. It jumps to `1.0f` only after activation. Normalize with `progress / 0.9f`.

## Loading Screen Pattern

A robust loading screen that handles async loading with minimum display time and fade transitions.

```csharp
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private float _minimumDisplayTime = 1.5f;
    [SerializeField] private float _fadeDuration = 0.3f;

    private static LoadingScreen _instance;

    private void Awake()
    {
        _instance = this;
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public static void Load(string sceneName)
    {
        _instance.gameObject.SetActive(true);
        _instance.StartCoroutine(_instance.LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        // Fade in loading screen
        yield return FadeCanvas(0f, 1f);
        _canvasGroup.blocksRaycasts = true;

        float startTime = Time.unscaledTime;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Update progress bar while loading
        while (asyncLoad.progress < 0.9f)
        {
            float progress = asyncLoad.progress / 0.9f;
            _progressBar.value = progress;
            _progressText.text = $"{progress:P0}";
            yield return null;
        }

        _progressBar.value = 1f;
        _progressText.text = "100%";

        // Enforce minimum display time (prevents flash)
        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < _minimumDisplayTime)
        {
            yield return new WaitForSecondsRealtime(_minimumDisplayTime - elapsed);
        }

        // Activate the loaded scene
        asyncLoad.allowSceneActivation = true;
        yield return asyncLoad; // Wait for activation to complete

        // Fade out loading screen
        _canvasGroup.blocksRaycasts = false;
        yield return FadeCanvas(1f, 0f);
        gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvas(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }
}
```

**Key decisions:**
- `Time.unscaledTime` / `WaitForSecondsRealtime` -- works even when game is paused (`Time.timeScale = 0`)
- `blocksRaycasts` -- prevents clicking through the loading screen
- Minimum display time -- prevents the loading screen from flashing for fast loads

## Additive Scene Loading

Load multiple scenes simultaneously. Essential for separating concerns: managers, gameplay, UI, audio.

```csharp
public class AdditiveSceneLoader : MonoBehaviour
{
    // Load a scene additively
    public async void LoadAdditive(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) await System.Threading.Tasks.Task.Yield();

        // Optionally set as active scene (new objects instantiate into active scene)
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);
    }

    // Unload an additive scene
    public async void UnloadScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
        {
            AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
            while (!op.isDone) await System.Threading.Tasks.Task.Yield();
        }
    }
}
```

**Active scene matters:** `Instantiate()` places new objects in the active scene. Lighting (ambient, skybox) comes from the active scene. Always set the active scene explicitly when using additive loading.

## Scene Bootstrapper Pattern

The most robust architecture for multi-scene games. One persistent "boot" scene holds managers; gameplay scenes are loaded additively.

```
Scenes/
  _Boot.unity          -- Persistent managers (always loaded, never unloaded)
  _UI.unity            -- HUD, menus (additive, persistent)
  MainMenu.unity       -- Main menu content (additive, swappable)
  Level_01.unity       -- Gameplay content (additive, swappable)
  Level_02.unity       -- Gameplay content (additive, swappable)
```

```csharp
// Lives in _Boot scene, marked DontDestroyOnLoad
public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private string _uiSceneName = "_UI";
    [SerializeField] private string _firstSceneName = "MainMenu";

    private string _currentContentScene;

    private static GameBootstrapper _instance;
    public static GameBootstrapper Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        // Load persistent UI scene
        yield return SceneManager.LoadSceneAsync(_uiSceneName, LoadSceneMode.Additive);

        // Load initial content scene
        yield return LoadContentScene(_firstSceneName);
    }

    public Coroutine TransitionTo(string sceneName)
    {
        return StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // Signal UI to show loading/transition
        OnSceneTransitionStarted?.Invoke();

        // Unload current content
        if (!string.IsNullOrEmpty(_currentContentScene))
        {
            yield return SceneManager.UnloadSceneAsync(_currentContentScene);
        }

        // Load new content
        yield return LoadContentScene(sceneName);

        OnSceneTransitionCompleted?.Invoke();
    }

    private IEnumerator LoadContentScene(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        _currentContentScene = sceneName;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(scene);
    }

    public event Action OnSceneTransitionStarted;
    public event Action OnSceneTransitionCompleted;
}

// Usage from anywhere:
// GameBootstrapper.Instance.TransitionTo("Level_02");
```

**Benefits:**
- Managers survive scene transitions (audio, input, settings)
- Each gameplay scene is self-contained
- UI persists across scenes without duplication
- Clean separation of concerns

### Ensuring Boot Scene Runs First

If a developer opens Level_01 directly in the editor, the boot scene has not loaded. Handle this with a bootstrap check.

```csharp
// Attach to an empty GameObject in every gameplay scene
public class BootstrapCheck : MonoBehaviour
{
    #if UNITY_EDITOR
    private void Awake()
    {
        // If boot scene hasn't loaded, load it
        if (GameBootstrapper.Instance == null)
        {
            Debug.Log("Boot scene not loaded. Loading bootstrapper...");
            // Remember which scene we're in
            string currentScene = SceneManager.GetActiveScene().name;
            PlayerPrefs.SetString("_EditorReturnScene", currentScene);

            SceneManager.LoadScene("_Boot");
        }
    }
    #endif
}
```

