# Input Actions Asset

> Part of the `unity-input-system` skill. See [SKILL.md](SKILL.md) for the overview.

## Input Actions Asset

The Input Actions asset (`.inputactions`) is the central configuration file. Create via **Assets > Create > Input Actions**.

### Structure

```
Input Actions Asset
  +-- Action Map (e.g., "Player", "UI", "Vehicle")
  |     +-- Action (e.g., "Move", "Fire", "Look")
  |     |     +-- Binding (e.g., "<Gamepad>/leftStick", "<Keyboard>/w")
  |     |     +-- Binding Composite (e.g., 2D Vector: WASD)
  |     +-- Action ...
  +-- Action Map ...
  +-- Control Scheme (e.g., "Keyboard+Mouse", "Gamepad")
```

### Action Maps

Group related actions by context:

| Action Map | Purpose | Example Actions |
|------------|---------|-----------------|
| Player | Gameplay movement/combat | Move, Look, Fire, Jump, Interact |
| UI | Menu navigation | Navigate, Submit, Cancel, ScrollWheel |
| Vehicle | Driving-specific | Throttle, Brake, Steer, Handbrake |

Switch maps at runtime:

```csharp
playerInput.SwitchCurrentActionMap("UI");
// Or directly:
inputActions.Player.Disable();
inputActions.UI.Enable();
```

