# Common Patterns

> Part of the `unity-input-system` skill. See [SKILL.md](SKILL.md) for the overview.

## Common Patterns

### Input Buffer (Coyote Time)

```csharp
float _jumpBufferTime = 0.15f;
float _jumpBufferTimer;

void OnJump(InputAction.CallbackContext ctx)
{
    if (ctx.performed) _jumpBufferTimer = _jumpBufferTime;
}

void Update()
{
    _jumpBufferTimer -= Time.deltaTime;
    if (_jumpBufferTimer > 0 && IsGrounded())
    {
        PerformJump();
        _jumpBufferTimer = 0;
    }
}
```

### Dead Zone Configuration

Set dead zones per binding in the Input Actions editor, or in code:

```csharp
// Stick dead zones (already handled by default processors)
// Add processor string to binding: "stickDeadzone(min=0.125,max=0.925)"

// Or apply at runtime:
InputSystem.settings.defaultDeadzoneMin = 0.125f;
InputSystem.settings.defaultDeadzoneMax = 0.925f;
```

### Action Map Switching for Game States

```csharp
public void EnterMenuState()
{
    _input.Player.Disable();
    _input.UI.Enable();
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}

public void EnterGameplayState()
{
    _input.UI.Disable();
    _input.Player.Enable();
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}
```
