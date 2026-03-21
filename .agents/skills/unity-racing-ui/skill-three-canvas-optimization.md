# Three-Canvas Optimization

> Part of the `unity-racing-ui` skill. See [SKILL.md](SKILL.md) for the overview.

## Three-Canvas Optimization

Split HUD across three canvases to minimize rebuild cost:

### Canvas 1: Static

- Background frames, decorative borders, static labels
- **Rebuilds:** Never (after initial layout)
- **Settings:** `Canvas.renderMode = ScreenSpaceOverlay`

### Canvas 2: Dynamic-Fast

- Speedometer needle, RPM bar fill, lap timer text
- **Rebuilds:** Every frame (unavoidable — values change every frame)
- **Settings:** Separate Canvas component, own `CanvasRenderer`
- **Optimization:** Use `TextMeshProUGUI.SetText(float)` with format caching to avoid string allocations

### Canvas 3: Dynamic-Slow

- Lap counter, position, sector deltas, minimap, notifications
- **Rebuilds:** Only on event (lap crossing, position change, sector crossing)
- **Settings:** Separate Canvas component
- **Optimization:** Update via events, not polling

### Critical HUD Optimizations

- **Disable Raycast Target** on ALL HUD elements. HUD is display-only; raycasting wastes CPU traversing the graphic tree every frame.
- **No Animator on needles.** Drive needle rotation via code: `needleTransform.localRotation = Quaternion.Euler(0, 0, -angle)`. Animator overhead is disproportionate for a single float interpolation.
- **Sprite atlases** for all HUD icons and frames. One draw call per atlas instead of one per sprite.

```csharp
// Code-driven speedometer needle — no Animator needed
float normalizedSpeed = currentSpeed / maxSpeed;
float angle = Mathf.Lerp(minAngle, maxAngle, normalizedSpeed);
needleTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
```

---

