# Assets/Scripts/Debug/

Runtime debug overlays for vehicle development and tuning.

## Files

| File | Role |
|------|------|
| `TelemetryHUD.cs` | On-screen telemetry overlay (speed, forces, wheel state). Toggle with F2. |
| `TuningPanel.cs` | Runtime parameter tuning panel with sliders for all vehicle physics. Toggle with Tab. |
| `R8EOX.Debug.asmdef` | Assembly definition referencing R8EOX.Vehicle |

## Conventions

- Namespace: `R8EOX.Debug`
- All overlays use `OnGUI` for immediate-mode rendering
- Each panel toggles with a unique key (F2, Tab, etc.)
- Panels reference `RCCar` via `[SerializeField]`
