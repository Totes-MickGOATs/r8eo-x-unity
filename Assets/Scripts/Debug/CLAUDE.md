# Assets/Scripts/Debug/

Runtime debug overlays for vehicle development and tuning.

## Files

| File | Role |
|------|------|
| `TelemetryHUD.cs` | On-screen telemetry overlay (speed, forces, wheel state). Toggle with F2. |
| `TuningPanel.cs` | Runtime parameter tuning panel with sliders for all vehicle physics. Toggle with Tab. |
| `ContractDebugger.cs` | Runtime chain-of-custody assertions: validates input/vehicle/wheel/observable contracts every frame. Stripped in release builds. |
| `R8EOX.Debug.asmdef` | Assembly definition referencing R8EOX.Vehicle and R8EOX.Input |

## Conventions

- Namespace: `R8EOX.Debug`
- All overlays use `OnGUI` for immediate-mode rendering
- Each panel toggles with a unique key (F2, Tab, etc.)
- Panels reference `RCCar` via `[SerializeField]`
- Contract assertions wrapped in `#if UNITY_EDITOR || DEBUG` for release stripping
- ContractDebugger observes only (Signal Up pattern) -- never modifies vehicle state

## Relevant Skills

- **`unity-debugging-profiling`** — Runtime debug overlays and profiling tools
