# 1/10 Scale RC Buggy Tire Guide — Research Report

Compiled for R8EO-X tire selection system implementation.

---

## Part 1: Tread Patterns and Tire Types

### 1.1 Pin Tires (Mini Pins / Micro Pins)

Small cylindrical rubber pins (2-5mm tall) arranged across the tire surface in various densities.

- **Surfaces:** Hard-packed clay, dusty tracks, blue groove, indoor clay
- **How they grip:** Each pin deforms independently against the surface. Round pins provide omnidirectional grip — consistent feel in all directions, slightly easier to slide. The pins "mould into" micro-imperfections of hard surfaces.
- **Pin height matters:**
  - Short pins (2-3mm): Hard-packed surfaces with minimal loose dirt. More precise steering.
  - Medium pins (4-5mm): General-purpose, good balance of grip and stability.
  - Tall pins (6mm+): Loose or loamy conditions where pins need to dig deeper.
- **Key principle:** "The lower the pin, the more precise the steering; the larger the pin, the more grip the tire will provide." (Adam Drake)
- **Popular examples:** Pro-Line Hole Shot, JConcepts Double Dees, AKA Pin Stripe

### 1.2 Bar Tires (Ribs / Bar Treads)

Horizontal connected bars running across the tire tread, sometimes at angles.

- **Surfaces:** High-grip indoor clay, blue groove, carpet, treated/prepped surfaces
- **Why bars vs pins:**
  - Bars provide maximum contact patch on hard, smooth surfaces
  - Connected tread technology delivers more consistent, predictable traction
  - Bars resist side-bite better — the car feels more "planted" and less "edgy"
  - Longer-lasting than pins due to larger rubber mass per tread element
  - Increased steering response on hard-packed surfaces
- **Trade-off:** Bars can pack with mud/dirt on loose surfaces; pins self-clean better
- **Popular examples:** Pro-Line Electron, JConcepts Ellipse, AKA PinStripe (bar/pin hybrid)

### 1.3 Step Pin Tires

Staggered or tiered pin designs with two heights — tall center pins surrounded by shorter outer pins, or hexagonal pods with connected chains.

- **Surfaces:** Loose dirt, loamy conditions, transitional surfaces (wet-to-dry)
- **Advantage over regular pins:**
  - Dual-layer construction: tall pins dig into loose material while short pins provide secondary contact when tall pins bottom out
  - Better structural support for tall pins — the steps prevent pins from folding over under load
  - More consistent grip across changing conditions
  - Better dirt evacuation — spacing between step levels lets debris clear
- **Popular examples:** JConcepts Step Spike, JConcepts Drop Step, JConcepts Pin Swag

### 1.4 Knobby / Lug Tires

Large, widely-spaced aggressive tread blocks or lugs.

- **Surfaces:** Very loose dirt, mud, sand, grass, wet conditions, heavily rutted tracks
- **How they work:** Deep treads and wide spacing allow lugs to physically dig into soft material. The tire deforms the surface rather than relying on surface adhesion.
- **Characteristics:**
  - Maximum mechanical grip in soft/loose conditions
  - Poor performance on hard-pack — lugs fold over, causing unpredictable handling
  - High rolling resistance due to tread deformation
  - Self-cleaning: wide spacing prevents mud packing
- **Popular examples:** Pro-Line Trencher (grass specialist), JConcepts Goose Bumps

### 1.5 Slick Tires

Smooth, treadless rubber surface.

- **Surfaces ONLY:** Carpet, astroturf, blue groove (heavy rubber deposit), tarmac/concrete, treated indoor clay
- **How they work:** Maximum rubber-to-surface contact area. Grip comes entirely from adhesion (chemical bond between rubber compound and surface) rather than mechanical interlocking.
- **Characteristics:**
  - Highest possible grip on smooth, clean, high-traction surfaces
  - Zero mechanical grip — completely useless on any loose surface
  - Lowest rolling resistance of any tread type
  - Very sensitive to compound selection and temperature
- **Notable trend:** Multiple manufacturers have moved toward slick/near-slick designs for indoor racing. "Four major tire manufacturers ditch the tread" (LiveRC on the Cactus-style slick trend)

### 1.6 Hole Shot Tires

Pro-Line's iconic multi-pin design, now in its 3rd generation (Hole Shot 3.0).

