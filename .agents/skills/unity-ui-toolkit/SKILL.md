---
name: unity-ui-toolkit
description: Unity UI Toolkit
---


# Unity UI Toolkit

Use this skill when building UI with Unity's UI Toolkit system, including UXML structure, USS styling, C# bindings, custom inspectors, or runtime game UI panels.

## When to Use UI Toolkit vs UGUI

| Use Case | Recommended System |
|----------|-------------------|
| Menus, settings, HUD overlays | **UI Toolkit** — cleaner styling, better tooling |
| World-space UI (health bars above enemies) | **UGUI** — native 3D canvas support |
| Complex runtime animations on UI | **UGUI** — more mature DOTween/animation integration |
| Editor custom inspectors and windows | **UI Toolkit** — this is the primary target |
| Data-heavy lists (inventory, leaderboards) | **UI Toolkit** — virtualized ListView |

## Runtime UI — UIDocument and PanelSettings

### UIDocument Component

Attach to a GameObject to display UI Toolkit content at runtime:

- **Source Asset** — the UXML file to render
- **Panel Settings** — shared rendering configuration
- **Sort Order** — higher values render on top (like Canvas sortingOrder)

### PanelSettings Asset

Create via Assets > Create > UI Toolkit > Panel Settings Asset:

- **Theme Style Sheet** — default USS applied to all documents using this panel
- **Scale Mode** — Constant Pixel Size, Scale With Screen Size, Constant Physical Size
- **Reference Resolution** — base resolution for Scale With Screen Size (e.g., 1920x1080)
- **Match** — 0 = match width, 1 = match height, 0.5 = blend (same concept as UGUI CanvasScaler)

Multiple UIDocuments can share one PanelSettings (same render panel) or use separate ones (independent panels).

## Runtime Theme Switching

```csharp
public class ThemeManager : MonoBehaviour
{
    [SerializeField] private PanelSettings panelSettings;
    [SerializeField] private ThemeStyleSheet lightTheme;
    [SerializeField] private ThemeStyleSheet darkTheme;

    public void SetDarkMode(bool dark)
    {
        panelSettings.themeStyleSheet = dark ? darkTheme : lightTheme;
    }
}
```

Alternatively, swap USS variables at runtime:

```csharp
// Override a custom property on the root
var root = uiDocument.rootVisualElement;
root.style.SetCustomProperty("--bg-primary", new StyleColor(Color.black));
```

## Programmatic Animation

```csharp
// Experimental animation API
element.experimental.animation
    .Start(new StyleValues { opacity = 0, top = -50 },
           new StyleValues { opacity = 1, top = 0 },
           300)
    .Ease(Easing.OutCubic)
    .OnCompleted(() => Debug.Log("Animation done"));
```

## UI Toolkit Debugger

Open via **Window > UI Toolkit > Debugger**:

- **Pick Element** — click in Game view to select and inspect any element
- **Style Inspector** — see computed styles, which USS rules apply, overrides
- **Hierarchy** — full visual tree, similar to browser DevTools
- **Layout** — see flex properties, margins, padding, borders visually

This is the most powerful tool for debugging layout issues. Use it before guessing at USS changes.



## Topic Pages

- [UXML — Structure](skill-uxml-structure.md)
- [C# Binding](skill-c-binding.md)
- [Data Binding](skill-data-binding.md)

