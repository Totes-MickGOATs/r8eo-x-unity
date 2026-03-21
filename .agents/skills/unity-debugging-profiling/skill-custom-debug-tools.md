# Custom Debug Tools

> Part of the `unity-debugging-profiling` skill. See [SKILL.md](SKILL.md) for the overview.

## Custom Debug Tools

### Runtime Debug Menu

```csharp
public class DebugMenu : MonoBehaviour
{
    private bool _showMenu;
    private bool _showFPS;
    private bool _showPhysics;
    private bool _godMode;

    private float _fps;
    private float _fpsTimer;
    private int _frameCount;

    #if UNITY_EDITOR || DEVELOPMENT_BUILD

    private void Update()
    {
        // Toggle with backtick
        if (Input.GetKeyDown(KeyCode.BackQuote))
            _showMenu = !_showMenu;

        // F-key toggles
        if (Input.GetKeyDown(KeyCode.F1)) _showFPS = !_showFPS;
        if (Input.GetKeyDown(KeyCode.F2)) _showPhysics = !_showPhysics;
        if (Input.GetKeyDown(KeyCode.F3)) _godMode = !_godMode;

        // FPS counter
        _frameCount++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 0.5f)
        {
            _fps = _frameCount / _fpsTimer;
            _frameCount = 0;
            _fpsTimer = 0f;
        }
    }

    private void OnGUI()
    {
        if (_showFPS)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 24 };
            style.normal.textColor = _fps >= 55 ? Color.green :
                                     _fps >= 30 ? Color.yellow : Color.red;
            GUI.Label(new Rect(10, 10, 200, 30), $"FPS: {_fps:F0}", style);
        }

        if (_showMenu)
        {
            GUILayout.BeginArea(new Rect(10, 50, 250, 400));
            GUILayout.Box("DEBUG MENU");

            _showFPS = GUILayout.Toggle(_showFPS, "Show FPS (F1)");
            _showPhysics = GUILayout.Toggle(_showPhysics, "Show Physics (F2)");
            _godMode = GUILayout.Toggle(_godMode, "God Mode (F3)");

            if (GUILayout.Button("Kill All Enemies"))
                KillAllEnemies();
            if (GUILayout.Button("Give All Weapons"))
                GiveAllWeapons();
            if (GUILayout.Button("Teleport to Checkpoint"))
                TeleportToCheckpoint();

            GUILayout.Label($"Time Scale: {Time.timeScale:F1}");
            Time.timeScale = GUILayout.HorizontalSlider(Time.timeScale, 0f, 3f);

            GUILayout.EndArea();
        }
    }

    #endif

    private void KillAllEnemies() { /* implementation */ }
    private void GiveAllWeapons() { /* implementation */ }
    private void TeleportToCheckpoint() { /* implementation */ }
}
```

### On-Screen Stats Display

```csharp
public class StatsOverlay : MonoBehaviour
{
    [SerializeField] private bool showStats = true;

    private void OnGUI()
    {
        if (!showStats) return;

        float y = 10;
        float lineHeight = 20;

        DrawStat(ref y, lineHeight, "FPS", (1f / Time.unscaledDeltaTime).ToString("F0"));
        DrawStat(ref y, lineHeight, "Delta", (Time.deltaTime * 1000f).ToString("F1") + "ms");
        DrawStat(ref y, lineHeight, "Objects", FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length.ToString());
        DrawStat(ref y, lineHeight, "Rigidbodies", FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length.ToString());
        DrawStat(ref y, lineHeight, "Heap", (GC.GetTotalMemory(false) / (1024 * 1024)).ToString("F1") + " MB");
    }

    private void DrawStat(ref float y, float h, string label, string value)
    {
        GUI.Label(new Rect(Screen.width - 200, y, 90, h), label);
        GUI.Label(new Rect(Screen.width - 100, y, 90, h), value);
        y += h;
    }
}
```

## Common Debugging Issues

### Null Reference on Destroyed Object

```csharp
// WRONG — object may be destroyed between frames
private Enemy _target;
private void Update()
{
    float dist = Vector3.Distance(transform.position, _target.transform.position);
    // MissingReferenceException if _target was Destroy'd
}

// CORRECT — Unity overloads == for destroyed objects
private void Update()
{
    if (_target == null) { FindNewTarget(); return; }
    float dist = Vector3.Distance(transform.position, _target.transform.position);
}

// ALSO CORRECT — TryGetComponent pattern
if (_target != null && _target.TryGetComponent<Health>(out var health))
{
    health.TakeDamage(10);
}
```

### Coroutine on Disabled Object

```csharp
// Coroutines STOP when the MonoBehaviour or GameObject is disabled
// They do NOT resume when re-enabled

// WRONG
private void OnDisable()
{
    StartCoroutine(FadeOut()); // This coroutine will never run
}

// SOLUTION — use a separate always-active manager to run coroutines
GameManager.Instance.StartCoroutine(FadeOut());
```

### Awake / Start Order Issues

```csharp
// Awake order between scripts is NOT guaranteed (unless you set Script Execution Order)
// Prefer explicit initialization over relying on Awake order

// FRAGILE
public class UIManager : MonoBehaviour
{
    private void Awake() { Instance = this; }
}
public class HUD : MonoBehaviour
{
    private void Awake() { UIManager.Instance.RegisterHUD(this); } // might be null!
}

// ROBUST — use Awake for self-init, Start for cross-references
public class HUD : MonoBehaviour
{
    private void Start() { UIManager.Instance.RegisterHUD(this); }
}
```

### IL2CPP Debugging

For builds using IL2CPP (default for most platforms):

1. Enable "Development Build" and "Script Debugging" in Build Settings
2. Build and run
3. In Unity, Debug > Attach Unity Debugger > select the running player
4. Breakpoints work in Visual Studio / Rider attached to the editor or player

**Tip:** IL2CPP strips unused code. If reflection-accessed types disappear, use `[Preserve]` attribute or a `link.xml` file.
