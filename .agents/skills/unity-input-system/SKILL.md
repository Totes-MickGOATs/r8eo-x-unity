---
name: unity-input-system
description: Unity Input System
---


# Unity Input System

Use this skill when configuring input actions, reading player input, implementing rebinding, or migrating from Unity's legacy Input Manager to the New Input System.

## Package Installation and Setup

Install via Package Manager or manifest:

```json
// Packages/manifest.json
"com.unity.inputsystem": "1.8.2"
```

After installation, Unity prompts to switch the active input handling:

- **Edit > Project Settings > Player > Active Input Handling** -- set to **Input System Package (New)** or **Both** during migration
- Restart the editor after changing this setting

The new system is event-driven rather than poll-based. Instead of checking `Input.GetKey()` every frame, you define actions and respond to callbacks.

## Action Types

Each action has a type that determines its behavior:

| Type | When to Use | Example |
|------|------------|---------|
| **Value** | Continuous input, tracks state changes | Movement stick, mouse delta, triggers |
| **Button** | Discrete press/release, has default Press interaction | Fire, Jump, Interact |
| **PassThrough** | Raw input, no conflict resolution between devices | When you need every device's input simultaneously |

**Value** actions perform conflict resolution -- if multiple controls are bound, only the most actuated one drives the action. **PassThrough** skips this and forwards all input.

```csharp
// In the .inputactions editor:
// Move: Type = Value, Control Type = Vector2
// Fire: Type = Button
// AnyDeviceInput: Type = PassThrough
```

## Input Action Phases

Every action fires callbacks at specific phases:

| Phase | When | Use For |
|-------|------|---------|
| **Started** | Control actuated above default | Charge-up begins, button touch |
| **Performed** | Interaction completed | Fire bullet, jump, confirm |
| **Canceled** | Control released / interaction failed | Release charge, stop aiming |

```csharp
// Charge attack pattern
_input.Player.HeavyAttack.started += ctx => StartCharging();
_input.Player.HeavyAttack.performed += ctx => ReleaseCharge(); // Hold interaction completed
_input.Player.HeavyAttack.canceled += ctx => CancelCharge();   // Released too early
```

## Input Debugger

**Window > Analysis > Input Debugger** shows:

- All connected devices and their controls in real-time
- Active actions and their current values
- Remote device connections (for debugging on-device builds)
- Event traces for diagnosing input issues

Enable **Input Debug Mode** in Project Settings > Input System for additional diagnostics.

## Migration from Legacy Input

| Legacy (UnityEngine.Input) | New Input System |
|---------------------------|------------------|
| `Input.GetAxis("Horizontal")` | `moveAction.ReadValue<Vector2>().x` |
| `Input.GetButtonDown("Fire1")` | `fireAction.performed += ctx => ...` |
| `Input.GetKey(KeyCode.Space)` | `jumpAction.IsPressed()` |
| `Input.GetMouseButtonDown(0)` | Bind `<Mouse>/leftButton` to action |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` |
| `Input.GetJoystickNames()` | `Gamepad.all`, `InputSystem.devices` |
| `Input.GetAxisRaw("Vertical")` | Set composite mode to Digital |

### Step-by-Step Migration

1. Set Active Input Handling to **Both** during migration
2. Create Input Actions asset mirroring your old axes/buttons
3. Replace `Input.GetAxis()` calls one script at a time
4. Test each script after conversion
5. Once all scripts converted, switch to **Input System Package (New)** only
6. Remove `using UnityEngine.InputSystem;` is the new namespace (not `UnityEngine.Input`)



## Topic Pages

- [Input Actions Asset](skill-input-actions-asset.md)
- [Control Schemes](skill-control-schemes.md)
- [Interactions](skill-interactions.md)
- [Common Patterns](skill-common-patterns.md)

