# Track Layout with Unity Splines

> Part of the `unity-terrain-track-creation` skill. See [SKILL.md](SKILL.md) for the overview.

## Track Layout with Unity Splines

Use the **Unity Splines** package (`com.unity.splines`) for track centerline definition.

### Setup

```
// Package: com.unity.splines (add via Package Manager)
// Create: GameObject > Spline > Draw Splines Tool
```

### Spline Configuration

- **Closed loop:** Enable `Closed` on the SplineContainer for circuit tracks.
- **Knot tangent modes:** Use `Auto Smooth` for flowing curves, `Bezier` for precise control of entry/exit angles.
- **Knot count:** 20-40 knots for a typical 100-150m RC circuit. Fewer knots = smoother curves.
- **Width:** Store track width as SplineData (float) attached to knots — allows variable-width sections.

### RC Track Dimensions

| Parameter | Range | Notes |
|-----------|-------|-------|
| Track width | 3-6 m | 3m minimum for 1/10 scale; 5-6m for multi-class racing |
| Total loop length | 80-200 m | 80m for technical tracks, 200m for speed circuits |
| Jump height | 0.3-1.2 m | 0.3m = small kicker, 1.2m = large tabletop |
| Banking angle | 5-20 degrees | 5 for gentle sweepers, 15-20 for high-speed berms |
| Straight length | 10-30 m | Longer straights for speed; short for technical |
| Minimum corner radius | 3-5 m | Tighter = more technical, slower |

---

