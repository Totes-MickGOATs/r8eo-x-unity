# Unity Damage, Wear & Progression for RC Racing

Comprehensive system for RC racing damage, wear, career progression, and vehicle upgrades.
All designs are specific to 1/10-scale electric RC racing (brushless motors, LiPo batteries,
independent suspension, lexan bodies).

## When to Use

- Implementing collision-based vehicle damage with visual and physics consequences
- Modeling tire wear, battery discharge, or motor degradation
- Building a repair/maintenance economy between races
- Designing career mode progression (clubs, championships, unlocks)
- Creating a vehicle upgrade and tuning system
- Implementing currency economy and unlock gates

## When NOT to Use

- Physics engine setup (solver, timestep, layers) -- use `unity-physics-tuning`
- ScriptableObject fundamentals -- use `unity-scriptable-objects`
- Save/load implementation details -- use `unity-save-load`
- Shader authoring basics for damage visuals -- use `unity-shaders`
- General state machine patterns -- use `unity-state-machines`

---

## 1. Visual Damage System

### Shader-Driven Damage

Two global shader properties drive all visual damage on the vehicle body:

| Property | Range | Effect |
|----------|-------|--------|
| `_DamageAmount` | 0.0-1.0 | Crack overlay, vertex displacement, color desaturation |
| `_DirtAmount` | 0.0-1.0 | Procedural dirt/dust accumulation from driving surface |

Both properties are set per-material instance via `MaterialPropertyBlock` to avoid
creating material copies. The values are driven by the `VehicleDamageState` component
which aggregates zone damage into a single 0-1 value for the shader.

```csharp
// Example: updating damage visuals each frame via MaterialPropertyBlock (no material copies)
_propBlock.SetFloat(_damageAmountId, vehicleDamage.NormalizedTotal);
_propBlock.SetFloat(_dirtAmountId, vehicleDamage.DirtAccumulation);
renderer.SetPropertyBlock(_propBlock);
```

### Decal System

Impact decals are spawned at collision contact points using Unity URP Decal Projector:

- **Scratch decals** -- low-force glancing impacts, elongated along velocity vector
- **Dent decals** -- medium-force impacts, circular with depth displacement
- **Crack decals** -- high-force impacts, radial crack pattern from contact point

Decals are pooled (max 32 per vehicle) and sorted by severity -- new decals replace the
least severe when the pool is full. Decals persist across the race but are cleared on repair.

### LOD Body Swap

At high cumulative damage (> 0.75 normalized), the pristine body mesh is swapped for a
pre-modeled damaged variant:

| LOD Level | Condition | Mesh |
|-----------|-----------|------|
| LOD0 | Damage < 0.25 | Pristine body |
| LOD1 | Damage 0.25-0.75 | Minor dents/scratches baked into mesh |
| LOD2 | Damage > 0.75 | Heavy damage mesh (bent body posts, cracked windshield) |

Body swap is handled by `DamageBodySwapper` which listens to `VehicleDamageState.OnDamageThresholdCrossed`.

---

## 2. Physics Damage Modifiers

Damage directly affects vehicle handling through modifier values applied to the physics
simulation. Each modifier is a multiplier (1.0 = undamaged, 0.0 = fully broken).

### Modifier Types

| Modifier | Affected System | Damage Effect |
|----------|----------------|---------------|
| `SteeringBias` | Steering servo | Adds constant offset to steering angle (pulls left/right) |
| `SteeringRange` | Steering servo | Reduces maximum steering angle |
| `MotorEfficiency` | Motor/ESC | Reduces torque output as a percentage |
| `SuspensionTravel` | Suspension | Reduces available suspension travel (stiffer response) |
| `SuspensionDamping` | Suspension | Alters damping coefficient (can increase or decrease) |
| `GripFront` | Front tires | Multiplier on front tire friction coefficient |
| `GripRear` | Rear tires | Multiplier on rear tire friction coefficient |
| `DragCoefficient` | Aerodynamics | Increases drag from bent/misaligned body panels |

