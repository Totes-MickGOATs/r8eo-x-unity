# Interactions

> Part of the `unity-input-system` skill. See [SKILL.md](SKILL.md) for the overview.

## Interactions

Modify when `performed` fires:

| Interaction | Behavior | Parameters |
|-------------|----------|------------|
| **Press** | On press, release, or both | `pressPoint` |
| **Hold** | Must hold for duration | `duration` (default 0.4s) |
| **Tap** | Press and release within time | `duration` (default 0.2s) |
| **SlowTap** | Press and release after minimum time | `duration` (default 0.5s) |
| **MultiTap** | Multiple taps in succession | `tapCount`, `tapDelay`, `tapTime` |

Set interactions per-action or per-binding in the Input Actions editor.

### Custom Interaction

```csharp
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class DoublePressInteraction : IInputInteraction
{
    public float tapTime = 0.3f;

    static DoublePressInteraction()
    {
        InputSystem.RegisterInteraction<DoublePressInteraction>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() { } // Ensure static constructor runs

    public void Process(ref InputInteractionContext context)
    {
        // Custom state machine logic
        // Call context.Started(), context.Performed(), context.Canceled()
    }

    public void Reset() { }
}
```

## Rebinding

Runtime rebinding lets players customize controls:

```csharp
public class RebindUI : MonoBehaviour
{
    [SerializeField] InputActionReference _actionToRebind;
    [SerializeField] int _bindingIndex = 0;
    [SerializeField] TMP_Text _bindingText;
    [SerializeField] GameObject _rebindOverlay;

    InputActionRebindingExtensions.RebindingOperation _rebindOp;

    public void StartRebind()
    {
        _actionToRebind.action.Disable();
        _rebindOverlay.SetActive(true);

        _rebindOp = _actionToRebind.action.PerformInteractiveRebinding(_bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Keyboard>/escape")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => RebindComplete())
            .OnCancel(op => RebindCanceled())
            .Start();
    }

    void RebindComplete()
    {
        _rebindOp.Dispose();
        _rebindOverlay.SetActive(false);
        _actionToRebind.action.Enable();
        _bindingText.text = InputControlPath.ToHumanReadableString(
            _actionToRebind.action.bindings[_bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        // Save: store overrides as JSON
        string overrides = _actionToRebind.action.actionMap.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("InputOverrides", overrides);
    }

    void RebindCanceled()
    {
        _rebindOp.Dispose();
        _rebindOverlay.SetActive(false);
        _actionToRebind.action.Enable();
    }
}

// Loading saved overrides on startup:
string json = PlayerPrefs.GetString("InputOverrides", string.Empty);
if (!string.IsNullOrEmpty(json))
    inputActions.asset.LoadBindingOverridesFromJson(json);
```

## Multiple Players

`PlayerInputManager` handles player joining for local multiplayer:

```csharp
// Setup in Inspector:
// - PlayerInputManager component on a manager GameObject
// - Join Behavior: Join Players When Button Is Pressed
// - Player Prefab: prefab with PlayerInput component
// - Max Player Count: 4
// - Split Screen: enable if needed

public class MultiplayerManager : MonoBehaviour
{
    void OnEnable()
    {
        PlayerInputManager.instance.onPlayerJoined += OnPlayerJoined;
        PlayerInputManager.instance.onPlayerLeft += OnPlayerLeft;
    }

    void OnPlayerJoined(PlayerInput player)
    {
        Debug.Log($"Player {player.playerIndex} joined with {player.currentControlScheme}");
        // Assign spawn point, team, color, etc.
    }

    void OnPlayerLeft(PlayerInput player)
    {
        Debug.Log($"Player {player.playerIndex} left");
    }
}
```

Split-screen is configured on `PlayerInputManager`: set screen division (horizontal/vertical), borders, and per-player camera assignment happens automatically.

