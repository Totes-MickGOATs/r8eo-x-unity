# UI/

UI screen management, theming, canvas layers, and screen lifecycle.

## Assembly

- **Name:** `R8EOX.UI`
- **Namespace:** `R8EOX.UI`
- **Dependencies:** `R8EOX.Core`, `R8EOX.GameFlow`

## Files

| File | Role |
|------|------|
| `IScreen.cs` | Screen lifecycle contract (Enter, AnimateIn, AnimateOut, Exit) |
| `ScreenRegistry.cs` | ScriptableObject mapping screen IDs to prefabs |
| `UIManager.cs` | Screen lifecycle coordinator, canvas layers, overlay stack |
| `ThemeConstants.cs` | Design tokens: colors, font sizes, animation durations, sort orders |

## Contracts

### Adding a New Screen

1. Create a prefab with a MonoBehaviour implementing `IScreen`
2. Add an entry to the `ScreenRegistry` ScriptableObject asset (Inspector)
3. Use `ScreenId` constants (add new constant if needed in `R8EOX.GameFlow.ScreenId`)
4. The screen receives `Enter(data)` → `AnimateIn()` → user interaction → `AnimateOut()` → `Exit()`
5. Navigation: fire `OnNavigationRequested` event with target screen ID

### Adding a New Overlay

Same as above, but shown via `UIManager.PushOverlay()` instead of `ShowScreen()`.
Overlays stack (LIFO) and are popped with `UIManager.PopOverlay()`.

## Relevant Skills

- `.agents/skills/unity-ui-toolkit/SKILL.md` — UXML/USS patterns, UI Toolkit API
- `.agents/skills/unity-ui-design/SKILL.md` — Menu architecture, settings patterns
- `.agents/skills/unity-racing-ui/SKILL.md` — HUD layout, three-canvas optimization
- `.agents/skills/unity-architecture-patterns/SKILL.md` — Observer, MVP patterns
