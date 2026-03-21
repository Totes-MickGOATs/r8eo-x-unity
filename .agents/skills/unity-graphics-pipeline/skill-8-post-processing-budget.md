# 8. Post-Processing Budget

> Part of the `unity-graphics-pipeline` skill. See [SKILL.md](SKILL.md) for the overview.

## 8. Post-Processing Budget

Allocate post-processing effects within a strict per-frame GPU budget. Racing games need consistent frame times — a 2ms spike causes visible stutter at 60fps.

**Effect Budgets:**

| Effect | Budget | Notes |
|--------|--------|-------|
| Color Grading (LUT) | < 0.1ms | Essentially free, always enable |
| Bloom | 0.8-1.5ms | Use 4-5 iterations, threshold 0.9-1.1 |
| SSAO (half-res) | 0.5ms | Half-resolution + bilateral blur |
| Motion Blur | 0.5-1.0ms | Per-object preferred over camera motion blur |
| Tonemapping | < 0.1ms | ACES or Neutral, negligible cost |
| Chromatic Aberration | < 0.1ms | Speed-driven only (see game-feel skill) |
| Vignette | < 0.05ms | Trivial cost, always available |
| Film Grain | < 0.1ms | Optional, adds cinematic grit |
| **Total target** | **< 3.0ms** | At 60fps, post leaves 13.6ms for everything else |

**Effects to AVOID in Racing:**
- Depth of Field — obscures track obstacles, confuses player about focus
- Screen Space Reflections at full-res — too expensive for racing frame budgets
- Panini Projection — distorts track geometry, disorienting at speed

**Volume Strategy:**
- Global Volume: base color grading + tonemapping + vignette (always on)
- Speed Volume: bloom intensity + chromatic aberration + motion blur (weight driven by speed)
- Impact Volume: brief intensity spike on collision (weight driven by impact force, auto-decay)

---