- **What they are:** A dense, small-pin tire with a specific pin arrangement optimized for hard-pack versatility. The design has remained "virtually unchanged" because it already provides "the optimum level of traction in dusty, hard-packed, and grooved conditions."
- **Why so popular:**
  - Excellent blend of forward bite AND side bite — easy to drive
  - Works across a wide range of conditions (dusty to blue groove)
  - Consistent performance as track conditions evolve during a race day
  - "Won title after title on tracks across the globe"
- **Best for:** Hard dirt, dusty hard-pack, hard-pack with a bit of loam on top
- **Not ideal for:** Grass, heavy mud, very loose conditions

### 1.7 Other Popular Patterns

**Dirtwebs (JConcepts):** Web-like connected tread pattern. Bridges the gap between pins and bars. Good for medium-grip outdoor tracks where you need some mechanical grip but also contact area.

**Chainlinks:** Connected chains of pins creating a linked tread structure. Provides structural support for taller pin elements while maintaining self-cleaning ability. Good for loose-to-medium conditions.

**Carcass / Mini Darts:** Low-profile carcass designs that produce more slip angle and better rotation while maintaining grip. These are "the only out of the box thinking tire" — using a different carcass shape to change handling character rather than just tread pattern.

**Sprinter (JConcepts):** Directional side-pin pattern. The go-to tire for outdoor dirt oval racing. Can be mounted in different orientations to tune forward vs. side bite.

**Rehab (JConcepts):** Dense pin layout that behaves like a bar tire but with pin forgiveness. Good for high-grip outdoor conditions where you want bar-like consistency but need dust tolerance.

**Switchblade (Pro-Line):** Unique directional tire with different tread on inside vs. outside. One side has standard pins (loose/low traction), other side has connected tread (high bite). Mounting direction tunes behavior.

**Positron (Pro-Line):** Directional bar/pin hybrid. Center pins boxed in by connected bars with angled bars shooting off. Maximum forward bite with corner speed. Designed for indoor clay.

### 1.8 Front vs. Rear Tire Differences

Front and rear tires differ significantly, especially on 2WD buggies.

**2WD Buggy:**
- **Front:** Narrow (50mm width), ribbed/directional tread optimized for steering traction. Fronts are unpowered, so tread is designed for cornering grip and directional stability, not power transmission.
- **Rear:** Wide (65mm width), full tread pattern for power delivery and traction.
- **Common front patterns:** Ribbed bars, small directional pins, "Groovy" ribs

**4WD Buggy:**
- **Front:** Medium width (58mm), same tread pattern as rear but on narrower tire. Since fronts are powered, they need the same traction pattern as rears.
- **Rear:** Wide (65mm width), full tread pattern.
- **Key:** "When equipping a 1/10 scale 4WD buggy, identical front and rear treads are fitted."

**Why narrower fronts?** Narrower front tires provide improved steering response at higher speeds. On 4WD, wider fronts would provide too much front grip, causing the car to push (understeer) in corners.

**Wheel specifications:** All 1/10 buggies use 2.2" rim diameter with 12mm hex mounting.

---

## Part 2: Rubber Compounds

### 2.1 Compound Systems by Manufacturer

#### Pro-Line Compounds (Hardest to Softest)

| Mark | Name | Hardness | Recommended Use | Optimal Temp |
|------|------|----------|-----------------|--------------|
| G8 | G8 | Firm | Rock terrain, crawling | <60°F (16°C) |
| PR | Predator | Soft | Rock terrain, technical crawling | Cold conditions |
| UB | Unblended | Medium-Firm | General purpose | Wide range |
| MC | MC (Clay) | Medium-Firm | Clay tracks, minimal tread flex | 70-90°F (21-32°C) |
| M2 | M2 | Medium | Dry surface, high-wear tracks | 80-100°F (27-38°C) |
| M3 | M3 (Soft) | Soft | Outdoor dirt, watered tracks | 70-90°F (21-32°C) |
| M4 | M4 (Super Soft) | Super Soft | Carpet, astroturf, low-grip | 70-90°F (21-32°C) |
| S3 | S3 | Soft (long-wear) | Outdoor dirt, dry conditions | Similar to M3 |
| S4 | S4 | Medium-Soft (long-wear) | General outdoor | Similar to M3 |
| S5 | S5 | Firm (long-wear) | High-wear conditions | Wide range |

**S-Series vs M-Series:** The S compounds are long-wear versions. "S3's softness is in between M3 and M4" (approximately M2.5). S4 is "pretty much the same softness as M3." S-series lasts significantly longer.

