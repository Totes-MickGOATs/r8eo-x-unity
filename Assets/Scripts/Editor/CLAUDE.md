# Assets/Scripts/Editor/

Editor-only scripts: menu items, debug tools, and scene setup automation. These scripts only run inside the Unity Editor and are excluded from builds.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `SceneSetup.cs` | `SceneSetup` | Menu item to create/configure the TestTrack scene with terrain, lighting, and vehicle |
| `OutpostTrackSetup.cs` | `OutpostTrackSetup` | Menu item to create/configure the OutpostTrack scene |
| `TerrainDebug.cs` | `TerrainDebug` | Editor terrain debug visualization utilities |
| `TerrainSpawnCheck.cs` | `TerrainSpawnCheck` | Editor tool to validate spawn point placement on terrain |
| `RCCarEditor.cs` | `RCCarEditor` | Custom inspector for RCCar — foldout groups, range sliders, preset warning, unit-converted fields (km/h, kgf, deg, N/mm) |
| `DrivetrainEditor.cs` | `DrivetrainEditor` | Custom inspector for Drivetrain — hides AWD sections in RWD, disables preload when not BallDiff |
| `R8EOX.Editor.asmdef` | — | Assembly definition (Editor-only platform) |

## Conventions

- Namespace: `R8EOX.Editor`
- All scripts use `[MenuItem]` attributes for Unity menu integration
- Assembly definition targets Editor platform only — excluded from player builds
- `RCCarEditor` depends on `R8EOX.Shared` (`Assets/Scripts/Shared/`) for unit conversion helpers — no UnityEditor code lives in Shared

## Relevant Skills

- **`unity-editor-scripting`** — Custom editor tools, menu items, and inspector extensions
