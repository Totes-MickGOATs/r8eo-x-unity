# Assets/Scripts/Input/

Input processing pipeline: raw Unity input axes to vehicle commands (throttle, brake, steer).

## Files

| File | Class | Purpose |
|------|-------|---------|
| `IVehicleInput.cs` | `IVehicleInput` | Interface for swappable input providers (player, AI, replay, network) |
| `RCInput.cs` | `RCInput` | MonoBehaviour implementing `IVehicleInput` via Unity's legacy Input Manager (keyboard + gamepad) |
| `InputMath.cs` | `InputMath` | Pure math: deadzone remapping and steering curve application |
| `InputGuard.cs` | `InputGuard` | Startup suppression — zeroes input for the first few frames to avoid stale axis values |
| `TriggerDetector.cs` | `TriggerDetector` | Gamepad trigger mode detection (Separate vs Combined vs None) with grace period and confirmation |
| `R8EOX.Input.asmdef` | — | Assembly definition for the Input system |

## Architecture

- `RCInput` reads raw axes each frame, applies `InputGuard` suppression, detects trigger mode via `TriggerDetector`, and processes values through `InputMath`
- Pure logic classes (`InputMath`, `InputGuard`, `TriggerDetector`) have no Unity lifecycle dependency for testability
- Namespace: `R8EOX.Input`

## Relevant Skills

- **`unity-input-system`** — Input handling patterns and best practices
- **`unity-csharp-mastery`** — C# conventions used in pure logic extraction
