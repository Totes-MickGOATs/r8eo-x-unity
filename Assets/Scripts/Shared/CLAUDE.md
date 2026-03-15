# Assets/Scripts/Shared/

Runtime-safe utility classes shared between game code and editor tooling.

## Files

| File | Role |
|------|------|
| `UnitConversion.cs` | Unit conversion helpers (speed, angle, spring rate, force) — no UnityEditor dependency |
| `R8EOX.Shared.asmdef` | Assembly definition — all platforms, no editor-only restriction |

## Notes

- No `UnityEditor` imports allowed here — these classes must be usable at runtime
- The custom inspector editors (`Assets/Scripts/Editor/`) reference these for unit display
- The in-game tuning UI will reference these same methods for consistent unit display
