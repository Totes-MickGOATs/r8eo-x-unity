---
name: unity-damage-progression
description: Unity Damage, Wear & Progression for RC Racing
---


# Unity Damage, Wear & Progression for RC Racing

Use this skill when implementing collision damage, tire wear, battery discharge, repair economies, career progression, or vehicle upgrade systems for RC racing.

## When NOT to Use

- Physics engine setup (solver, timestep, layers) -- use `unity-physics-tuning`
- ScriptableObject fundamentals -- use `unity-scriptable-objects`
- Save/load implementation details -- use `unity-save-load`
- Shader authoring basics for damage visuals -- use `unity-shaders`
- General state machine patterns -- use `unity-state-machines`

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-scriptable-objects`** | Template pattern for `TireCompoundSO`, `VehicleUpgradeSO`, `MotorSpecSO`, `DamageZoneConfig` |
| **`unity-save-load`** | Persisting career state, battery degradation, upgrade inventory, tuning presets to JSON |
| **`unity-physics-3d`** | Collision detection, contact point processing, force-based damage calculation |


## Topic Pages

- [1. Visual Damage System](skill-1-visual-damage-system.md)

