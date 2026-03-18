# Assets/

Unity project Assets folder. All game content, scripts, and test suites live here.

## Subdirectories

| Dir | Purpose |
|-----|---------|
| `Scripts/` | Game scripts organized by system (Vehicle, Input, Camera, etc.) |
| `Scenes/` | Unity scene files (TestTrack, OutpostTrack) |
| `Terrain/` | Terrain data assets and layers |
| `Tests/` | Unity Test Framework suites (EditMode) |
| `Prefabs/` | Reusable prefab assets (RCBuggy) |
| `Plugins/` | Third-party plugin assemblies |

## Root Files

| File | Purpose |
|------|---------|
| `InputDiagnostics.cs` | Runtime debug tool — logs raw input values, trigger mode, and processed outputs for phantom input diagnosis |

## Assembly Definitions

Each script subdirectory has its own `.asmdef` to enforce dependency boundaries:
- `R8EOX.Core` — shared types, no dependencies on other game assemblies
- `R8EOX.Vehicle` — references Core
- `R8EOX.Input` — references Core
- `R8EOX.Camera` — references Unity.InputSystem
- `R8EOX.Debug` — references Vehicle
- `R8EOX.Track` — references Core
- `R8EOX.Editor` — editor-only platform

## Relevant Skills

- **`unity-project-foundations`** — Project structure and assembly definition strategy
- **`unity-scene-management`** — Scene organization and loading patterns
