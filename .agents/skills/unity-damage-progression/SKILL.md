# Unity Damage, Wear & Progression for RC Racing

Use this skill when implementing vehicle damage modeling, component wear simulation, or career progression systems for an RC racing simulator. Covers damage zones, visual damage, physics modifiers, tire wear, battery/motor simulation, repair economy, career tiers, vehicle upgrades, and unlock systems.

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

## Damage Architecture

### Three-Layer Visual Damage

| Layer | Mechanism | Performance | When to Apply |
|-------|-----------|-------------|---------------|
| **Shader-based** | `_DamageAmount` float per material | Cheapest | Always (Light-Critical) |
| **Decals** | URP Decal Projector for scratches/scuffs | Medium | On impact (Medium-Heavy) |
| **LOD body swap** | Replace mesh with pre-damaged version | Expensive (one-time) | Critical damage threshold |

```csharp
// Shader-based damage: blend from clean to damaged appearance
vehicleRenderer.material.SetFloat("_DamageAmount", damageNormalized); // 0 = pristine, 1 = wrecked
vehicleRenderer.material.SetFloat("_DirtAmount", dirtNormalized);     // Accumulates over race
```

### Physics Damage Modifiers

Damage affects vehicle handling, not just visuals:

```csharp
[System.Serializable]
public class DamageState
{
    [Range(0f, 1f)] public float steeringBias;         // Pulls left/right (0 = straight)
    [Range(0f, 1f)] public float motorEfficiency;       // 1 = full power, 0.5 = half power
    [Range(0f, 1f)] public float suspensionTravel;      // Reduced travel = harsher ride
    [Range(0f, 1f)] public float wheelGripMultiplier;   // Per-wheel grip reduction

    public static DamageState Pristine => new DamageState
    {
        steeringBias = 0f,
        motorEfficiency = 1f,
        suspensionTravel = 1f,
        wheelGripMultiplier = 1f
    };
}
```

Application in vehicle physics:

```csharp
void ApplyDamageToPhysics(DamageState damage)
{
    // Steering bias: add constant offset to steering input
    float effectiveSteering = steeringInput + damage.steeringBias;

    // Motor efficiency: reduce applied torque
    float effectiveTorque = motorTorque * damage.motorEfficiency;

    // Suspension: reduce max travel
    float effectiveTravel = maxSuspensionTravel * damage.suspensionTravel;

    // Wheel grip: per-wheel multiplier
    for (int i = 0; i < 4; i++)
    {
        wheels[i].gripMultiplier = damage.wheelGripMultiplier;
    }
}
```

---

## Damage Zones

Divide the vehicle into 6 zones, each with independent health:

| Zone | Index | Force Threshold (N) | Typical Damage |
|------|-------|---------------------|----------------|
| Front | 0 | 15 | Steering bias, bumper loss |
| Rear | 1 | 15 | Motor efficiency reduction |
| Left | 2 | 12 | Left wheel grip loss, body shell crack |
| Right | 3 | 12 | Right wheel grip loss, body shell crack |
| Top | 4 | 20 | Body shell damage (visual only usually) |
| Underside | 5 | 8 | Suspension damage, chassis flex |

```csharp
public class DamageZoneSystem : MonoBehaviour
{
    private static readonly int ZoneCount = 6;
    private float[] zoneHealth = new float[ZoneCount]; // 1 = pristine, 0 = destroyed
    private float[] zoneThresholds = { 15f, 15f, 12f, 12f, 20f, 8f };

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            int zone = ClassifyContactZone(contact.point);
            float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;

            if (impactForce > zoneThresholds[zone])
            {
                float damage = (impactForce - zoneThresholds[zone]) * damageScale;
                zoneHealth[zone] = Mathf.Max(0f, zoneHealth[zone] - damage);
                OnZoneDamaged?.Invoke(zone, zoneHealth[zone]);
            }
        }
    }

    private int ClassifyContactZone(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);

        if (local.y > 0.03f) return 4;       // Top
        if (local.y < -0.01f) return 5;       // Underside
        if (local.z > 0.05f) return 0;        // Front
        if (local.z < -0.05f) return 1;       // Rear
        if (local.x > 0f) return 3;           // Right
        return 2;                              // Left
    }
}
```

