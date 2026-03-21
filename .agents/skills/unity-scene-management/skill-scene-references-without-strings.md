# Scene References Without Strings

> Part of the `unity-scene-management` skill. See [SKILL.md](SKILL.md) for the overview.

## Scene References Without Strings

Avoid magic strings for scene names. They break silently when scenes are renamed.

### ScriptableObject Scene Reference

```csharp
[CreateAssetMenu(menuName = "Scene Management/Scene Reference")]
public class SceneReference : ScriptableObject
{
    #if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset _sceneAsset;

    private void OnValidate()
    {
        if (_sceneAsset != null)
        {
            _sceneName = _sceneAsset.name;
            _scenePath = UnityEditor.AssetDatabase.GetAssetPath(_sceneAsset);
        }
    }
    #endif

    [SerializeField, HideInInspector] private string _sceneName;
    [SerializeField, HideInInspector] private string _scenePath;

    public string SceneName => _sceneName;

    public AsyncOperation LoadAsync(LoadSceneMode mode = LoadSceneMode.Single)
    {
        return SceneManager.LoadSceneAsync(_sceneName, mode);
    }

    public AsyncOperation UnloadAsync()
    {
        return SceneManager.UnloadSceneAsync(_sceneName);
    }
}

// Usage in Inspector: drag a SceneReference asset instead of typing a string
public class LevelSelector : MonoBehaviour
{
    [SerializeField] private SceneReference[] _levels;

    public void LoadLevel(int index) => _levels[index].LoadAsync();
}
```

### Enum-Based Scene Index

```csharp
// Simpler alternative -- maintain an enum matching Build Settings order
public enum GameScene
{
    Boot = 0,
    MainMenu = 1,
    Level01 = 2,
    Level02 = 3,
}

public static class SceneExtensions
{
    public static AsyncOperation LoadAsync(this GameScene scene, LoadSceneMode mode = LoadSceneMode.Single)
    {
        return SceneManager.LoadSceneAsync((int)scene, mode);
    }
}

// Usage
GameScene.Level01.LoadAsync();
```

**Tradeoff:** Enum is simpler but requires manual sync with Build Settings. SO references are safer but more setup.

## Cross-Scene Communication

When systems in different scenes need to communicate without direct references.

### Via ScriptableObject Events (Recommended)

```csharp
// Create a GameEvent asset (see ScriptableObjects skill)
// Both scenes reference the same SO event -- no direct coupling

// In gameplay scene:
_onPlayerDied.Raise();

// In UI scene:
_onPlayerDied.Subscribe(ShowGameOverScreen);
```

### Via Static Events

```csharp
public static class GameEvents
{
    public static event Action<string> OnSceneRequested;

    public static void RequestScene(string name) => OnSceneRequested?.Invoke(name);
}

// Any scene can request a transition:
GameEvents.RequestScene("Level_02");

// Bootstrapper listens:
private void OnEnable() => GameEvents.OnSceneRequested += TransitionTo;
private void OnDisable() => GameEvents.OnSceneRequested -= TransitionTo;
```

