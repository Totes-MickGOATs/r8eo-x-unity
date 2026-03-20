---
name: unity-racing-ui
description: Unity Racing UI — HUD, Menus, and Race Management
---

# Unity Racing UI — HUD, Menus, and Race Management

Use this skill when building racing game UI systems including in-race HUD, menu flow, minimap, split-screen viewports, leaderboards, ghost data display, race start sequences, and accessibility features.

---

## HUD Layout

### Sim-Style Racing HUD

Position critical telemetry where the eye naturally rests:

| Element | Position | Update Rate | Canvas |
|---------|----------|-------------|--------|
| Speedometer (needle) | Bottom-center | Every frame | Dynamic-fast |
| RPM bar | Bottom-center, above speed | Every frame | Dynamic-fast |
| Gear indicator | Bottom-center, inset | On change | Dynamic-fast |
| Lap timer | Top-center | Every frame | Dynamic-fast |
| Sector delta (+/-) | Top-center, below timer | On sector crossing | Dynamic-slow |
| Lap counter | Top-right | On lap crossing | Dynamic-slow |
| Position indicator | Top-right, below lap | On position change | Dynamic-slow |
| Minimap | Bottom-left or top-left | Every 2-3 frames | Dynamic-slow |
| Race notifications | Center, fade-in/out | On event | Dynamic-slow |

---

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

## Hybrid UI Approach

Use the right UI system for each context:

| Context | System | Rationale |
|---------|--------|-----------|
| Main menu, settings, vehicle select | **UI Toolkit** | Complex layouts, USS styling, scrollable lists, data binding |
| In-race HUD | **uGUI Canvas** | Per-frame updates, render texture minimap, world-space markers |
| Pause overlay | **UI Toolkit** | Modal dialog over HUD, does not need per-frame updates |
| Results screen | **UI Toolkit** | Table layout, scrolling, styled text |

### Why Not All UI Toolkit?

UI Toolkit lacks efficient per-frame element updates and render texture integration needed for racing HUD. uGUI Canvas with `CanvasRenderer` is purpose-built for real-time telemetry display.

### Why Not All uGUI?

Menu systems with complex layouts, data binding, and styling are verbose and fragile in uGUI. UI Toolkit's USS + UXML provides CSS-like layout that scales better for menu hierarchies.

---

## Minimap

### Render Texture Approach

1. Create an orthographic camera positioned above the track, looking down (Y-axis).
2. Set camera `cullingMask` to only render terrain, track, and vehicle layers.
3. Render to a 256x256 `RenderTexture` (sufficient resolution for a minimap).
4. Display on a `RawImage` UI element in the HUD.

### Performance

- **Update every 2-3 frames**, not every frame. Minimap does not need 60fps fidelity.
- Use `Camera.targetTexture` and disable the camera, then call `Camera.Render()` manually on a timer.
- Use a low pixel resolution (256x256) and a simplified culling mask.

### Vehicle Blips

- Do NOT render vehicles in the minimap camera. Instead, overlay UI `Image` icons positioned via world-to-minimap coordinate mapping.
- Color-code blips: player = bright, opponents = dimmer, ghost = semi-transparent.

---

## Menu Flow

```
Main Menu
  ├── Vehicle Select
  │     └── Tuning / Livery
  ├── Track Select
  │     └── Race Config (laps, AI count, weather)
  ├── Settings
  │     ├── Graphics
  │     ├── Audio
  │     ├── Controls / Rebinding
  │     └── Accessibility
  ├── Leaderboards
  └── Quit

Race Config → Pre-Race (countdown) → Race → Results
                                              ├── Replay
                                              ├── Retry
                                              └── Back to Menu
```

### Scene Transitions

Each major screen maps to a scene or an additive scene:

| Screen | Scene Strategy |
|--------|---------------|
| Main Menu | Persistent scene, loaded at boot |
| Vehicle / Track Select | UI overlay in menu scene (no scene load) |
| Race | Full scene load (track + vehicles + HUD) |
| Results | Additive scene over race scene (keeps race state for replay) |

---

## Async Scene Loading

Use the `allowSceneActivation = false` pattern for loading screens with progress bars:

```csharp
public async Awaitable LoadTrackScene(string sceneName, Slider progressBar)
{
    AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
    op.allowSceneActivation = false;

    while (op.progress < 0.9f)
    {
        // progress stops at 0.9 until allowSceneActivation = true
        progressBar.value = op.progress / 0.9f;
        await Awaitable.NextFrameAsync();
    }

    progressBar.value = 1f;
    // Optional: hold for minimum display time or player input
    await Awaitable.WaitForSecondsAsync(0.5f);

    op.allowSceneActivation = true;
}
```

