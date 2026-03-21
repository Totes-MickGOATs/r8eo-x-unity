# Accessibility

> Part of the `unity-racing-ui` skill. See [SKILL.md](SKILL.md) for the overview.

## Accessibility

| Feature | Implementation | Priority |
|---------|---------------|----------|
| Colorblind modes | Post-processing shader (protanopia, deuteranopia, tritanopia) | High |
| Input rebinding | Unity Input System `InputActionRebindingExtensions` | High |
| UI scale multiplier | `CanvasScaler.scaleFactor` adjustable in settings | Medium |
| Steering assist | Reduce required input precision, auto-correct toward racing line | Medium |
| Braking assist | Auto-brake before corners based on speed/distance | Medium |
| Screen reader | UI Toolkit `label` properties for accessibility tree | Low |

### Colorblind Shader

Apply as a full-screen post-processing effect. Use a color transformation matrix that simulates then corrects for the specific type of color vision deficiency. Provide a dropdown in Settings: Normal, Protanopia, Deuteranopia, Tritanopia.

### Input Rebinding Flow

```csharp
// Interactive rebinding with Unity Input System
var rebind = action.PerformInteractiveRebinding()
    .WithControlsExcluding("Mouse")
    .OnComplete(op => {
        op.Dispose();
        SaveBindings();
    })
    .Start();
```

---

