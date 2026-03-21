# Data Binding

> Part of the `unity-ui-toolkit` skill. See [SKILL.md](SKILL.md) for the overview.

## Data Binding

> **Unity 6: Runtime Data Binding** is now production-ready. Key features:
>
> - **`[CreateProperty]`** attribute on fields/properties makes them bindable without boilerplate.
> - **`INotifyBindablePropertyChanged`** interface on your data source enables automatic UI updates when properties change (replaces manual event wiring).
> - **`BindingMode`**: `TwoWay` (input controls), `ToTarget` (read-only display), `ToSource` (write-only from UI), `ToTargetOnce` (initial set only).
> - **`[UxmlElement]` / `[UxmlAttribute]`** replace the deprecated `UxmlFactory` / `UxmlTraits` pattern for custom elements. Use these for all new custom controls.
>
> These features eliminate most manual C# event wiring for data-driven UIs like settings screens, tuning panels, and garage menus.

For editor tools or runtime data binding:

```csharp
// Runtime binding (manual — most common in games)
public void BindPlayerData(PlayerData data)
{
    var nameLabel = root.Q<Label>("player-name");
    var healthBar = root.Q<HealthBar>("player-health");

    // Update on change — use events or polling
    data.OnHealthChanged += hp => healthBar.CurrentHealth = hp;
    nameLabel.text = data.PlayerName;
}

// SerializedObject binding (editor tools)
public override VisualElement CreateInspectorGUI()
{
    var root = new VisualElement();
    var speedField = new FloatField("Speed");
    speedField.BindProperty(serializedObject.FindProperty("speed"));
    root.Add(speedField);
    return root;
}
```