**X-Compound (legacy/European):**
- X1 (Firm), X2 (Medium), X3 (Soft), X4 (Super Soft)
- Designed for European-style tracks
- "Longest wearing performance compound"
- Maintains stability in elevated heat with minimal tire growth
- X3 was phased out and replaced by S-series

**Key insight from Adam Drake:** "Use M3's when it's wet and run S3's when it's dry, then switch to M4/S4 when temps drop below 70°F."

#### JConcepts Compounds (Hardest to Softest)

| Color | Name | Feel | Expansion | Best Conditions | Temp Range |
|-------|------|------|-----------|-----------------|------------|
| Yellow | Medium | Medium | Low | General purpose | Wide range |
| Gold | Clay | Soft | Medium | Warm/prepped/clay surfaces | >90°F (32°C) or treated |
| Blue | Soft | Soft | Medium-High | All weather outdoor | <90°F (32°C) |
| Aqua | Ultra-Grip | Very Soft | High | Indoor high-bite tracks | Variable |
| Silver | Silver | Very Soft | High | Indoor carpet, cold/wet | Cold/wet |
| Green | Super Soft | Super Soft | High | Cool/cold weather outdoor | <75°F (24°C) |
| Black | Mega Soft | Mega Soft | Very High | Extremely cold or loose | <59°F (15°C) |

**Outdoor compounds:** Blue and Green are conditioned for outdoor use.
**Indoor compounds:** Aqua, Gold, and Silver are indoor favorites.

#### AKA Racing Compounds (Hardest to Softest)

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

### 2.2 How Compound Softness Affects Grip

**Core principle:** "The softer the tire compound, the more the rubber can physically mould itself into the tiny imperfections of the surface," increasing adhesion. However, softer compounds wear faster.

**Counterintuitive rule:** "The harder the surface, the softer the tire; the softer the surface, the harder the tire."
- Hard surface (blue groove, concrete): Use SOFT compound — grip comes from rubber adhesion
- Soft surface (loose dirt, mud): Use HARD compound — grip comes from mechanical tread digging; soft compound would deform and fold

**Grip sources (Ray Munday):**
1. **Adhesion:** Rubber chemically bonds to the track surface (dominant on hard, clean surfaces)
2. **Deformation:** Tire digs into surface irregularities (dominant on medium surfaces)
3. **Destruction:** Rubber tears away, leaving grip deposit (dominant on abrasive surfaces)

### 2.3 Temperature Sensitivity

Every compound has an optimal temperature window:

- **Too cold:** Rubber is too hard to conform to surface. Feels "skatey" with poor grip.
- **Optimal range:** Maximum adhesion and deformation. Tires "stick" to the track.
- **Too hot:** Rubber becomes too soft. Excessive tire growth (ballooning), graining (tiny rolls of rubber forming on surface), and rapid degradation.

**General temperature bands:**
- Below 60°F (15°C): Super soft / mega soft compounds
- 60-75°F (15-24°C): Soft compounds
- 75-90°F (24-32°C): Medium / clay compounds
- Above 90°F (32°C): Medium-hard / long-wear compounds

**Track surface temperature vs. air temperature:** Track surface can be 30-40°F hotter than air temperature in direct sun. RC racers measure SURFACE temperature, not air temperature.

### 2.4 Compound Wear Rates

**Quantitative wear data:**
- Soft compounds: ~3-5 runs on abrasive surfaces before significant degradation
- Hard compounds: ~20+ runs on smooth tracks
- Best performance typically occurs in the first 3-4 runs regardless of compound
- Long-wear (S-series, Z-series) compounds last roughly 2-3x longer than standard equivalents

**How grip changes over a run:**
1. **Break-in period:** First 1-2 laps, grip increases as tire heats up and pins wear to optimal height
2. **Peak window:** Laps 3-8, maximum grip as compound reaches optimal temperature
3. **Degradation onset:** Grip begins fading. Soft tires may show "graining" (tiny rubber rolls) — a clear signal the compound is too soft for conditions
4. **End of life:** Lap times drop consistently despite good driving. Cord may show through tread.

**Pro racing strategy:** Use standard compounds (M3/M4) for qualifiers (short runs, maximum grip). Switch to long-wear compounds (S3/S4) for main events (longer runs, consistent grip over distance).

