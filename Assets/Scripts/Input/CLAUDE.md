# Assets/Scripts/Input/

Input abstraction layer for the RC buggy. Supports keyboard (WASD) and gamepad (Xbox/PS triggers + left stick).

## Files

| File | Role |
|------|------|
| `IVehicleInput.cs` | Interface for swappable input sources (player, AI, replay, network) |
| `InputMath.cs` | Pure math: deadzone remapping, symmetric deadzone, steering curves, input merging |
| `RCInput.cs` | MonoBehaviour input provider: polls keyboard + gamepad, auto-detects controller |
| `R8EOX.Input.asmdef` | Assembly definition for the Input system |

## Architecture

- **RCInput** implements `IVehicleInput` and is the primary player input source
- **60-frame grace period** on startup suppresses ghost gamepad axis values
- **No combined trigger mode** -- only separate triggers (modern Xbox/PS standard)
- **Deadzone handling** is done in code via `InputMath`, not via InputManager dead values
- **InputManager axes** use near-zero dead (0.001) so `GetAxisRaw` returns true raw values

## InputManager Axis Mapping

| Axis Name | Unity Axis | Type | Purpose |
|-----------|-----------|------|---------|
| `Horizontal` | 0 | Key/Button | Keyboard arrows + A/D |
| `GamepadSteerX` | 0 | Joystick | Left stick X for steering |
| `GamepadThrottle` | 9 | Joystick | Right trigger for throttle |
| `GamepadBrake` | 8 | Joystick | Left trigger for brake |

## Namespace Warning

Use `UnityEngine.Input` explicitly (not bare `Input`) inside the `R8EOX.Input` namespace to avoid collision.

## Relevant Skills

- `.agents/skills/unity-csharp-mastery/SKILL.md` -- C# patterns and conventions
- `.agents/skills/unity-testing-patterns/SKILL.md` -- TDD with Unity Test Framework
