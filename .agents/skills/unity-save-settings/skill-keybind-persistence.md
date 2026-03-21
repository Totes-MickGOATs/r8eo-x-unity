# Keybind Persistence

> Part of the `unity-save-settings` skill. See [SKILL.md](SKILL.md) for the overview.

## Keybind Persistence

Use the Input System's built-in serialization — do NOT manually serialize individual bindings:

```csharp
// Save — the ONLY correct API
string overrides = playerInput.actions.SaveBindingOverridesAsJson();
File.WriteAllText(bindingsPath, overrides);

// Load — the ONLY correct API
string json = File.ReadAllText(bindingsPath);
playerInput.actions.LoadBindingOverridesFromJson(json);
```

### Composite Rebinding (WASD)

Composite bindings (like WASD movement) have parts. Target them by binding index:

```csharp
// For a Vector2 composite "Move" with Up/Down/Left/Right parts:
// Index 0 = the composite itself (skip this)
// Index 1 = Up part
// Index 2 = Down part
// Index 3 = Left part
// Index 4 = Right part

action.PerformInteractiveRebinding()
    .WithTargetBinding(2)  // rebind the "Down" part
    .Start();
```

Always skip index 0 (the composite node) when listing rebindable parts in the UI.

