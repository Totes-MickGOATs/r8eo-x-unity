# RC Tire Compounds

Reference for R8EO-X tire compound selection. Part of the [RC Tire Guide](./rc-tire-guide.md).

---

## Pro-Line Compounds (Hardest to Softest)

| Mark | Name | Hardness | Recommended Use | Optimal Temp |
|------|------|----------|-----------------|--------------|
| G8 | G8 | Firm | Rock terrain, crawling | <60°F (16°C) |
| PR | Predator | Soft | Rock terrain, technical crawling | Cold conditions |
| UB | Unblended | Medium-Firm | General purpose | Wide range |
| MC | MC (Clay) | Medium-Firm | Clay tracks | 70-90°F (21-32°C) |
| M2 | M2 | Medium | Dry surface, high-wear tracks | 80-100°F (27-38°C) |
| M3 | M3 (Soft) | Soft | Outdoor dirt, watered tracks | 70-90°F (21-32°C) |
| M4 | M4 (Super Soft) | Super Soft | Carpet, astroturf, low-grip | 70-90°F (21-32°C) |
| S3 | S3 | Soft (long-wear) | Outdoor dirt, dry conditions | Similar to M3 |
| S4 | S4 | Medium-Soft (long-wear) | General outdoor | Similar to M3 |
| S5 | S5 | Firm (long-wear) | High-wear conditions | Wide range |

**S-Series vs M-Series:** S compounds are long-wear versions. S3 is between M3 and M4. S4 ≈ M3 softness. S-series lasts significantly longer.

**Key insight from Adam Drake:** "Use M3's when it's wet and run S3's when it's dry, then switch to M4/S4 when temps drop below 70°F."

## JConcepts Compounds (Hardest to Softest)

| Color | Name | Feel | Best Conditions | Temp Range |
|-------|------|------|-----------------|------------|
| Yellow | Medium | Medium | General purpose | Wide range |
| Gold | Clay | Soft | Warm/prepped/clay surfaces | >90°F (32°C) |
| Blue | Soft | Soft | All weather outdoor | <90°F (32°C) |
| Aqua | Ultra-Grip | Very Soft | Indoor high-bite | Variable |
| Silver | Silver | Very Soft | Indoor carpet, cold/wet | Cold/wet |
| Green | Super Soft | Super Soft | Cool/cold weather outdoor | <75°F (24°C) |
| Black | Mega Soft | Mega Soft | Extremely cold or loose | <59°F (15°C) |

**Outdoor:** Blue and Green. **Indoor:** Aqua, Gold, Silver.

## AKA Racing Compounds (Hardest to Softest)

| Dot Color | Name | Best Conditions | Temp Range |
|-----------|------|-----------------|------------|
| — | Medium | High-grip, hot weather | >25°C (77°F) |
| Orange | Medium Soft (K) | Medium-high grip | 15-25°C |
| Blue | Super Soft (V) | Low grip, cold | 5-15°C |
| Purple | Clay (C) | Indoor/outdoor clay (damp) | Variable |
| Gold | Medium Carpet (H) | Carpet racing | Variable |
| Gold/Gold | Soft Carpet (G) | High-grip carpet | Variable |
| — | Ultra Soft | Extreme cold, very low grip | <5°C (41°F) |
| Yellow/Yellow | Long Wear (Z) | Extended races, durability | Wide range |

---

## Compound Principles

**Core rule:** "The harder the surface, the softer the tire; the softer the surface, the harder the tire."
- Hard surface (blue groove, concrete): SOFT compound — grip from rubber adhesion
- Soft surface (loose dirt, mud): HARD compound — grip from mechanical tread; soft compound would fold

**Grip sources (Ray Munday):**
1. Adhesion — rubber bonds chemically to track (dominant on hard, clean surfaces)
2. Deformation — tire digs into surface irregularities (dominant on medium surfaces)
3. Destruction — rubber tears away, leaving grip deposit (dominant on abrasive surfaces)

## Temperature Sensitivity

| Air Temp | Compound |
|----------|---------|
| Below 60°F (15°C) | Super soft / mega soft |
| 60-75°F (15-24°C) | Soft compounds |
| 75-90°F (24-32°C) | Medium / clay compounds |
| Above 90°F (32°C) | Medium-hard / long-wear |

Track surface can be 30-40°F hotter than air in direct sun. Racers measure SURFACE temperature.

## Wear Rates

- Soft compounds: ~3-5 runs on abrasive surfaces before significant degradation
- Hard compounds: ~20+ runs on smooth tracks
- Best performance typically occurs in first 3-4 runs regardless of compound
- Long-wear (S-series, Z-series) lasts ~2-3x longer than standard equivalents

**Grip over a run:**
1. Break-in (laps 1-2): grip increases as tire heats and pins wear to optimal height
2. Peak window (laps 3-8): maximum grip
3. Degradation: grip fades. Soft tires may show "graining" (tiny rubber rolls)
4. End of life: lap times drop consistently. Cord may show through.

**Pro racing strategy:** M3/M4 for qualifiers (short, max grip). S3/S4 for main events (longer, consistent).

## Tire Sauce / Traction Compounds

Liquid chemicals applied to tires to chemically soften rubber and increase grip.

- **Common products:** Sticky Kicks, TDK High Grip; DIY recipes use various solvents
- **Health concerns:** Many are associated with reproductive toxicity, skin irritation
- **Rules:** BRCA (British) and various bodies have banned tire additives. Enforcement is nearly impossible.
- **Game modeling:** Tire sauce shifts compound ~1-2 grades softer. Model as +5-15% grip, +50-100% wear rate.

## Foam Inserts

| Type | Compression | Best For |
|------|------------|---------|
| Closed-Cell | Resists, recovers quickly | Smooth, high-grip tracks; prevents traction rolling |
| Open-Cell | Compresses easily, recovers slowly | Outdoor, soft-pack, bumpy conditions; more grip |
| Dual-Layer | Soft outer + firm inner | Best of both worlds |

**Game modeling:** Closed-cell = stiffer sidewall, less grip variation; Open-cell = softer sidewall, more grip but less predictable.
