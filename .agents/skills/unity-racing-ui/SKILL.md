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


## Topic Pages

- [Three-Canvas Optimization](skill-three-canvas-optimization.md)
- [Menu Flow](skill-menu-flow.md)
- [Race Start Sequence](skill-race-start-sequence.md)
- [Ghost Data](skill-ghost-data.md)
- [Accessibility](skill-accessibility.md)
- [Async Scene Loading](skill-async-scene-loading.md)

