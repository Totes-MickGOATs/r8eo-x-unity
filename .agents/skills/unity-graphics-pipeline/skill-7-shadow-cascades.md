# 7. Shadow Cascades

> Part of the `unity-graphics-pipeline` skill. See [SKILL.md](SKILL.md) for the overview.

## 7. Shadow Cascades

Shadow quality is critical for racing — shadows communicate road surface detail, time of day, and object proximity. Misconfigured shadows cause shimmer, peter-panning, or wasted resolution.

**Recommended Configuration (3 Cascades):**

| Cascade | Distance | Resolution | Purpose |
|---------|----------|------------|---------|
| 0 | 5m | 2048 | Vehicle + immediate surroundings |
| 1 | 20m | 1024 | Track surface ahead |
| 2 | 50m | 512 | Distant scenery, buildings |

**Why 3, Not 4:**
- 4th cascade adds minimal quality for 25% more shadow map memory
- RC tracks are smaller than full-size racing tracks — 50m covers most of the visible track
- Saved budget goes to higher resolution in cascade 0

**Shadow Settings:**
- Shadow Resolution: 2048 (cascade 0), matching URP Asset settings
- Depth Bias: 0.5-1.0 (prevents shadow acne on flat track surfaces)
- Normal Bias: 0.3-0.5 (prevents peter-panning on small RC-scale objects)
- Soft Shadows: Enabled with PCF 5x5 (visually important for ground contact shadows)
- Screen Space Shadows: Enable if GPU budget allows (~0.3ms) — reduces shadow aliasing

**Racing-Specific Issues:**
- Shadow cascade transitions visible on flat track surfaces — enable cascade blending (10% border)
- Fast camera movement causes shadow shimmer — enable Stable Fit shadow projection
- Small RC-scale objects (antenna, wing mounts) may lose shadows — increase shadow resolution or exclude from shadow casting

---