---

## Repair System

### Two Repair Modes

| Mode | When | Time | Cost | Effect |
|------|------|------|------|--------|
| **Quick Repair** | Mid-race pit stop | 5-15 seconds (race time) | Free (time penalty) | Restores 30-50% of worst zone |
| **Full Repair** | Between races | Instant (menu) | Currency cost per zone | Restores to 100% |

```csharp
public class RepairService
{
    public int CalculateRepairCost(float[] zoneHealth, RepairTier tier)
    {
        int totalCost = 0;
        for (int i = 0; i < zoneHealth.Length; i++)
        {
            float damage = 1f - zoneHealth[i];
            int zoneCost = Mathf.CeilToInt(damage * tier.costPerFullRepair);
            totalCost += zoneCost;
        }
        return totalCost;
    }

    public void ApplyQuickRepair(float[] zoneHealth)
    {
        // Find most damaged zone, restore partially
        int worstZone = 0;
        float worstHealth = 1f;
        for (int i = 0; i < zoneHealth.Length; i++)
        {
            if (zoneHealth[i] < worstHealth)
            {
                worstHealth = zoneHealth[i];
                worstZone = i;
            }
        }
        zoneHealth[worstZone] = Mathf.Min(1f, zoneHealth[worstZone] + 0.4f);
    }
}
```

---

## Tire Wear

### Non-Linear Grip Curve

Tire grip degrades non-linearly -- fast initial drop, then gradual:

```csharp
[CreateAssetMenu(menuName = "Racing/Tire Compound")]
public class TireCompoundConfig : ScriptableObject
{
    public string compoundName;
    public TireCompoundType compoundType;
    public AnimationCurve gripVsWear;         // X: wear 0-1, Y: grip multiplier
    public float wearRate;                     // Base wear per unit of slip
    public float optimalTempMin;
    public float optimalTempMax;
    public Color tireColor;
}

public enum TireCompoundType
{
    Soft,       // High grip, fast wear (15-20 min race life)
    Medium,     // Balanced (25-35 min race life)
    Hard        // Low grip, slow wear (45-60 min race life)
}
```

### Wear Calculation

```csharp
void UpdateTireWear(float slipRatio, float slipAngle, float load, float deltaTime)
{
    float slipMagnitude = Mathf.Sqrt(slipRatio * slipRatio + slipAngle * slipAngle);
    float loadFactor = load / nominalLoad; // More load = more wear

    float wearIncrement = slipMagnitude * loadFactor * compound.wearRate * deltaTime;
    tireWearNormalized = Mathf.Clamp01(tireWearNormalized + wearIncrement);

    currentGripMultiplier = compound.gripVsWear.Evaluate(tireWearNormalized);
}
```

### Visual Tire Wear

```csharp
// Shader property: _TireWear 0 (new, pronounced tread) to 1 (bald, smooth)
tireRenderer.material.SetFloat("_TireWear", tireWearNormalized);
```

---

## Battery Simulation

### LiPo Discharge Curve

LiPo batteries have a flat voltage curve with a sudden drop at the end:

```csharp
public class BatterySimulation
{
    [SerializeField] private AnimationCurve dischargeCurve; // Flat middle, steep end
    [SerializeField] private float nominalVoltage = 7.4f;   // 2S LiPo
    [SerializeField] private float capacityMah = 5000f;
    [SerializeField] private float lvcVoltage = 6.4f;       // Low Voltage Cutoff

    private float chargeRemaining; // 0-1
    private float currentVoltage;

    public float VoltageMultiplier => currentVoltage / nominalVoltage;

    void UpdateBattery(float currentDraw, float deltaTime)
    {
        // Discharge
        float mahUsed = currentDraw * 1000f * deltaTime / 3600f;
        chargeRemaining -= mahUsed / capacityMah;
        chargeRemaining = Mathf.Max(0f, chargeRemaining);

        // Voltage from discharge curve
        currentVoltage = dischargeCurve.Evaluate(chargeRemaining) * nominalVoltage;

        // Voltage sag under load
        float sagFactor = currentDraw * internalResistance;
        currentVoltage -= sagFactor;

        // Low Voltage Cutoff
        if (currentVoltage <= lvcVoltage)
        {
            OnLowVoltageCutoff?.Invoke();
            // Reduce motor power dramatically
        }
    }

    // Long-term degradation (career mode)
    public void ApplyCycleDegradation(int chargeCount)
    {
        // After many charges, max capacity drops
        float degradation = Mathf.Lerp(1f, 0.7f, chargeCount / 500f);
        effectiveCapacity = capacityMah * degradation;
    }
}
```

---

## Motor Wear

```csharp
public class MotorSimulation
{
    [SerializeField] private float baseEfficiency = 0.85f;
    [SerializeField] private float heatGenerationRate = 0.1f;
    [SerializeField] private float coolingRate = 0.05f;
    [SerializeField] private float maxTemp = 120f;

    private float currentTemp;
    private float wearLevel; // 0 = new, 1 = needs replacement

    public float EfficiencyMultiplier
    {
        get
        {
            float tempPenalty = currentTemp > 80f
                ? Mathf.Lerp(1f, 0.7f, (currentTemp - 80f) / (maxTemp - 80f))
                : 1f;
            float wearPenalty = Mathf.Lerp(1f, 0.6f, wearLevel);
            return baseEfficiency * tempPenalty * wearPenalty;
        }
    }

    void UpdateMotor(float throttle, float rpm, float ambientTemp, float deltaTime)
    {
        // Heat from use
        currentTemp += throttle * rpm / maxRpm * heatGenerationRate * deltaTime;
        // Cooling
        currentTemp -= (currentTemp - ambientTemp) * coolingRate * deltaTime;
        currentTemp = Mathf.Max(ambientTemp, currentTemp);

        // Wear accumulates with use
        wearLevel += throttle * deltaTime * wearRate;
        wearLevel = Mathf.Clamp01(wearLevel);
    }
}
```

### Motor Upgrade Tiers

| Turn Rating | Power Class | Typical Use |
|-------------|-------------|-------------|
| 21.5T | Stock / Spec | Club racing, beginner |
| 17.5T | Modified entry | Regional competition |
| 13.5T | Modified mid | National level |
| 10.5T | Modified hot | Pro class |

Higher turn count = less power but more efficient and cooler running. Lower turn count = more power but more heat and wear.

---

## Career Mode

### Progression Tiers

| Tier | Name | Entry Requirement | Event Types | Reward Scale |
|------|------|------------------|-------------|-------------|
| 1 | Local Club | None (starting tier) | Practice, Club Race, Time Trial | 1x |
| 2 | Regional | 500 XP + 3 club wins | Regional Series, Endurance | 2x |
| 3 | National | 2000 XP + Regional champion | National Championship, Invitational | 4x |
| 4 | Pro | 5000 XP + National top 3 | Pro Series, World Championship | 8x |

### Event Types

```csharp
public enum EventType
{
    Practice,           // No rewards, free testing
    TimeAttack,         // Solo, beat target times
    ClubRace,           // 6-8 cars, short format (3 rounds + main)
    RegionalSeries,     // Multi-round championship
    Endurance,          // Long race, tire/battery management critical
    NationalChampionship, // Season-long points
    Invitational,       // Special events with unique rules
    ProSeries           // Top tier, all modifications allowed
}
```

---

## Vehicle Upgrades