### 2.5 Tire Sauce / Traction Compounds

**What it is:** Liquid chemicals applied to tires to soften the rubber compound and increase grip. Common term: "sauce."

**How it works:** The liquid soaks into the rubber, chemically softening the compound. This increases adhesion and traction, especially on smooth/hard surfaces.

**Common ingredients:**
- Commercial products: Sticky Kicks, TDK High Grip, various brand-name sauces
- DIY recipes: Liquid wrench + chlorinated brake cleaner; Coleman camp fuel + mineral oil + mineral spirits; WD-40; brake fluid

**Health concerns:** Many tire sauce chemicals are associated with reproductive toxicity, skin irritation/corrosion, eye damage, and organ toxicity.

**Rules and bans:**
- BRCA (British) and various national/regional organizations have banned tire additives
- ROAR (US) has debated bans extensively
- Many local tracks have their own sauce rules — some ban it, others specify approved products
- Enforcement is nearly impossible: "policing traction sauce is impossible"

**For game modeling:** Tire sauce effectively shifts a compound 1-2 grades softer (e.g., makes a medium compound behave like soft). Could be modeled as a pre-race setup option that increases grip coefficient by 5-15% but accelerates wear rate by 50-100%.

### 2.6 Foam Inserts

RC tires use foam inserts instead of air pressure. The insert type affects handling:

**Closed-Cell Foam:**
- Resists compression, recovers quickly
- Provides stable, consistent tire shape
- Better for smooth, high-grip tracks
- Reduces grip on carpet (more stable but less conforming)
- Causes faster tread wear
- Prevents traction rolling in high-grip conditions

**Open-Cell Foam:**
- Compresses easily, recovers slowly
- Tire conforms to surface irregularities — more grip
- Better for outdoor, soft-pack, bumpy conditions
- Can cause traction rolling in high-grip situations
- Wears out faster than closed-cell

**Dual-Layer Foam:** Soft outer (traction) + firm inner (stability). Best of both worlds.

**For game modeling:** Foam insert type could affect tire vertical stiffness and contact patch behavior. Closed-cell = stiffer sidewall, less grip variation; Open-cell = softer sidewall, more grip but less predictable.

---

## Part 3: Surface-to-Tire Matching

### 3.1 Packed Clay (Outdoor, Groomed)

The most common competitive RC surface.

- **Tread:** Small-to-medium pins (Hole Shot 3.0, JConcepts Sprinter)
- **Compound:** Medium or Soft depending on moisture (M3/Blue when watered, M2/Gold when dry)
- **Why:** Hard surface needs smaller pins for precision. Medium compound balances grip and wear on abrasive clay.

### 3.2 Loose Dirt (Dusty, Ungroomed)

Low-traction conditions requiring maximum mechanical grip.

- **Tread:** Tall pins or step pins (JConcepts Double Dees V2, JConcepts Goose Bumps)
- **Compound:** Super Soft / Soft (M4/Green for cold; M3/Blue for warm)
- **Why:** Tall pins dig through loose material. Super soft compound maximizes adhesion on whatever hard surface exists beneath the dust.
- **Front tires:** Ribbed fronts (2WD) help cut through dust to find grip

### 3.3 Indoor Clay

Typically higher grip than outdoor due to controlled moisture and temperature.

- **Tread:** Bar tires or small pins (Pro-Line Electron/Positron, JConcepts Ellipse)
- **Compound:** Gold (clay-specific), Aqua, or MC
- **Why:** Indoor clay is smoother and more consistent. Bar treads maximize contact patch. Clay compounds are formulated for the specific adhesion properties of clay minerals.
- **Key difference from outdoor:** Indoor tracks are often treated/prepped, creating very high grip. Use connected-tread (bar) designs that won't be "edgy" in high traction.

### 3.4 Carpet / Astroturf

Indoor surfaces with consistent, high-grip characteristics.

- **Tread:** Spike tires (Schumacher Minispike for astroturf), Slicks or near-slicks for short-pile carpet, Pro-Line Electron 2.0 for high-bite surfaces
- **Compound:** Medium carpet-specific compounds (Schumacher Yellow for dry, Silver for wet)
- **Pile height matters:**
  - Tall pile (EOS carpet): Longer spikes to reach between fibers
  - Short pile (astroturf): Shorter spikes or mini-pins
  - Flat carpet: Near-slick or slick