---

## Split-Screen

### Camera Viewport Partitioning

```csharp
// 2-player horizontal split
camera1.rect = new Rect(0f, 0.5f, 1f, 0.5f); // Top half
camera2.rect = new Rect(0f, 0f, 1f, 0.5f);   // Bottom half

// 2-player vertical split
camera1.rect = new Rect(0f, 0f, 0.5f, 1f);   // Left half
camera2.rect = new Rect(0.5f, 0f, 0.5f, 1f); // Right half
```

### Per-Camera HUD

- Each player gets their own Canvas set to `Screen Space - Camera` mode, assigned to their camera.
- `Screen Space - Overlay` canvases span the full screen and cannot be split. Always use `Screen Space - Camera` for split-screen HUD.
- Scale HUD elements down proportionally to viewport size.

---

## Leaderboards

### Local Storage

- Store per-track leaderboards as JSON: `{trackId, entries: [{name, time, date, ghostFile}]}`.
- Cap at 10 entries per track. Insert-sort on completion time.
- Save to `Application.persistentDataPath/leaderboards/`.

### Online (Steam)

- **Steam Leaderboards API:** Upload score (lap time in milliseconds as int) via `SteamUserStats.UploadLeaderboardScore`.
- **Ghost attachment:** Upload ghost file as UGC (User-Generated Content) and attach UGC handle to the leaderboard entry.
- Download top-N or friends-only entries for display.

---

## Ghost Data

### Recording Format

Record vehicle state at 30Hz (every other FixedUpdate at 50Hz):

```csharp
struct GhostFrame  // 28 bytes at 30Hz recording rate
{
    float time;           // 4 bytes
    Vector3 position;     // 12 bytes
    Quaternion rotation;  // 16 bytes (or compressed, see replay-ghost skill)
    // Total: 32 bytes with padding
}
```

### Storage Estimate

- 30 frames/sec x 120 sec (2-min lap) = 3,600 frames
- 32 bytes x 3,600 = ~115 KB uncompressed
- With DeflateStream: ~60-80 KB per ghost

### Playback Interpolation

- Use **Hermite spline interpolation** for position to avoid jerky movement between samples.
- Use `Quaternion.Slerp` for rotation between adjacent frames.
- See `unity-replay-ghost` skill for full implementation details.

---

## Race Start Sequence

### F1-Style Traffic Lights

1. **Formation lap complete** — vehicles on grid, motor authority LOCKED (throttle input ignored).
2. **5 sequential red lights** — each light illuminates 1 second apart (5 seconds total).
3. **Random hold** — all 5 reds stay lit for a random duration (0.5-3.0 seconds). This prevents anticipation.
4. **Lights out** — all reds extinguish simultaneously. Motor authority UNLOCKED. GO.
5. **Jump start detection** — if any vehicle crosses the start line before lights out, apply a time penalty.

```csharp
// Race start sequence coroutine
public async Awaitable RunStartSequence()
{
    motorAuthority.Lock(); // Prevent throttle input

    for (int i = 0; i < 5; i++)
    {
        lights[i].SetRed(true);
        await Awaitable.WaitForSecondsAsync(1.0f);
    }

    float holdTime = Random.Range(0.5f, 3.0f);
    await Awaitable.WaitForSecondsAsync(holdTime);

    foreach (var light in lights)
        light.SetRed(false);

    motorAuthority.Unlock(); // GO
    raceTimer.Start();
}
```

---

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

## Canvas Optimization Checklist

- [ ] Sprite atlases for all HUD icons (one atlas per canvas)
- [ ] Raycast Target disabled on every non-interactive HUD element
- [ ] No Animator components on HUD elements (code-driven animation only)
- [ ] Three separate canvases (static, dynamic-fast, dynamic-slow)
- [ ] `TextMeshProUGUI` with cached format strings (no `string.Format` per frame)
- [ ] Minimap camera renders every 2-3 frames, not every frame
- [ ] Split-screen uses `Screen Space - Camera`, not `Screen Space - Overlay`

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-ui-toolkit`** | UI Toolkit fundamentals, USS styling, UXML layout, data binding |
| **`unity-ui-design`** | General UI/UX design patterns and principles |
| **`unity-input-system`** | Input System setup, rebinding, action maps |
