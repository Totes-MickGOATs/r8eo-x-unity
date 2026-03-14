# Assets/Scripts/

Game scripts organized by system. Each subdirectory is a separate assembly with its own namespace.

## Subdirectories

| Dir | Namespace | Assembly | Role |
|-----|-----------|----------|------|
| `Vehicle/` | `R8EOX.Vehicle` | `R8EOX.Vehicle` | RC car controller, raycast wheels, drivetrain, air physics |
| `Input/` | `R8EOX.Input` | `R8EOX.Input` | Input abstraction: keyboard + gamepad |
| `Camera/` | `R8EOX.Camera` | `R8EOX.Camera` | Chase camera system |
| `Debug/` | `R8EOX.Debug` | `R8EOX.Debug` | Telemetry HUD overlay |
| `Core/` | `R8EOX.Core` | `R8EOX.Core` | Shared types: SurfaceType enum, SurfaceConfig |
| `Track/` | `R8EOX.Track` | `R8EOX.Track` | Track systems: surface zones |
| `GameFlow/` | `R8EOX.GameFlow` | `R8EOX.GameFlow` | Game flow state machine, navigation stack, session config, scene registry |
| `Editor/` | `R8EOX.Editor` | `R8EOX.Editor` | Editor-only scene/prefab builders |

## Conventions

- **Namespaces:** Every script declares `namespace R8EOX.{Folder}`
- **One class per file:** File name matches class name (PascalCase)
- **`[SerializeField]` + `[Tooltip]`:** All inspector fields use both attributes
- **`_camelCase`:** All private fields use underscore prefix
- **`k_PascalCase`:** All constants use `k_` prefix
- **Assembly definitions:** Each system folder has its own `.asmdef`
- **Full coding standards:** `.ai/knowledge/architecture/coding-standards.md`

## Relevant Skills

- **`unity-csharp-mastery`** — C# patterns and conventions
- **`unity-composition`** — Component architecture
- **`unity-testing-patterns`** — TDD with Unity Test Framework
- **`unity-input-system`** — Input handling patterns (Input/)
- **`unity-physics-3d`** — Vehicle physics and raycast wheels (Vehicle/)
- **`unity-editor-scripting`** — Custom editor tools and menu items (Editor/)