### Modifier Application

Modifiers are stored in `DamageModifierSet` (a plain C# struct) and applied each
physics tick by `DamagePhysicsBridge`:

```csharp
float effectiveTorque = baseTorque * damageModifiers.MotorEfficiency;
float effectiveSteer = requestedSteer * damageModifiers.SteeringRange + damageModifiers.SteeringBias;
float effectiveGripF = baseFriction * damageModifiers.GripFront;
```

Modifiers are recalculated only when zone damage changes, not every frame.

---

## 3. Damage Zones

The vehicle is divided into 6 damage zones, each with independent health and force thresholds.

### Zone Layout

```
         +---------------+
         |  FRONT (Z1)   |
    +----+               +----+
    | FL |   CHASSIS     | FR |
    |(Z3)|    (Z5)       |(Z4)|
    +----+               +----+
         |  REAR (Z2)    |
         +---------------+
              UNDERCARRIAGE (Z6)
```

| Zone | ID | Max HP | Primary Effect |
|------|----|--------|----------------|
| Front | Z1 | 100 | Steering damage, body visual damage |
| Rear | Z2 | 100 | Motor mount stress, rear grip loss |
| Front-Left | Z3 | 80 | Left front suspension, steering bias left |
| Front-Right | Z4 | 80 | Right front suspension, steering bias right |
| Chassis | Z5 | 150 | Motor efficiency, ESC thermal stress |
| Undercarriage | Z6 | 120 | Suspension travel, ground clearance |

### Force Thresholds

Collision forces below the minimum threshold are ignored (curb taps, minor contact).
Forces above the critical threshold cause immediate component failure.

| Threshold | Force (N) | Result |
|-----------|-----------|--------|
| Ignored | < 50 | No damage registered |
| Minor | 50-200 | 1-5 HP damage, cosmetic only |
| Moderate | 200-800 | 5-25 HP damage, physics modifiers begin |
| Severe | 800-2000 | 25-60 HP damage, significant handling loss |
| Critical | > 2000 | 60-100 HP damage, potential component failure |

### Zone-to-Modifier Mapping

Each zone contributes to specific physics modifiers when damaged. The contribution
is weighted -- front zone damage affects steering more than chassis damage does:

```csharp
SteeringBias += Z3.DamagePercent * -0.15f;  // pulls left
SteeringBias += Z4.DamagePercent * +0.15f;  // pulls right
SteeringRange *= 1.0f - (Z1.DamagePercent * 0.4f);
MotorEfficiency *= 1.0f - (Z5.DamagePercent * 0.5f);
MotorEfficiency *= 1.0f - (Z2.DamagePercent * 0.2f);
GripFront *= 1.0f - (Z3.DamagePercent * 0.3f + Z4.DamagePercent * 0.3f);
GripRear *= 1.0f - (Z2.DamagePercent * 0.4f);
SuspensionTravel *= 1.0f - (Z6.DamagePercent * 0.6f);
DragCoefficient *= 1.0f + (Z1.DamagePercent * 0.3f + Z2.DamagePercent * 0.2f);
```

### Collision Detection

Zone assignment uses the collision contact point local-space position relative
to the vehicle root. A `DamageZoneResolver` maps world-space `ContactPoint` data
to the nearest zone using axis-aligned bounding regions defined in `DamageZoneConfig`.

---

## 4. Repair System

Two repair tiers reflect real RC pit stop options.

### Quick Repair

- **Duration:** 5-15 seconds (scales with damage)
- **Cost:** Low (proportional to damage repaired)
- **Effect:** Restores each zone by up to 50% of its missing HP
- **Limits:** Cannot restore past 75% max HP per zone
- **Visual:** Removes decals, blends `_DamageAmount` back toward LOD0 threshold
- **When:** Available between heats, during pit window in endurance events

### Full Repair

- **Duration:** 30-60 seconds (or between-event only in career)
- **Cost:** High (parts + labor)
- **Effect:** Restores all zones to 100% HP
- **Visual:** Full decal clear, body swap back to LOD0, `_DamageAmount` = 0
- **When:** Between events in career mode, any time in free practice

### Repair Cost Formula

```csharp
quickRepairCost = sum(zone.MissingHP * zone.RepairCostPerHP * 0.5f);
fullRepairCost  = sum(zone.MissingHP * zone.RepairCostPerHP * 1.0f) + laborFlat;
```

`RepairCostPerHP` varies per zone -- chassis repairs cost more than cosmetic front damage.

---

## 5. Tire Wear System

### Non-Linear Wear Curve

Tire grip does not degrade linearly. The wear curve follows a three-phase model:

| Phase | Wear Range | Grip Behavior |
|-------|-----------|---------------|
| Break-in | 0-5% | Grip increases slightly from 1.0 to peak |
| Plateau | 5% to cliff onset | Stable grip, long duration, gradual decline |
| Cliff | Cliff onset to 100% | Rapid falloff to floor value |

Phase transitions are defined per compound in the `TireCompoundSO` ScriptableObject.

### Wear Accumulation

Wear is accumulated per-tire based on:

| Factor | Weight | Description |
|--------|--------|-------------|
| Slip angle | 0.4 | Lateral sliding wears tread faster |
| Slip ratio | 0.3 | Wheelspin/lockup accelerates wear |
| Surface abrasion | 0.2 | Asphalt > dirt > carpet |
| Temperature | 0.1 | Over-temperature accelerates degradation |

```csharp
wearDelta = dt * wearRate * (
    slipAngleFactor * 0.4f +
    slipRatioFactor * 0.3f +
    surfaceAbrasion * 0.2f +
    tempFactor * 0.1f
);
```

### Tire Compounds -- `TireCompoundSO`

Three compounds available as ScriptableObject assets:

| Compound | Grip Peak | Wear Rate | Cliff Onset | Use Case |
|----------|-----------|-----------|-------------|----------|
| **Soft (Pink)** | 1.05 | 1.8x | 55% wear | Sprint races, qualifying |
| **Medium (Blue)** | 1.00 | 1.0x | 70% wear | General purpose, most events |
| **Hard (White)** | 0.92 | 0.6x | 85% wear | Endurance, high-abrasion surfaces |

`TireCompoundSO` fields:

```csharp
[CreateAssetMenu(menuName = "R8EO/Tires/Compound")]
public class TireCompoundSO : ScriptableObject
{
    [Header("Grip")]
    public float PeakGripMultiplier;
    public AnimationCurve WearToGripCurve;  // x=wear%, y=grip multiplier

    [Header("Wear")]
    public float BaseWearRate;
    public float CliffOnsetPercent;
    public float CliffGripFloor;

    [Header("Temperature")]
    public float OptimalTempMin;
    public float OptimalTempMax;
    public AnimationCurve TempToGripCurve;

    [Header("Visual")]
    public Color TireColor;
    public string CompoundName;
}
```

### Tire Change

Tires can be changed during pit stops. Changing compound mid-event is a strategic
decision -- fresh softs for a sprint finish vs. nursing hards to the end.

---

## 6. LiPo Battery Simulation

Accurate to real 2S/3S LiPo behavior used in 1/10 scale RC racing.

### Voltage Discharge Curve (Flat-Then-Drop)

LiPo cells maintain near-constant voltage for most of their capacity, then drop
sharply near depletion:

| Charge State | Voltage per Cell | Behavior |
|-------------|-----------------|----------|
| Full | 4.20V | Peak, brief |
| Nominal plateau | 3.70-4.10V | Flat, ~80% of capacity |
| Knee | 3.50V | Curve steepens |
| Rapid drop | 3.30-3.50V | Voltage falls quickly |
| LVC cutoff | 3.00V | ESC cuts motor power |

### Voltage Sag Under Load

Real LiPo packs experience voltage drop under high current draw. This is modeled
as an internal resistance that increases with discharge and age:

```csharp
V_terminal = V_opencircuit - (I_draw * R_internal);
R_internal = baseResistance * (1.0f + ageDegradation) * tempFactor;
```

High-throttle situations cause temporary voltage sag that reduces motor power,
recovering when throttle is released. This creates a natural power management dynamic.

### Low Voltage Cutoff (LVC)

The ESC enforces LVC to protect the battery:

| Stage | Voltage/Cell | ESC Response |
|-------|-------------|--------------|
| Normal | > 3.50V | Full power available |
| Warning | 3.30-3.50V | Power reduced to 75% (ESC thermal protection) |
| Cutoff | < 3.00V | Motor disabled, coast to stop |

LVC triggers `OnLowVoltageWarning` and `OnLowVoltageCutoff` events for UI and audio.

### Battery Degradation (Long-Term)

Over a career, batteries lose capacity and gain internal resistance:

```csharp
public class BatteryState
{
    public float MaxCapacityMah;        // Degrades over charge cycles
    public float InternalResistanceOhm; // Increases over charge cycles
    public int ChargeCycles;            // Lifetime counter
    public float HealthPercent;         // Derived: capacity / original capacity
}
```

Degradation per cycle:
- Capacity loss: 0.1-0.3% per cycle (depends on discharge rate, storage voltage)
- Resistance increase: 0.05-0.15% per cycle

Players must eventually replace batteries -- a key career expense.

### Battery Configurations

| Config | Cells | Nominal V | Use |
|--------|-------|-----------|-----|
| 2S | 2 | 7.4V | Stock/sportsman class |
| 3S | 3 | 11.1V | Modified/pro class |

Higher cell count = more power but faster wear on motor and drivetrain components.

---

## 7. Motor Wear System

### Motor Efficiency Degradation

Brushless motors lose efficiency over time due to bearing wear, magnet degradation,
and sensor drift:

```csharp
currentEfficiency = baseEfficiency * (1.0f - wearPercent * maxEfficiencyLoss);
```

| Factor | Contribution | Description |
|--------|-------------|-------------|
| Runtime hours | 0.4 | Total motor-on time |
| Heat cycles | 0.3 | Number of times motor exceeded thermal limit |
| Peak current events | 0.2 | Hard acceleration from standstill |
| Crash stress | 0.1 | Sudden stops from collisions |

### Motor Heat Model

Motor temperature affects efficiency and accelerates wear:

```csharp
dTemp_dt = (I_sq * R_winding * heatGenFactor) - (coolingRate * (T_motor - T_ambient));
```

| Temp Range | Effect |
|------------|--------|
| < 60C | Optimal -- no penalties |
| 60-80C | Minor efficiency loss (2-5%) |
| 80-100C | Significant loss (5-15%), accelerated wear |
| > 100C | Thermal protection: ESC reduces power, rapid wear |

### Motor Upgrade Tiers

Motors are rated by turn count -- lower turns = more power, more heat, more wear:

| Turn Rating | Class | Power | Heat Gen | Wear Rate | Unlock |
|-------------|-------|-------|----------|-----------|--------|
| 21.5T | Stock | Low | Low | 0.5x | Default |
| 17.5T | Sportsman | Medium | Medium | 0.8x | Tier 2 |
| 13.5T | Modified | High | High | 1.2x | Tier 3 |
| 10.5T | Pro | Very High | Very High | 1.8x | Tier 4 |

Each tier is a `MotorSpecSO` ScriptableObject with KV rating, max current, thermal
characteristics, and cost.

---

## 8. Career Mode

### Tier Structure

| Tier | Name | Motor Class | Events | Unlock Condition |
|------|------|------------|--------|------------------|
| 1 | **Clubman** | 21.5T Stock | Club races, time trials | Default |
| 2 | **Sportsman** | 17.5T | Regional events, enduros | 500 XP + Tier 1 champion |
| 3 | **Modified** | 13.5T | National events, team relay | 2000 XP + Tier 2 champion |
| 4 | **Pro** | 10.5T | Pro series, invitational | 5000 XP + Tier 3 champion |

### Event Types

| Event | Format | Duration | Damage Impact |
|-------|--------|----------|---------------|
| **Sprint** | 3 heats + main | 5 min/heat | Accumulates across heats |
| **Endurance** | Single long race | 15-30 min | Tire/battery strategy critical |
| **Time Trial** | Solo, best lap | 3 laps | Minimal (no contact) |
| **Team Relay** | Tag-team, shared car | 20 min | Shared damage pool |
| **Invitational** | Special rules | Varies | Event-specific modifiers |

### XP System

| Action | XP Reward |
|--------|-----------|
| Finish a race | 10-50 (scales with tier) |
| Podium finish (1st/2nd/3rd) | +25/+15/+10 bonus |
| Clean race (no major collisions) | +20 bonus |
| Fastest lap | +10 bonus |
| Win championship round | +100 bonus |
| Complete endurance event | +40 bonus |

---

## 9. Vehicle Upgrades -- `VehicleUpgradeSO`

### Upgrade ScriptableObject Template

```csharp
[CreateAssetMenu(menuName = "R8EO/Upgrades/Vehicle Upgrade")]
public class VehicleUpgradeSO : ScriptableObject
{
    [Header("Identity")]
    public string UpgradeId;
    public string DisplayName;
    public string Description;
    public Sprite Icon;
    public UpgradeCategory Category;

    [Header("Tier")]
    public int TierRequired;        // Career tier needed to purchase
    public int UpgradeLevel;        // 1, 2, 3 within category
    public VehicleUpgradeSO Prerequisite;  // Must own this first (null = none)

    [Header("Cost")]
    public int PurchasePrice;
    public int InstallLaborCost;
    public float InstallTimeSeconds;

    [Header("Effects")]
    public UpgradeModifier[] Modifiers;

    [Header("Compatibility")]
    public VehicleClassSO[] CompatibleClasses;
}

[System.Serializable]
public struct UpgradeModifier
{
    public ModifierTarget Target;   // enum: MaxSpeed, Acceleration, Grip, etc.
    public ModifierOp Operation;    // enum: Add, Multiply, Override
    public float Value;
}
```

### Upgrade Categories and Linear Tiers

Upgrades within each category follow a strict linear progression (Level 1 must be
owned before Level 2, etc.):

| Category | Lvl 1 | Lvl 2 | Lvl 3 |
|----------|-------|-------|-------|
| **Motor** | 17.5T swap | 13.5T swap | 10.5T swap |
| **Battery** | 2S high-C | 3S standard | 3S high-C |
| **Suspension** | Aluminum shocks | Big bore shocks | Competition springs kit |
| **Tires** | Medium compound | Soft compound access | All compounds + warmers |
| **Steering** | High-torque servo | Aluminum servo horn | Direct-drive steering |
| **Drivetrain** | Ball diff | Spool conversion | Lightweight drivetrain |
| **Body** | Reinforced body clips | Lightweight body | Aero body kit |
| **Electronics** | Gyro stabilizer | Telemetry module | Data logger + overlay |

---

## 10. Currency Economy

### Earning

| Source | Amount | Frequency |
|--------|--------|-----------|
| Race finish | 50-200 | Per event |
| Podium bonus | +100/+60/+30 | Per event |
| Clean race bonus | +50 | Per event |
| Championship win | 500-2000 | Per series |
| Daily challenge | 75-150 | Daily |
| Milestone rewards | 200-1000 | One-time |

### Spending

| Expense | Cost Range | Frequency |
|---------|-----------|-----------|
| Quick repair | 20-100 | After races |
| Full repair | 100-500 | Between events |
| Tire set | 50-200 | Every 2-4 races |
| Battery replacement | 200-800 | Every 20-40 races |
| Motor service | 150-600 | Every 10-20 races |
| Upgrade purchase | 100-3000 | Progression |
| Upgrade install | 25-200 | With purchase |

### Balance Target

Players should be able to maintain their vehicle and slowly save for upgrades through
regular racing. Podium finishes accelerate progression. The economy should feel tight
in Tier 1, comfortable in Tier 2, and investment-heavy in Tiers 3-4 where component
costs scale up but rewards do too.

- Average income per event: ~150 credits
- Average maintenance per event: ~60 credits
- Net savings per event: ~90 credits
- Events to afford a mid-tier upgrade: ~10-15 events

---

## 11. Tuning System (3-Tier Progressive Disclosure)

Tuning settings are revealed progressively as the player advances, preventing
information overload for new players.

### Tier 1 -- Basic (Clubman)

Accessible immediately. Simple slider interface with descriptive labels.

| Setting | Range | Label |
|---------|-------|-------|
| Ride Height | Low/Med/High | "How high the car sits" |
| Spring Stiffness | Soft/Med/Firm | "How bouncy the car is" |
| Steering Rate | Slow/Med/Fast | "How quickly the car turns" |

### Tier 2 -- Intermediate (Sportsman)

Unlocked at Tier 2. Numeric values with visual feedback.

| Setting | Range | Unit |
|---------|-------|------|
| Front/Rear Ride Height | 15-35 | mm |
| Front/Rear Spring Rate | 2.0-8.0 | lb/in |
| Front/Rear Camber | -3.0 to 0.0 | degrees |
| Front/Rear Toe | -2.0 to 2.0 | degrees |
| Steering Dual Rate | 50-100 | % |
| Steering Expo | 0-50 | % |

### Tier 3 -- Advanced (Modified/Pro)

Full parameter access. Data-driven interface with telemetry integration.

| Setting | Range | Unit |
|---------|-------|------|
| All Tier 2 settings | -- | -- |
| Shock Oil Weight | 20-60 | wt |
| Piston Holes | 1-4 | count |
| Diff Oil (F/R) | 1000-100000 | cst |
| Spur/Pinion Gear Ratio | 2.0-5.0 | ratio |
| Droop (F/R) | 0-10 | mm |
| Anti-Roll Bar (F/R) | Off/Thin/Thick | -- |
| Motor Timing | 0-30 | degrees |
| ESC Punch | 1-10 | level |
| Brake Drag | 0-50 | % |

### Tuning Presets

Players can save/load tuning presets per track. Presets are stored as `TuningPresetSO`
ScriptableObjects at design time and serialized to JSON at runtime for player-created presets.

---

## 12. Implementation Priority

### Phase 1 -- Core Damage (MVP)

**Goal:** Collisions have consequences. Players notice damage affecting handling.

| Task | Priority | Dependencies |
|------|----------|-------------|
| `DamageZoneConfig` ScriptableObject | P0 | None |
| `VehicleDamageState` component | P0 | DamageZoneConfig |
| Collision detection and zone mapping | P0 | VehicleDamageState |
| `DamageModifierSet` struct | P0 | None |
| `DamagePhysicsBridge` -- apply modifiers | P0 | DamageModifierSet |
| `_DamageAmount` shader property hookup | P1 | VehicleDamageState |
| Quick/Full repair logic | P1 | VehicleDamageState |
| Damage HUD (health bars per zone) | P1 | VehicleDamageState |
| Decal spawning on impact | P2 | VehicleDamageState |
| LOD body swap | P2 | Damaged body meshes |

### Phase 2 -- Wear and Consumables

**Goal:** Strategic resource management during and between races.

| Task | Priority | Dependencies |
|------|----------|-------------|
| `TireCompoundSO` ScriptableObject | P0 | None |
| Tire wear accumulation per-wheel | P0 | TireCompoundSO |
| Wear-to-grip curve evaluation | P0 | TireCompoundSO |
| `BatteryState` + discharge model | P0 | None |
| Voltage sag under load | P1 | BatteryState |
| LVC stages (warning, cutoff) | P1 | BatteryState |
| Motor heat model | P1 | None |
| Motor wear accumulation | P2 | Motor heat model |
| Tire change during pit stop | P2 | TireCompoundSO |
| Battery degradation (career) | P2 | BatteryState |

### Phase 3 -- Career and Progression

**Goal:** Long-term progression loop with meaningful choices.

| Task | Priority | Dependencies |
|------|----------|-------------|
| `VehicleUpgradeSO` ScriptableObject | P0 | None |
| Upgrade purchase and install flow | P0 | VehicleUpgradeSO, Currency |
| Currency earn/spend system | P0 | None |
| Career tier structure | P0 | XP system |
| XP earn and tier unlock logic | P1 | Career tier structure |
| Event type definitions | P1 | None |
| Tuning Tier 1 (basic sliders) | P1 | None |
| Tuning Tier 2 (intermediate) | P2 | Career tier 2 unlock |
| Tuning Tier 3 (advanced) | P2 | Career tier 3 unlock |
| Tuning presets save/load | P2 | Tuning system |
| Championship series structure | P2 | Event types |

---

## 13. Key Interfaces

### Events (Signal Up)

```csharp
// VehicleDamageState
event Action<DamageZone, float> OnZoneDamaged;          // zone, damageAmount
event Action<DamageZone> OnZoneDestroyed;                // zone HP reached 0
event Action<float> OnTotalDamageChanged;                // normalized 0-1
event Action<int> OnDamageThresholdCrossed;              // LOD level

// BatterySimulation
event Action<float> OnVoltageChanged;                    // terminal voltage
event Action OnLowVoltageWarning;                        // 3.3-3.5V/cell
event Action OnLowVoltageCutoff;                         // < 3.0V/cell

// TireWearTracker
event Action<int, float> OnTireWearChanged;              // wheelIndex, wearPercent
event Action<int> OnTireWornOut;                          // wheelIndex

// MotorWearTracker
event Action<float> OnEfficiencyChanged;                 // current efficiency
event Action OnOverheatWarning;                          // > 80C
event Action OnThermalShutdown;                          // > 100C
```

### Data Flow

```
Collision --> DamageZoneResolver --> VehicleDamageState --> DamageModifierSet
                                          |                       |
                                          v                       v
                                 Visual Systems            Physics Systems
                             (shader, decals, LOD)     (steering, motor, grip)

Gameplay --> TireWearTracker ----> grip modifier per wheel
          -> BatterySimulation --> motor power limit
          -> MotorWearTracker ---> efficiency modifier
```

---

## 14. ScriptableObject Asset Organization

```
Assets/Data/
  Damage/
    DamageZoneConfig_Buggy.asset
    DamageZoneConfig_Truggy.asset
  Tires/
    TireCompound_Soft.asset
    TireCompound_Medium.asset
    TireCompound_Hard.asset
  Motors/
    MotorSpec_21_5T.asset
    MotorSpec_17_5T.asset
    MotorSpec_13_5T.asset
    MotorSpec_10_5T.asset
  Upgrades/
    Motor/
    Battery/
    Suspension/
    ...per category
  Tuning/
    TuningPreset_Default.asset
    TuningPreset_HighGrip.asset
  Career/
    CareerTier_Clubman.asset
    CareerTier_Sportsman.asset
    CareerTier_Modified.asset
    CareerTier_Pro.asset
```

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-scriptable-objects`** | Template pattern for `TireCompoundSO`, `VehicleUpgradeSO`, `MotorSpecSO`, `DamageZoneConfig` |
| **`unity-save-load`** | Persisting career state, battery degradation, upgrade inventory, tuning presets to JSON |
| **`unity-physics-3d`** | Collision detection, contact point processing, force-based damage calculation |
