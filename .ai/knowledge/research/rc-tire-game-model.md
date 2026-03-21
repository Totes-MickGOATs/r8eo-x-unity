# RC Tire Game Model — Pacejka Parameter Mapping

Proposed tire system for R8EO-X implementation. Part of the [RC Tire Guide](./rc-tire-guide.md).

---

## Pacejka Formula Overview

Formula: `F = D * sin(C * atan(B*x - E*(B*x - atan(B*x))))`

- **D (Peak friction):** Compound softness and surface match
- **B (Cornering stiffness):** Tread pattern and carcass design
- **C (Shape factor):** Tread pattern — transition from grip to slide
- **E (Curvature):** Compound — behavior past peak slip angle

---

## Grip Level (D) — Compound Softness Mapping

| Compound Class | Relative Grip (D multiplier) | Notes |
|---------------|------------------------------|-------|
| Hard / Long-Wear | 0.85 - 0.95 | Baseline grip, maximum durability |
| Medium | 0.95 - 1.05 | Standard reference grip |
| Soft | 1.05 - 1.20 | Good grip, moderate wear |
| Super Soft | 1.15 - 1.30 | High grip, fast wear |
| Mega Soft | 1.25 - 1.40 | Maximum grip, very fast wear |

**Surface match multiplier:**
- Wrong compound for surface: 0.7 - 0.85x
- Acceptable match: 0.90 - 1.0x
- Optimal match: 1.0 - 1.10x
- Perfect match (blue groove + soft): 1.10 - 1.20x

## Response Sharpness (B) — Tread Pattern Effects

| Tread Type | B (Stiffness Factor) | Rationale |
|-----------|---------------------|-----------|
| Slick | 1.2 - 1.4 (highest) | Maximum contact patch, immediate response |
| Bar | 1.1 - 1.3 | Connected tread, stable predictable buildup |
| Small pin | 1.0 - 1.2 (reference) | Individual pins flex slightly |
| Medium pin | 0.9 - 1.1 | More flex in taller pins |
| Tall pin / knobby | 0.7 - 0.9 (lowest) | Significant pin flex before full engagement |
| Step pin | 0.85 - 1.05 | Two-stage engagement |

**Directional bias:** Bar tires have higher B laterally than longitudinally. Pin tires are more isotropic.

## Breakaway Character (C, E)

| Tread Type | C (Shape) | E (Curvature) | Breakaway Feel |
|-----------|-----------|----------------|----------------|
| Slick | 1.6 - 1.8 | -0.5 to 0.0 | Sharp, sudden. Very little warning. |
| Bar | 1.5 - 1.7 | -0.3 to 0.2 | Progressive but quick. Predictable. |
| Small pin | 1.3 - 1.6 | -0.2 to 0.3 | Gradual, forgiving. Easy to catch. |
| Tall pin / knobby | 1.1 - 1.4 | 0.0 to 0.5 | Very gradual. Pins fold progressively. |
| Step pin | 1.2 - 1.5 | -0.1 to 0.3 | Two-stage: initial grip, plateau, then slide. |

## Rolling Resistance

| Tread Type | Rolling Resistance Multiplier |
|-----------|------------------------------|
| Slick | 1.0x (reference) |
| Bar | 1.05 - 1.10x |
| Small pin | 1.10 - 1.15x |
| Medium pin | 1.15 - 1.25x |
| Tall pin / knobby | 1.25 - 1.40x |
| Paddle (sand) | 1.40 - 1.60x |

Softer compounds have 5-10% higher rolling resistance. On loose surfaces, rolling resistance can be 2-3x the hard-surface value.

---

## Proposed Tire Types for R8EO-X

| ID | Name | Type | Best Surfaces | Game Role |
|----|------|------|---------------|-----------|
| `pin_small` | Mini Pin | Small pin | Hard clay, blue groove, dust | All-rounder, default |
| `pin_tall` | Mega Pin | Tall pin | Loose dirt, loam, wet | Loose surface specialist |
| `step_pin` | Step Pin | Step/staggered | Transitional, mixed | Versatile off-road |
| `bar` | Bar Tread | Connected bars | Indoor clay, carpet, treated | High-grip specialist |
| `knobby` | Knobby | Aggressive lug | Mud, grass, sand, wet | Extreme loose/soft |
| `slick` | Slick | No tread | Carpet, tarmac, blue groove | Maximum grip (clean only) |
| `hole_shot` | Hole Shot | Dense multi-pin | Hard-pack, dusty, grooved | Popular all-rounder |

