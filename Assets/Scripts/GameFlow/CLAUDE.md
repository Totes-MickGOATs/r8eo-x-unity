# GameFlow/

Game flow state management, navigation, and scene orchestration.

## Assembly

- **Name:** `R8EOX.GameFlow`
- **Namespace:** `R8EOX.GameFlow`
- **Dependencies:** `R8EOX.Core`

## Files

| File | Role |
|------|------|
| `GameState.cs` | Enum: Boot, Splash, MainMenu, ModeSelect, CarSelect, TrackSelect, Loading, Playing, Paused, Results |
| `SessionConfig.cs` | Immutable session configuration (mode, track, car, laps, AI difficulty) |
| `NavigationStack.cs` | Stack-based screen navigation with breadcrumbs and events |
| `GameFlowStateMachine.cs` | State machine with validated transitions between game states |
| `SceneEntry.cs` | Data class for scene registry entries (id, display name, path) |
| `SceneRegistry.cs` | ScriptableObject lookup table for registered scenes |
| `IGameFlowService.cs` | Contract interface for game flow coordination (state, session, transitions) |
| `IScreenNavigator.cs` | Contract interface for screen navigation (push, pop, breadcrumbs) |
| `ScreenId.cs` | String constants for well-known screen identifiers |
| `GameFlowManager.cs` | MonoBehaviour singleton implementing IGameFlowService + IScreenNavigator |
| `SceneBootstrapper.cs` | Standalone scene launcher — creates GameFlowManager if missing |

## Contracts

- **`IGameFlowService`** — consumed by UI and scene systems for state queries and transitions
- **`IScreenNavigator`** — consumed by UI screens for navigation (push/pop stack)
- **`GameFlowManager`** implements both interfaces and owns the state machine + navigation stack
- **`SceneBootstrapper`** — place in any gameplay scene; if launched standalone, creates a minimal manager and fast-forwards to Playing

## Design

- **Pure C# where possible** — NavigationStack, SessionConfig, GameFlowStateMachine have no Unity dependencies beyond ScriptableObject
- **Transition validation** — GameFlowStateMachine enforces a strict transition graph; invalid transitions throw
- **Events** — NavigationStack and GameFlowStateMachine fire C# events for observers
- **ScriptableObject registry** — SceneRegistry is an asset-based lookup, designer-editable in Inspector

## Relevant Skills

- `.agents/skills/unity-scene-management/SKILL.md` — Scene loading, bootstrapper pattern
- `.agents/skills/unity-state-machines/SKILL.md` — State machine patterns
- `.agents/skills/unity-architecture-patterns/SKILL.md` — Observer, MVP, SO patterns
- `.agents/skills/unity-scriptable-objects/SKILL.md` — SO event channels, config data
- `.agents/skills/unity-ui-toolkit/SKILL.md` — UI Toolkit for menu screens (Phase B)
- `.agents/skills/unity-racing-ui/SKILL.md` — HUD and racing UI patterns (Phase B)