- **Key:** "The taller the pile requires longer spikes, while AstroTurf and shorter pile carpet tracks can be driven using shorter spikes"

### 3.5 High-Grip "Blue Groove" Clay

The racing line becomes polished with deposited rubber, creating a dark blue-gray strip of extreme grip.

- **Tread:** Very small pins or "taper pins" / "fuzzy" tires (Pro-Line Fuzzy, Losi IFMAR Pin)
- **Compound:** Soft to Medium (paradoxically, slightly harder than you'd use on regular clay — the deposited rubber provides so much grip that too-soft compounds become "edgy" and unpredictable)
- **Why:** The rubber-coated surface provides extraordinary adhesion. Tread pattern matters less than compound selection. "No matter what you ran, you could hold it full throttle in the corners."
- **Key principle:** "The closer and finer the tread pattern, the more it should be used for blue groove."

### 3.6 Wet / Damp Clay

Moisture dramatically changes grip characteristics.

- **Tread:** Larger pins or step pins (to cut through water film), more aggressive patterns
- **Compound:** Softer than dry conditions. Pink clay compound (AKA) for damp sticky clay. Super soft for very wet.
- **Key changes from dry:**
  - Switch to softer compound (one grade softer than dry)
  - Switch to more aggressive tread (taller pins)
  - Some tires "pack up" with wet clay — choose patterns with good self-cleaning
  - Vent holes in tires become important for water drainage

### 3.7 Hard-Packed with Rocks/Debris

Abrasive surfaces that destroy soft compounds quickly.

- **Tread:** Medium pins with good durability (Pro-Line Hole Shot in S-series compound)
- **Compound:** Long-wear variants (S3/S5, AKA Long Wear). Standard soft compounds would be shredded in 1-2 runs.
- **Why:** Abrasive surfaces provide good mechanical grip from surface texture alone. Hard compound resists premature wear. Long-wear compounds accept slightly less peak grip for dramatically longer life.

### 3.8 Sand

Very loose, deep, non-compacting surface.

- **Tread:** Paddle tires or very aggressive knobby/lug patterns
- **Compound:** Hard — the tire needs to scoop and throw material, not adhere to it
- **For game modeling:** Sand is fundamentally different from all other surfaces. Grip comes from displacing material, not from friction. Very high rolling resistance.

### 3.9 Tarmac / Asphalt

Hard, smooth, high-grip surface.

- **Tread:** Slicks or very low-profile pins
- **Compound:** Soft to Medium depending on temperature and surface roughness
- **For game modeling:** Similar to blue groove but without the rubber deposit. High adhesion grip, low mechanical grip.

### 3.10 Grass

Soft, variable surface with root structure.

- **Tread:** Aggressive knobby (Pro-Line Trencher specifically designed for grass)
- **Compound:** Medium — needs structure to dig through grass blades to reach soil
- **Key:** "In grass, the Holeshots are not optimal; for those conditions the Trenchers will be your weapon of choice."

---

## Part 4: Game Modeling — Pacejka Parameter Mapping

### 4.1 Overview: What Changes Between Tire Types

In the Pacejka Magic Formula `F = D * sin(C * atan(B*x - E*(B*x - atan(B*x))))`:

- **D (Peak friction):** Primarily driven by compound softness and surface match
- **B (Cornering stiffness):** Driven by tread pattern and carcass design
- **C (Shape factor):** Driven by tread pattern — how the tire transitions from grip to slide
- **E (Curvature):** Driven by compound — how the tire behaves past peak slip angle

### 4.2 Grip Level (D) — Compound Softness Mapping

Based on real-world friction coefficient data, RC tire grip ranges from approximately 0.8 to 1.5 depending on compound and surface match:

| Compound Class | Relative Grip (D multiplier) | Notes |
|---------------|------------------------------|-------|
| Hard / Long-Wear | 0.85 - 0.95 | Baseline grip, maximum durability |
| Medium | 0.95 - 1.05 | Standard reference grip |
| Soft | 1.05 - 1.20 | Good grip, moderate wear |
| Super Soft | 1.15 - 1.30 | High grip, fast wear |
| Mega Soft | 1.25 - 1.40 | Maximum grip, very fast wear |

**Surface match multiplier** (applied on top of compound):
- Wrong compound for surface: 0.7 - 0.85x (e.g., soft compound on loose dirt = folding, less grip)
- Acceptable match: 0.90 - 1.0x
- Optimal match: 1.0 - 1.10x
- Perfect match (blue groove + soft): 1.10 - 1.20x

### 4.3 Response Sharpness (B) — Tread Pattern Effects

Cornering stiffness (B * C * D = initial slope) varies by tread type:

| Tread Type | B (Stiffness Factor) | Rationale |
|-----------|---------------------|-----------|
| Slick | 1.2 - 1.4 (highest) | Maximum contact patch, immediate response |
| Bar | 1.1 - 1.3 | Connected tread provides stable, predictable buildup |
| Small pin | 1.0 - 1.2 (reference) | Individual pins flex slightly before engaging |
| Medium pin | 0.9 - 1.1 | More flex in taller pins, slightly delayed response |
| Tall pin / knobby | 0.7 - 0.9 (lowest) | Significant pin flex before full engagement |
| Step pin | 0.85 - 1.05 | Two-stage engagement: short pins first, then tall pins |

**Directional bias:** Bar tires have higher B in the lateral direction than longitudinal. Pin tires are more isotropic (equal in all directions). Round pins are more isotropic than square blocks.

### 4.4 Breakaway Character (C, E) — Grip-to-Slide Transition

| Tread Type | C (Shape) | E (Curvature) | Breakaway Feel |
|-----------|-----------|----------------|----------------|
| Slick | 1.6 - 1.8 | -0.5 to 0.0 | Sharp, sudden breakaway. Very little warning. |
| Bar | 1.5 - 1.7 | -0.3 to 0.2 | Progressive but quick. Predictable. |
| Small pin | 1.3 - 1.6 | -0.2 to 0.3 | Gradual, forgiving. Easy to catch a slide. |
| Tall pin / knobby | 1.1 - 1.4 | 0.0 to 0.5 | Very gradual. Pins fold progressively. Mushy feel. |
| Step pin | 1.2 - 1.5 | -0.1 to 0.3 | Two-stage: initial grip, brief plateau, then slide. |

**Compound effect on breakaway:**
- Soft compounds: Slightly more progressive breakaway (lower C, higher E). The rubber deforms before releasing.
- Hard compounds: Sharper breakaway (higher C, lower E). Less warning.

### 4.5 Rolling Resistance

| Tread Type | Rolling Resistance Multiplier | Notes |
|-----------|------------------------------|-------|
| Slick | 1.0x (reference/lowest) | Smooth contact, no tread deformation energy |
| Bar | 1.05 - 1.10x | Slight bar deformation at contact patch edges |
| Small pin | 1.10 - 1.15x | Pin compression/release cycle consumes energy |
| Medium pin | 1.15 - 1.25x | More pin mass to deform per revolution |
| Tall pin / knobby | 1.25 - 1.40x | Significant energy lost to tread squirm |
| Paddle (sand) | 1.40 - 1.60x | Designed to displace material, not roll smoothly |

**Compound effect:** Softer compounds have 5-10% higher rolling resistance than harder compounds (more hysteresis in the rubber).

**Surface effect:** Rolling resistance increases on soft surfaces because the tire sinks in and must climb out of its own rut. On loose dirt/sand, rolling resistance can be 2-3x the hard-surface value.

### 4.6 Load Sensitivity

All rubber tires exhibit load sensitivity — grip coefficient decreases as vertical load increases. This is universal but tread pattern affects the rate:

| Tread Type | Load Sensitivity | Rationale |
|-----------|-----------------|-----------|
| Slick | Standard (reference) | Uniform contact patch pressure distribution |
| Bar | Slightly lower sensitivity | Connected tread distributes load more evenly |
| Pin | Slightly higher sensitivity | Individual pins have concentrated pressure points |
| Knobby | Higher sensitivity | Large lugs create very uneven pressure distribution |

**For Pacejka modeling:** Use the load sensitivity coefficient (typically 0.7-0.9 for RC tires) and modify it +-5% based on tread type.

### 4.7 Wear / Degradation Model

Proposed game wear system:

```
wear_rate = base_wear * compound_factor * surface_abrasion * speed_factor * slip_factor

Where:
  base_wear = 0.001 per second of contact (dimensionless, 0.0 = new, 1.0 = destroyed)
  compound_factor:
    Hard/Long-wear = 0.3
    Medium = 0.6
    Soft = 1.0
    Super Soft = 1.5
    Mega Soft = 2.5
  surface_abrasion:
    Carpet = 0.3
    Clay (smooth) = 0.5
    Clay (rough) = 0.8
    Packed dirt = 0.7
    Loose dirt = 0.4 (low abrasion but high pin stress)
    Gravel = 1.5
    Tarmac = 1.0
    Sand = 0.6
  speed_factor = 1.0 + (speed_mps / 20.0) * 0.5  (faster = more wear)
  slip_factor = 1.0 + abs(slip_ratio) * 2.0 + abs(slip_angle_rad) * 3.0
```

**Grip degradation curve:**

```
grip_multiplier = 1.0 - (wear * wear * 0.3)  # Quadratic: grip holds up initially, then drops

At wear = 0.0: grip = 1.0 (full)
At wear = 0.3: grip = 0.97 (barely noticeable)
At wear = 0.5: grip = 0.925 (starting to feel it)
At wear = 0.7: grip = 0.853 (significantly degraded)
At wear = 1.0: grip = 0.70 (30% grip loss, tire is done)
```

**Temperature-dependent wear:**

```
temp_wear_factor:
  Below optimal range: 1.0 (no extra wear, just less grip)
  In optimal range: 1.0
  Above optimal range: 1.5 - 2.0 (overheating causes rapid degradation/graining)
```

---

## Part 5: Proposed Tire System for R8EO-X

### 5.1 Tire Types to Implement

Based on the research, the following tire types cover the full spectrum of real RC racing:

| ID | Name | Type | Best Surfaces | Game Role |
|----|------|------|---------------|-----------|
| `pin_small` | Mini Pin | Small pin | Hard clay, blue groove, dust | All-rounder, default |
| `pin_tall` | Mega Pin | Tall pin | Loose dirt, loam, wet | Loose surface specialist |
| `step_pin` | Step Pin | Step/staggered | Transitional, mixed | Versatile off-road |
| `bar` | Bar Tread | Connected bars | Indoor clay, carpet, treated | High-grip specialist |
| `knobby` | Knobby | Aggressive lug | Mud, grass, sand, wet | Extreme loose/soft |
| `slick` | Slick | No tread | Carpet, tarmac, blue groove | Maximum grip (clean surfaces only) |
| `hole_shot` | Hole Shot | Dense multi-pin | Hard-pack, dusty, grooved | Popular all-rounder |

### 5.2 Compound Grades to Implement

Simplified to 5 grades that cover the real-world spread:

| ID | Name | Grip Mult | Wear Mult | Optimal Temp (°C) | Temp Window |
|----|------|-----------|-----------|--------------------|----|
| `hard` | Hard / Long-Wear | 0.90 | 0.30 | 30-45 | Wide (+-15°C) |
| `medium` | Medium | 1.00 | 0.60 | 25-38 | Medium (+-10°C) |
| `soft` | Soft | 1.10 | 1.00 | 18-30 | Medium (+-8°C) |
| `super_soft` | Super Soft | 1.20 | 1.50 | 10-22 | Narrow (+-6°C) |
| `mega_soft` | Mega Soft | 1.30 | 2.50 | 0-15 | Narrow (+-5°C) |

### 5.3 Surface Compatibility Matrix

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

### 5.4 Pacejka Parameter Deltas by Tire Type

Base Pacejka values modified by tire type:

| Tire Type | D | B | C | E | Roll Resist |
|-----------|------|------|------|-------|-------------|
| `pin_small` | 1.00 | 1.05 | 1.40 | 0.10 | 1.12 |
| `pin_tall` | 1.00 | 0.85 | 1.25 | 0.25 | 1.30 |
| `step_pin` | 1.00 | 0.95 | 1.35 | 0.15 | 1.20 |
| `bar` | 1.05 | 1.15 | 1.60 | 0.00 | 1.07 |
| `knobby` | 1.00 | 0.80 | 1.20 | 0.30 | 1.35 |
| `slick` | 1.10 | 1.30 | 1.70 | -0.20 | 1.00 |
| `hole_shot` | 1.02 | 1.00 | 1.40 | 0.05 | 1.15 |

### 5.5 AI Tire Selection Logic

AI drivers should select tires based on:

1. **Surface type** of the track -> pick tire with highest compatibility from matrix
2. **Difficulty level** affects selection quality:
   - CHAMPION: Always picks optimal tire+compound
   - EXPERT: 80% chance of optimal, 20% chance of one-off selection
   - INTERMEDIATE: 60% optimal
   - BEGINNER: Random from "reasonable" options

---

## Sources

- [AMain Hobbies — Choosing the Right Off-Road RC Tires](https://www.amainhobbies.com/choosing-the-right-offroad-rc-tires-and-wheels/cp1106)
- [AMain Hobbies — Choosing Correct Tires Part 1: Compounds by Adam Drake](https://www.amainhobbies.com/choosing-the-correct-tires-part-1-compounds-by-adam-drake/cp1337)
- [AMain Hobbies — Choosing Correct Tires Part 2: Tread Patterns by Adam Drake](https://www.amainhobbies.com/choosing-the-correct-tires.-part-2-tread-patterns-by-adam-drake/cp1338)
- [AMain Hobbies — Carpet Racing: Selecting the Right RC Carpet Tire](https://www.amainhobbies.com/carpet-racing-selecting-the-right-rc-carpet-tire/cp1082)
- [JConcepts 1/10th Scale Outdoor Off-Road Tire Guide](https://blog.jconcepts.net/2023/06/jconcepts-1-10th-scale-outdoor-off-road-tire-guide/)
- [JConcepts Tire Reference and Comparison Packet](https://blog.jconcepts.net/2020/10/jconcepts-tire-reference-and-comparison-packet/)
- [JConcepts Compound Key & Tire Reference Packet](https://jconcepts.net/Compound-Key-Tire-Reference-Packet_df_58.html)
- [Pro-Line Tire Compound Chart (PDF)](https://www.prolineracing.com/on/demandware.static/-/Sites-horizon-master/default/dwc229b8f3/Manuals/PL-Tire-Compound-Chart.pdf)
- [Pro-Line Factory Team — Adam Drake's M and S Compound Breakdown](https://blog.prolineracing.com/2020/02/05/adam-drakes-breakdown-of-pro-line-m-and-s-tire-compounds/)
- [Pro-Line Hole Shot Review — Big Squid RC](https://www.bigsquidrc.com/rc-reviews/pro-line-racing-holeshot-tire-review/)
- [AKA Tire Compound Chart (PDF)](https://www.horizonhobby.com/on/demandware.static/-/Sites-horizon-master/default/dwf7ecd6ab/Manuals/AKA-Tire-Compound-Guide.pdf)
- [AKA Tyre Chart Selection — RSRC](https://rsrc.biz/en/blog/aka-tyre-chart-selection-n38)
- [EuroRC — RC Tire Size Chart & Selection Guide](https://www.eurorc.com/page/100/rc-tire-size-chart--selection-guide-110-to-18-scale)
- [Hardcore RC Buggy — Choosing the Right Tyre for Track Conditions](http://hardcorercbuggy.blogspot.com/2011/08/choosing-right-tyre-for-right-track.html)
- [RC Tech Forums — Tire Compounds Discussion](https://www.rctech.net/forum/electric-off-road/1017151-tire-compounds-one-when-why-does-really-matter.html)
- [RC Tech Forums — Pro-Line Compound Question](https://www.rctech.net/forum/electric-off-road/1063714-pro-line-racing-tire-compound-question.html)
- [Sodialed — RC Off-road Tire Tread Pattern Selection Guide](https://www.sodialed.com/rc-setup-tips/choosing-rc-off-road-tire-tread-patterns-with-adam-drake)
- [Hearns Hobbies — RC Tyre Compounds Guide](https://www.hearnshobbies.com/blogs/radio-control/rc-tyre-compounds-a-guide-to-grip-speed-control)
- [Absolute Hobbyz — RC Car Tires Complete Guide](https://www.absolutehobbyz.com/rc-car-tires-guide.html)
- [LiveRC — Cactus: Slick and Sticky Tire Trend](https://www.liverc.com/news/cactus-slick-and-sticky-four-major-tire-manufacturers-ditch-the-tread/)
- [LiveRC — Analysis: The Tire Additive Bans](https://www.liverc.com/news/analysis-the-tire-additive-bans/)
- [RaceNRCs — What Is RC Tire Sauce?](https://racenrcs.com/what-is-rc-tire-sauce/)
- [RCUniverse — Blue Groove Explained](https://www.rcuniverse.com/forum/rc-car-general-discussions-179/5693732-what-%22blue-groove%22.html)
- [Horizon Hobby — How to Choose RC Tires](https://www.horizonhobby.com/blog-article-rc-tires/)
