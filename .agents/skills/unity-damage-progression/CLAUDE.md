# unity-damage-progression/

Vehicle damage modeling, component wear simulation, and career progression systems for RC racing.

## Files

| File | Contents |
|------|----------|
| `SKILL.md` | Visual damage (shader `_DamageAmount`/`_DirtAmount`, decals, LOD body swap), physics modifiers (8 modifier types), 6 damage zones with HP values and 5-tier force thresholds, zone-to-modifier mapping formulas, quick vs full repair with cost formulas, tire wear (non-linear 3-phase curve, 3 compounds as `TireCompoundSO`), LiPo battery (per-cell voltage discharge, voltage sag, LVC, degradation), motor wear (efficiency, heat differential, 4 upgrade tiers 21.5T-10.5T as `MotorSpecSO`), career mode (4 tiers Clubman-Pro, 5 event types, XP), vehicle upgrades (`VehicleUpgradeSO` with `UpgradeModifier`, 8 categories x 3 levels), currency economy (earn/spend balance targets), 3-tier progressive tuning disclosure, implementation phases 1-3 with dependency chains, event interfaces, data flow diagram, asset organization |

## Relevant Skills

| Skill | Relationship |
|-------|-------------|
| `unity-scriptable-objects` | Template pattern for `TireCompoundSO`, `VehicleUpgradeSO`, `MotorSpecSO`, `DamageZoneConfig` |
| `unity-save-load` | Persisting career state, battery degradation, upgrade inventory, tuning presets to JSON |
| `unity-physics-3d` | Collision detection, contact point processing, force-based damage calculation |