### Template vs State Pattern

**Key architectural pattern:** Separate read-only templates (ScriptableObjects) from mutable runtime state (plain C# classes serialized to JSON). String IDs link them.

```csharp
// Template: ScriptableObject, read-only, lives in Assets/
[CreateAssetMenu(menuName = "Racing/Upgrade Template")]
public class UpgradeTemplate : ScriptableObject
{
    public string upgradeId;            // "motor_17_5t", "tire_soft_compound"
    public string displayName;
    public string description;
    public UpgradeCategory category;
    public int tier;                    // 1-4, higher = better
    public string[] prerequisites;     // upgradeIds that must be owned first
    public int cost;
    public Sprite icon;

    [Header("Stat Modifiers")]
    public float motorEfficiencyBonus;
    public float gripBonus;
    public float weightDelta;
    public float topSpeedBonus;
}

// State: plain C# class, mutable, serialized to JSON save file
[System.Serializable]
public class VehicleState
{
    public string vehicleId;
    public string templateId;               // Links to VehicleTemplate SO
    public List<string> installedUpgradeIds; // Links to UpgradeTemplate SOs
    public float[] damageZoneHealth;
    public float tireWear;
    public float batteryHealth;
    public int totalRaces;
}
```

### Linear Tiers with Prerequisites

```csharp
public class UpgradeService
{
    public bool CanPurchase(UpgradeTemplate upgrade, VehicleState vehicle, int playerCurrency)
    {
        // Check currency
        if (playerCurrency < upgrade.cost) return false;

        // Check prerequisites
        foreach (string prereqId in upgrade.prerequisites)
        {
            if (!vehicle.installedUpgradeIds.Contains(prereqId)) return false;
        }

        // Check not already installed
        if (vehicle.installedUpgradeIds.Contains(upgrade.upgradeId)) return false;

        return true;
    }
}
```

---

## Currency Economy

### Sources (Earning)

| Source | Amount | Frequency |
|--------|--------|-----------|
| Race finish (position-based) | 50-500 | Per race |
| Time trial personal best | 25-100 | Per improvement |
| Championship completion | 500-5000 | Per series |
| Achievement unlock | 100-1000 | One-time |
| Daily challenge | 50-200 | Daily |

### Sinks (Spending)

| Sink | Cost Range | Frequency |
|------|-----------|-----------|
| Full vehicle repair | 50-300 | After damage |
| Tire replacement | 30-100 | Every 3-5 races |
| Battery replacement | 100-200 | Every 20-30 races |
| Motor upgrade | 200-2000 | Progression milestone |
| New vehicle purchase | 1000-10000 | Major milestone |

### Balance Target

A player should be able to afford basic maintenance (repairs + tires) from race winnings at mid-pack finishes. Upgrades require consistently good finishes or championship completions. The economy should never force grinding -- progression should feel natural.

---

## Unlock System

### Trigger Types

```csharp
public enum UnlockTriggerType
{
    Milestone,      // Reach a specific XP or career tier
    Championship,   // Win or place in a championship
    Challenge,      // Complete a specific challenge (e.g., "win without damage")
    Achievement     // Cumulative achievement (e.g., "complete 100 races")
}

[CreateAssetMenu(menuName = "Racing/Unlock Condition")]
public class UnlockCondition : ScriptableObject
{
    public string unlockId;
    public UnlockTriggerType triggerType;
    public string targetId;             // Championship ID, achievement ID, etc.
    public int requiredValue;           // XP amount, placement, count, etc.
    public string[] rewardIds;          // What gets unlocked (vehicle IDs, track IDs, upgrade IDs)
}
```

---

## Tuning System: Progressive Disclosure

Three tiers of tuning complexity, progressively revealed as the player advances:

### Tier 1: Beginner (Presets)

```csharp
public enum TuningPreset
{
    Balanced,       // Default, works everywhere
    Grip,           // More front downforce, softer springs
    Speed,          // Lower ride height, stiffer springs
    OffRoad         // Higher ride height, softer damping
}
```

Player picks a preset. No individual parameters exposed.

### Tier 2: Intermediate (Sliders)

Unlock at Regional tier. Expose grouped sliders:

| Group | Parameters |
|-------|-----------|
| Suspension | Front stiffness, Rear stiffness, Ride height |
| Drivetrain | Gear ratio (single speed), Differential lock % |
| Aero | Front wing angle, Rear wing angle |
| Tires | Compound selection, Camber (front/rear) |

Each slider has a tooltip and visual indicator showing effect on handling balance.

### Tier 3: Expert (Full Parameters)

Unlock at National tier. All individual parameters exposed:

```csharp
[System.Serializable]
public class FullTuningState
{
    [Header("Suspension")]
    public float frontSpringRate;
    public float rearSpringRate;
    public float frontDamperRebound;
    public float frontDamperCompression;
    public float rearDamperRebound;
    public float rearDamperCompression;
    public float frontRideHeight;
    public float rearRideHeight;
    public float frontAntiRollBar;
    public float rearAntiRollBar;

    [Header("Geometry")]
    public float frontCamber;
    public float rearCamber;
    public float frontToe;
    public float rearToe;
    public float casterAngle;
    public float ackermanPercentage;

    [Header("Drivetrain")]
    public float finalDriveRatio;
    public float frontDiffLock;
    public float rearDiffLock;
    public float centerDiffBias;

    [Header("Aero")]
    public float frontWingAngle;
    public float rearWingAngle;

    [Header("Tires")]
    public TireCompoundType compound;
    public float tirePressure;
}
```

---

## Implementation Priority

### Phase 1: Core (MVP)

| Feature | Effort | Impact |
|---------|--------|--------|
| 6-zone damage with health tracking | Medium | High -- core collision consequence |
| Shader-based damage visuals (`_DamageAmount`) | Low | High -- immediate feedback |
| Physics damage modifiers (steering bias, grip loss) | Medium | High -- affects gameplay |
| Basic repair between races | Low | Medium -- minimum economy |
| Tire wear (single compound, linear) | Low | Medium -- race strategy |
| Battery discharge (basic curve) | Low | Medium -- race pacing |

### Phase 2: Depth

| Feature | Effort | Impact |
|---------|--------|--------|
| 3 tire compounds with non-linear wear curves | Medium | High -- strategic depth |
| LiPo voltage sag and LVC | Low | Medium -- realism |
| Motor heat simulation | Medium | Medium -- management depth |
| Career tiers (Local Club through Regional) | High | High -- player retention |
| Vehicle upgrade system (Tier 1-2) | High | High -- progression loop |
| Currency economy (earn from races, spend on upgrades) | Medium | High -- motivation |
| Tuning presets (Tier 1 beginner) | Low | Medium -- accessibility |

### Phase 3: Stretch

| Feature | Effort | Impact |
|---------|--------|--------|
| Full career (National, Pro tiers) | High | Medium -- endgame content |
| All upgrade tiers with prerequisites | Medium | Medium -- long-term progression |
| Expert tuning (Tier 3) | Medium | Low -- enthusiast feature |
| Motor upgrade tiers (21.5T through 10.5T) | Low | Low -- variety |
| Long-term battery/motor degradation | Low | Low -- realism detail |
| Achievement and challenge unlock system | Medium | Medium -- engagement hooks |
| Decal and LOD body swap damage visuals | High | Low -- visual polish |

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-scriptable-objects`** | Template pattern used for upgrades, tire compounds, and unlock conditions |
| **`unity-save-load`** | Serialization of VehicleState, career progress, and currency to JSON |
| **`unity-physics-3d`** | Physics foundations for collision detection and force-based damage |
| **`unity-physics-tuning`** | Vehicle Rigidbody config that damage modifiers apply to |
