# Control Schemes

> Part of the `unity-input-system` skill. See [SKILL.md](SKILL.md) for the overview.

## Control Schemes

Define which devices constitute a valid input setup:

```
Keyboard+Mouse:
  - <Keyboard> (required)
  - <Mouse> (required)

Gamepad:
  - <Gamepad> (required)

Touch:
  - <Touchscreen> (required)
```

Automatic switching: when `PlayerInput` is set to **Auto-Switch**, changing to a different device mid-game switches the active scheme and triggers `onControlsChanged`.

```csharp
public class ControlSchemeDisplay : MonoBehaviour
{
    PlayerInput _playerInput;

    void OnEnable()
    {
        _playerInput = GetComponent<PlayerInput>();
        _playerInput.onControlsChanged += OnControlsChanged;
    }

    void OnControlsChanged(PlayerInput input)
    {
        string scheme = input.currentControlScheme;
        // Update UI prompts: "Press A" vs "Press Space"
        Debug.Log($"Switched to: {scheme}");
    }
}
```

## Binding Composites

Combine multiple controls into a single logical value:

### 2D Vector (WASD)

```
Move (Value, Vector2):
  +-- 2D Vector Composite
        Up:    <Keyboard>/w
        Down:  <Keyboard>/s
        Left:  <Keyboard>/a
        Right: <Keyboard>/d
```

Mode options: **Digital** (normalized, 8-direction), **Digital Normalized** (default), **Analog** (raw).

### 1D Axis

```
Zoom (Value, float):
  +-- 1D Axis Composite
        Negative: <Keyboard>/minus
        Positive: <Keyboard>/equals
```

### Button With Modifier

```
Sprint (Button):
  +-- Button With One Modifier
        Modifier: <Keyboard>/leftShift
        Button:   <Keyboard>/w
```

## Reading Input

Three approaches, from simplest to most flexible:

### 1. PlayerInput Component (Inspector-Driven)

Attach `PlayerInput` to a GameObject. Set behavior mode:

| Mode | How It Works |
|------|-------------|
| Send Messages | Calls `OnMove(InputValue)`, `OnFire(InputValue)` on same GameObject |
| Broadcast Messages | Same but searches children too |
| Invoke Unity Events | Drag methods in the Inspector like button onClick |
| Invoke C# Events | Subscribe in code to `onActionTriggered` |

```csharp
// Send Messages mode -- method name = "On" + action name
public class PlayerController : MonoBehaviour
{
    Vector2 _moveInput;

    void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    void OnFire(InputValue value)
    {
        if (value.isPressed) Shoot();
    }
}
```

### 2. Generated C# Class

Enable **Generate C# Class** in the Input Actions asset inspector. This creates a strongly-typed wrapper:

```csharp
public class PlayerController : MonoBehaviour
{
    GameInputActions _input; // Generated class name matches asset name

    void Awake()
    {
        _input = new GameInputActions();
    }

    void OnEnable()
    {
        _input.Player.Enable();
        _input.Player.Fire.performed += OnFire;
        _input.Player.Fire.canceled += OnFireReleased;
    }

    void OnDisable()
    {
        _input.Player.Fire.performed -= OnFire;
        _input.Player.Fire.canceled -= OnFireReleased;
        _input.Player.Disable();
    }

    void Update()
    {
        Vector2 move = _input.Player.Move.ReadValue<Vector2>();
        transform.Translate(new Vector3(move.x, 0, move.y) * Time.deltaTime * 5f);
    }

    void OnFire(InputAction.CallbackContext ctx) { Shoot(); }
    void OnFireReleased(InputAction.CallbackContext ctx) { StopShooting(); }
}
```

### 3. Direct InputAction References

Reference individual actions without the full asset:

```csharp
public class SimpleController : MonoBehaviour
{
    [SerializeField] InputActionReference _moveAction;
    [SerializeField] InputActionReference _fireAction;

    void OnEnable()
    {
        _moveAction.action.Enable();
        _fireAction.action.Enable();
        _fireAction.action.performed += ctx => Shoot();
    }

    void OnDisable()
    {
        _moveAction.action.Disable();
        _fireAction.action.Disable();
    }

    void Update()
    {
        Vector2 move = _moveAction.action.ReadValue<Vector2>();
        // ...
    }
}
```

