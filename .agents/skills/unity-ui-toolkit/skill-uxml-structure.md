# UXML — Structure

> Part of the `unity-ui-toolkit` skill. See [SKILL.md](SKILL.md) for the overview.

## UXML — Structure

UXML defines the visual hierarchy declaratively. Think of it as HTML for Unity UI.

```xml
<!-- Assets/UI/MainMenu.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements">
    <ui:Template name="SettingsPanel" src="project://database/Assets/UI/SettingsPanel.uxml" />

    <ui:VisualElement name="root" class="main-menu">
        <ui:VisualElement name="header" class="header-bar">
            <ui:Label name="title" text="My Game" class="title-text" />
        </ui:VisualElement>

        <ui:VisualElement name="button-container" class="button-column">
            <ui:Button name="btn-play" text="Play" class="menu-button primary" />
            <ui:Button name="btn-settings" text="Settings" class="menu-button" />
            <ui:Button name="btn-quit" text="Quit" class="menu-button" />
        </ui:VisualElement>

        <!-- Reuse a template -->
        <ui:Instance template="SettingsPanel" name="settings-panel" />
    </ui:VisualElement>
</ui:UXML>
```

### Key Elements

| Element | Purpose |
|---------|---------|
| `VisualElement` | Generic container (like `<div>`) |
| `Label` | Text display |
| `Button` | Clickable button |
| `TextField` | Text input |
| `Toggle` | Checkbox |
| `Slider` / `SliderInt` | Range input |
| `DropdownField` | Dropdown selector |
| `ListView` | Virtualized scrolling list |
| `ScrollView` | Scrollable container |
| `Foldout` | Collapsible section |
| `ProgressBar` | Progress display |
| `RadioButton` / `RadioButtonGroup` | Exclusive selection |
| `Template` / `Instance` | UXML reuse |

### Template Reuse

Define reusable UI fragments as separate UXML files, then instantiate them:

```xml
<!-- ItemRow.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement class="item-row">
        <ui:Label name="item-name" />
        <ui:Label name="item-count" />
    </ui:VisualElement>
</ui:UXML>
```

## USS — Styling

USS (Unity Style Sheets) uses CSS-like syntax with Unity-specific properties.

```css
/* Assets/UI/MainMenu.uss */

/* Type selector — matches all Labels */
Label {
    color: #CCCCCC;
    font-size: 16px;
    -unity-font-style: normal;
}

/* Class selector — matches .menu-button */
.menu-button {
    width: 300px;
    height: 60px;
    margin: 8px 0;
    padding: 12px 24px;
    background-color: rgba(40, 40, 40, 0.9);
    border-width: 2px;
    border-color: #555555;
    border-radius: 8px;
    color: white;
    font-size: 20px;
    -unity-text-align: middle-center;
    transition-duration: 0.15s;
    transition-property: background-color, border-color, scale;
}

/* Name selector — matches #title */
#title {
    font-size: 48px;
    -unity-font-style: bold;
    color: #FFD700;
}

/* Pseudo-states */
.menu-button:hover {
    background-color: rgba(60, 60, 60, 0.95);
    border-color: #888888;
    scale: 1.02 1.02;
}

.menu-button:active {
    background-color: rgba(80, 80, 80, 1.0);
    scale: 0.98 0.98;
}

.menu-button:focus {
    border-color: #4488FF;
    border-width: 3px;
}

.menu-button:disabled {
    opacity: 0.4;
}

/* Combined selectors */
.menu-button.primary {
    background-color: rgba(30, 80, 160, 0.9);
    border-color: #4488FF;
}

.menu-button.primary:hover {
    background-color: rgba(40, 100, 200, 0.95);
}
```

### USS Variables (Theming)

Define variables for consistent theming and runtime theme switching:

```css
/* Theme-Light.uss */
:root {
    --bg-primary: #F0F0F0;
    --bg-secondary: #FFFFFF;
    --text-primary: #222222;
    --text-secondary: #666666;
    --accent: #2266CC;
    --accent-hover: #3388EE;
    --border-default: #CCCCCC;
    --font-size-body: 16px;
    --font-size-heading: 28px;
    --spacing-sm: 4px;
    --spacing-md: 8px;
    --spacing-lg: 16px;
}

/* Consume variables */
.panel {
    background-color: var(--bg-primary);
    padding: var(--spacing-lg);
    border-color: var(--border-default);
}

.heading {
    font-size: var(--font-size-heading);
    color: var(--text-primary);
}
```

### USS Transitions and Animations

```css
.card {
    transition-property: translate, opacity, scale;
    transition-duration: 0.3s, 0.2s, 0.3s;
    transition-timing-function: ease-out-cubic;
    opacity: 1;
    translate: 0 0;
}

.card.hidden {
    opacity: 0;
    translate: 0 20px;
}
```

