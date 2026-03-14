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

### Phantom Input Defense

Multiple defense layers prevent phantom gamepad values from producing unintended vehicle movement:

1. **TriggerDetector variance rejection** — During the `Combined` confirmation window, the detector requires `(max - min) > k_VarianceThreshold` (0.02). A constant phantom axis (e.g., always 0.0039) has zero variance and is rejected.
2. **Mode gating** — During `Detecting` and `None` modes, `RCInput` returns 0 for throttle/brake. Only after a mode is confirmed does input flow through.
3. **FilterGamepadSteering** — `InputMath.FilterGamepadSteering` applies deadzone only when a gamepad is detected, preventing small phantom horizontal values from producing steering.

**Principle:** Detection observes, never drives. The detection phase gathers data about axis behavior but never allows that data to reach the vehicle.

### Key InputMath Methods

| Method | Purpose |
|--------|---------|
| `CombinedTriggerThrottle` | Extracts throttle from a combined trigger axis (positive half) with deadzone |
| `CombinedTriggerBrake` | Extracts brake from a combined trigger axis (negative half) with deadzone |
| `FilterGamepadSteering` | Applies deadzone to horizontal input only when gamepad is detected |

## Debug Tools

- **`Assets/InputDiagnostics.cs`** — Runtime debug MonoBehaviour that logs raw input values, trigger mode, and processed outputs. Attach to any GameObject during debugging sessions.

## Relevant Skills

- **`unity-input-system`** — Input handling patterns and best practices
- **`unity-csharp-mastery`** — C# conventions used in pure logic extraction