## Proposed Compound Grades

| ID | Name | Grip Mult | Wear Mult | Optimal Temp (°C) | Temp Window |
|----|------|-----------|-----------|--------------------|----|
| `hard` | Hard / Long-Wear | 0.90 | 0.30 | 30-45 | Wide (+-15°C) |
| `medium` | Medium | 1.00 | 0.60 | 25-38 | Medium (+-10°C) |
| `soft` | Soft | 1.10 | 1.00 | 18-30 | Medium (+-8°C) |
| `super_soft` | Super Soft | 1.20 | 1.50 | 10-22 | Narrow (+-6°C) |
| `mega_soft` | Mega Soft | 1.30 | 2.50 | 0-15 | Narrow (+-5°C) |

## Surface Compatibility Matrix

Grip effectiveness multiplier when tire type meets surface:

| Tire \ Surface | Packed Clay | Loose Dirt | Gravel | Grass | Tarmac | Carpet | Sand |
|---------------|-------------|------------|--------|-------|--------|--------|------|
| Mini Pin | 1.00 | 0.85 | 0.80 | 0.70 | 0.90 | 0.85 | 0.50 |
| Mega Pin | 0.85 | 1.00 | 0.90 | 0.90 | 0.70 | 0.65 | 0.65 |
| Step Pin | 0.90 | 0.95 | 0.90 | 0.85 | 0.75 | 0.70 | 0.60 |
| Bar Tread | 1.05 | 0.65 | 0.60 | 0.55 | 0.95 | 1.05 | 0.40 |
| Knobby | 0.75 | 0.90 | 0.95 | 1.00 | 0.60 | 0.50 | 0.85 |
| Slick | 0.95 | 0.40 | 0.35 | 0.30 | 1.10 | 1.10 | 0.25 |
| Hole Shot | 1.00 | 0.90 | 0.85 | 0.70 | 0.85 | 0.80 | 0.50 |

## Pacejka Parameter Deltas by Tire Type

| Tire Type | D | B | C | E | Roll Resist |
|-----------|------|------|------|-------|-------------|
| `pin_small` | 1.00 | 1.05 | 1.40 | 0.10 | 1.12 |
| `pin_tall` | 1.00 | 0.85 | 1.25 | 0.25 | 1.30 |
| `step_pin` | 1.00 | 0.95 | 1.35 | 0.15 | 1.20 |
| `bar` | 1.05 | 1.15 | 1.60 | 0.00 | 1.07 |
| `knobby` | 1.00 | 0.80 | 1.20 | 0.30 | 1.35 |
| `slick` | 1.10 | 1.30 | 1.70 | -0.20 | 1.00 |
| `hole_shot` | 1.02 | 1.00 | 1.40 | 0.05 | 1.15 |

## Wear / Degradation Model

```
wear_rate = base_wear * compound_factor * surface_abrasion * speed_factor * slip_factor

Where:
  base_wear = 0.001 per second of contact (0.0 = new, 1.0 = destroyed)
  compound_factor: Hard=0.3, Medium=0.6, Soft=1.0, Super Soft=1.5, Mega Soft=2.5
  surface_abrasion: Carpet=0.3, Clay(smooth)=0.5, Clay(rough)=0.8, Packed dirt=0.7,
                    Loose dirt=0.4, Gravel=1.5, Tarmac=1.0, Sand=0.6
  speed_factor = 1.0 + (speed_mps / 20.0) * 0.5
  slip_factor = 1.0 + abs(slip_ratio) * 2.0 + abs(slip_angle_rad) * 3.0

grip_multiplier = 1.0 - (wear * wear * 0.3)  # Quadratic degradation
```

## AI Tire Selection Logic

1. Surface type → pick tire with highest compatibility from the matrix
2. Difficulty level affects selection quality:
   - CHAMPION: Always picks optimal tire + compound
   - EXPERT: 80% optimal, 20% one-off
   - INTERMEDIATE: 60% optimal
   - BEGINNER: Random from "reasonable" options
