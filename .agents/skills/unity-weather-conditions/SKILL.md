---
name: unity-weather-conditions
description: Unity Weather & Track Conditions for RC Racing
---

# Unity Weather & Track Conditions for RC Racing

Use this skill when implementing dynamic weather systems and track condition modeling for an RC racing simulator. Covers weather state machines, global wetness, rain/dust/mud effects, wind physics, time of day, temperature models, track evolution, and wet surface shaders.

## When to Use

- Implementing a weather state machine (clear, rain, drying transitions)
- Adding wet-track physics (grip reduction, aquaplaning, mud drag)
- Creating rain, dust, or mud particle effects
- Integrating wind forces with vehicle physics and particles
- Building time-of-day or temperature systems that affect racing
- Modeling track evolution (rubber buildup, marbles, oil spills)

## When NOT to Use

- General particle system setup -- use `unity-particles-vfx`
- Shader authoring fundamentals -- use `unity-shaders` or `unity-3d-materials`
- Physics engine configuration (solver, timestep, layers) -- use `unity-physics-tuning`
- Networking weather state sync -- use `unity-networking`

---

## Weather State Machine

Model weather as a finite state machine with ScriptableObject configs per state.

### States

```
Clear <-> Cloudy <-> LightRain <-> HeavyRain -> Drying -> Clear
```

Each state defines visual and gameplay parameters:

```csharp
[CreateAssetMenu(menuName = "Racing/Weather State")]
public class WeatherStateConfig : ScriptableObject
{
    [Header("Identity")]
    public string stateName;
    public WeatherType weatherType;

    [Header("Wetness")]
    [Range(0f, 1f)] public float targetWetness;
    public float wetnessTransitionRate;     // Units per second toward target

    [Header("Rain")]
    public int rainParticleCount;           // 0 for Clear/Cloudy/Drying
    public float rainIntensity;

    [Header("Wind")]
    public float windStrengthMin;
    public float windStrengthMax;

    [Header("Lighting")]
    public float sunIntensityMultiplier;
    public Color ambientColorTint;

    [Header("Temperature")]
    public float ambientTempCelsius;

    [Header("Allowed Transitions")]
    public WeatherStateConfig[] validTransitions;
}
```

### State Machine Controller

```csharp
public class WeatherController : MonoBehaviour
{
    [SerializeField] private WeatherStateConfig initialState;
    [SerializeField] private float minStateDuration = 60f;
    [SerializeField] private float maxStateDuration = 300f;

    private WeatherStateConfig currentState;
    private float currentWetness;
    private float stateTimer;

    void Update()
    {
        // Lerp wetness toward current state's target
        currentWetness = Mathf.MoveTowards(currentWetness,
            currentState.targetWetness,
            currentState.wetnessTransitionRate * Time.deltaTime);

        // Push to global shader property
        Shader.SetGlobalFloat("_GlobalWetness", currentWetness);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            TransitionToNextState();
        }
    }
}
```

---

## Global Wetness as Single Source of Truth

A single `_GlobalWetness` float (0 = bone dry, 1 = fully saturated) drives all weather-dependent systems.

```csharp
// Set globally -- all shaders and scripts can read it
Shader.SetGlobalFloat("_GlobalWetness", currentWetness);
```

### Consumers

| System | How It Uses Wetness |
|--------|-------------------|
| Tire physics | `grip *= (1f - wetness * surfaceConfig.wetGripReduction)` |
| Shaders | Increase smoothness, darken albedo, enable ripple normals |
| Dust particles | `emission = speed * (1f - wetness)` |
| Rain particles | `emission = wetness * maxRainParticles` |
| Audio | Blend in rain ambience, tire splash sounds |
| AI | Reduce target speed, increase following distance |

---

## Rain System

### Camera-Relative Particle Emitter

Attach the rain emitter to the camera so rain is always visible without covering the entire world.

```csharp
public class RainSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem rainParticles;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 emitterOffset = new Vector3(0f, 15f, 5f);

    void LateUpdate()
    {
        // Follow camera position but not rotation
        transform.position = cameraTransform.position + emitterOffset;
    }
}
```

### Particle Settings

| Parameter | Value | Notes |
|-----------|-------|-------|
| Max Particles | 500-1500 | Scale with quality settings |
| Emission Rate | 200-1000/sec | Driven by rain intensity |
| Start Speed | 8-15 | Vertical fall speed |
| Start Size | 0.01-0.03 | Thin streaks at 1/10 scale |
| Shape | Box (10m x 0.1m x 10m) | Wide area above camera |
| Renderer | Stretched Billboard | Length = 0.05-0.1 for streak effect |
| Collision | World, track surface | For splash sub-emitter on impact |

### WindZone Integration

Use Unity's `WindZone` to affect rain direction and particle drift:

```csharp
// WindZone affects particles automatically if Particle System has External Forces enabled
// Also apply to vehicle physics:
void FixedUpdate()
{
    Vector3 windForce = windZone.transform.forward * windZone.windMain;
    vehicleRb.AddForce(windForce * windDragCoefficient, ForceMode.Force);
}
```

---

## Wet Track Physics

### Grip Reduction

```csharp
public float CalculateWetGrip(float baseGrip, float wetness, SurfaceConfig surface)
{
    // Non-linear: first bit of water has biggest effect
    float wetFactor = 1f - (surface.wetGripReduction * Mathf.Sqrt(wetness));
    return baseGrip * wetFactor;
}
```

### Aquaplaning

At high speed on standing water, tires lose contact with the surface:

```csharp
public float CalculateAquaplaneRisk(float speed, float wetness, float tireWidth)
{
    // Risk increases with speed and wetness, decreases with narrow tires
    float speedFactor = Mathf.InverseLerp(5f, 15f, speed); // RC scale: 5-15 m/s
    float risk = speedFactor * wetness * (tireWidth / 0.03f);
    return Mathf.Clamp01(risk);
}

// Apply: reduce lateral grip proportionally
float lateralGrip = baseLateralGrip * (1f - aquaplaneRisk * 0.8f);
```

### WheelFrictionCurve Modification (if using WheelCollider)

```csharp
WheelFrictionCurve fwd = wheelCollider.forwardFriction;
fwd.stiffness *= (1f - wetness * 0.4f);
wheelCollider.forwardFriction = fwd;

WheelFrictionCurve side = wheelCollider.sidewaysFriction;
side.stiffness *= (1f - wetness * 0.5f);
wheelCollider.sidewaysFriction = side;
```

---

## Mud Dynamics

### Physics Effects

```csharp
public struct MudEffect
{
    public float gripMultiplier;    // 0.3-0.6 of base grip
    public float dragForce;         // Additional resistance (N)
    public float dirtAccumulation;  // Rate of visual dirt buildup
}

void ApplyMudPhysics(Rigidbody rb, float speed, MudEffect mud)
{
    // Drag increases with speed (viscous drag model)
    Vector3 dragForce = -rb.linearVelocity.normalized * mud.dragForce * speed;
    rb.AddForce(dragForce, ForceMode.Force);
}
```

### Visual Dirt Accumulation

Track dirt buildup per-vehicle and push to the shader:

```csharp
private float dirtAmount;

void Update()
{
    if (isOnMud && speed > 0.5f)
    {
        dirtAmount = Mathf.MoveTowards(dirtAmount, 1f, dirtAccumulationRate * Time.deltaTime);
    }

    // Slowly clean in rain
    float cleanRate = Shader.GetGlobalFloat("_GlobalWetness") * rainCleanRate;
    dirtAmount = Mathf.MoveTowards(dirtAmount, 0f, cleanRate * Time.deltaTime);

    vehicleRenderer.material.SetFloat("_DirtAmount", dirtAmount);
}
```

---

## Dust System

Dust emission inversely proportional to moisture:

```csharp
void UpdateDustEmission(float speed, float wetness, SurfaceType surface)
{
    if (surface != SurfaceType.Dirt && surface != SurfaceType.Gravel)
    {
        dustEmission.rateOverTime = 0f;
        return;
    }

    float baseRate = speed * dustEmissionPerSpeed;
    float moistureReduction = 1f - Mathf.Clamp01(wetness * 1.5f); // Fully suppressed at 0.67 wetness
    dustEmission.rateOverTime = baseRate * moistureReduction;
}
```

### Roost Visibility

Dust reduces visibility for following cars. Use a trigger collider behind each car:

```csharp
// Roost zone: box trigger behind vehicle
// Following cars inside the zone get a visibility penalty
void OnTriggerStay(Collider other)
{
    if (other.TryGetComponent<AIDriver>(out var ai))
    {
        ai.SetVisibilityPenalty(dustDensity);
    }
}
```

---

## Wind Effects

### Vehicle Physics

```csharp
void FixedUpdate()
{
    Vector3 windDirection = windZone.transform.forward;
    float windSpeed = windZone.windMain + windZone.windTurbulence * Mathf.PerlinNoise(Time.time, 0f);

    // Cross-wind pushes vehicle laterally
    Vector3 vehicleRight = transform.right;
    float crossComponent = Vector3.Dot(windDirection, vehicleRight);
    Vector3 crossForce = vehicleRight * crossComponent * windSpeed * windDragArea;

    // Head/tail wind affects top speed
    Vector3 vehicleForward = transform.forward;
    float headComponent = Vector3.Dot(windDirection, vehicleForward);
    Vector3 headForce = vehicleForward * headComponent * windSpeed * windDragArea;

    rb.AddForce(crossForce + headForce, ForceMode.Force);
}
```

### Particle Integration

Enable **External Forces** on particle systems and set the `externalForces.multiplier`:

```csharp
var externalForces = particleSystem.externalForces;
externalForces.enabled = true;
externalForces.multiplier = 1.0f; // WindZone affects particles automatically
```

---

## Time of Day

### Directional Light Rotation

```csharp
public class TimeOfDayController : MonoBehaviour
{
    [SerializeField] private Light sunLight;
    [SerializeField] private Gradient sunColorOverDay;
    [SerializeField] private AnimationCurve sunIntensityOverDay;

    private float timeOfDay; // 0-1 (0 = midnight, 0.5 = noon)

    void Update()
    {
        timeOfDay += Time.deltaTime / dayLengthSeconds;
        timeOfDay %= 1f;

        // Rotate sun
        float sunAngle = (timeOfDay - 0.25f) * 360f; // Sunrise at 0.25
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Color temperature
        sunLight.color = sunColorOverDay.Evaluate(timeOfDay);
        sunLight.intensity = sunIntensityOverDay.Evaluate(timeOfDay);

        // Update GI
        DynamicGI.UpdateEnvironment();
    }
}
```

---

## Temperature Model

### Tire Temperature and Grip

```csharp
[CreateAssetMenu(menuName = "Racing/Tire Compound")]
public class TireCompoundConfig : ScriptableObject
{
    public string compoundName;
    public AnimationCurve gripVsTemperature; // X: temp (C), Y: grip multiplier 0-1
    public float optimalTempMin;             // e.g., 30C
    public float optimalTempMax;             // e.g., 50C
    public float heatGenerationRate;         // From slip
    public float coolingRate;                // Ambient cooling
}

void UpdateTireTemp(float slipMagnitude, float ambientTemp, float deltaTime)
{
    float heatGain = slipMagnitude * compound.heatGenerationRate * deltaTime;
    float heatLoss = (tireTemp - ambientTemp) * compound.coolingRate * deltaTime;
    tireTemp += heatGain - heatLoss;

    float gripMultiplier = compound.gripVsTemperature.Evaluate(tireTemp);
}
```

### Battery Performance in Cold

```csharp
// LiPo batteries lose capacity and voltage in cold
float batteryEfficiency = Mathf.Lerp(0.7f, 1.0f,
    Mathf.InverseLerp(-5f, 25f, ambientTemp));
```

---

## Track Evolution

### Rubber Buildup (Racing Line)

Use a `RenderTexture` to paint grip zones where cars drive:

```csharp
// Each frame, paint a small circle at each tire contact point onto a RenderTexture
// This texture is sampled by the tire model as a grip bonus
void PaintRubber(Vector2 uvPosition, float amount)
{
    rubberMaterial.SetVector("_PaintPosition", new Vector4(uvPosition.x, uvPosition.y, 0f, 0f));
    rubberMaterial.SetFloat("_PaintAmount", amount);
    Graphics.Blit(rubberTexture, rubberTextureTemp, rubberMaterial);
    // Swap textures
    (rubberTexture, rubberTextureTemp) = (rubberTextureTemp, rubberTexture);
}
```

### Marbles Off-Line

Tire wear particles accumulate off the racing line, reducing grip:

```csharp
// Inverse of rubber buildup -- areas with low rubber have marbles
float marbleGripPenalty = (1f - rubberAmount) * marbleFactor;
float surfaceGrip = baseGrip + rubberBonus - marbleGripPenalty;
```

### Oil Spills

Place decal projectors with a slippery trigger zone:

```csharp
// Oil spill: decal for visuals + trigger collider for physics
void OnTriggerStay(Collider other)
{
    if (other.TryGetComponent<VehiclePhysics>(out var vehicle))
    {
        vehicle.ApplyGripModifier(oilGripMultiplier); // e.g., 0.2
    }
}
```

---

## Wet Material Shaders

### Shader Properties

```hlsl
// Add to existing surface shader or Shader Graph
float _GlobalWetness;   // Set via Shader.SetGlobalFloat
float _DirtAmount;      // Per-material

// Wetness effect:
// 1. Increase smoothness (wet surfaces are shinier)
float wetSmoothness = lerp(baseSmoothness, 0.95, _GlobalWetness);

// 2. Darken albedo (wet surfaces absorb more light)
float3 wetAlbedo = baseAlbedo * lerp(1.0, 0.6, _GlobalWetness);

// 3. Ripple normal map (animated water surface)
float2 rippleUV = worldPos.xz * 2.0 + _Time.y * float2(0.1, 0.15);
float3 rippleNormal = UnpackNormal(tex2D(_RippleNormalMap, rippleUV));
float3 finalNormal = lerp(baseNormal, rippleNormal, _GlobalWetness * 0.5);
```

---

## Integration with SurfaceConfig

Extend existing surface configuration with wetness data:

```csharp
[CreateAssetMenu(menuName = "Racing/Surface Config")]
public class SurfaceConfig : ScriptableObject
{
    [Header("Base Physics")]
    public float baseGrip;
    public float rollingResistance;

    [Header("Weather Modifiers")]
    [Range(0f, 1f)] public float wetGripReduction;     // How much grip drops when wet
    [Range(0f, 1f)] public float mudDragCoefficient;    // Drag in wet conditions
    public float dustEmissionRate;                       // Dust per unit speed
    public float waterAbsorptionRate;                    // How fast surface dries

    public float GetEffectiveGrip(float wetness)
    {
        return baseGrip * (1f - wetGripReduction * Mathf.Sqrt(wetness));
    }
}
```

---

## Performance Considerations

### Particle LOD by Distance

```csharp
void UpdateParticleLOD(float distanceToCamera)
{
    var emission = particleSystem.emission;
    if (distanceToCamera > 50f)
    {
        emission.rateOverTimeMultiplier = 0f; // Too far, disable
    }
    else if (distanceToCamera > 25f)
    {
        emission.rateOverTimeMultiplier = 0.25f; // Reduced
    }
    else
    {
        emission.rateOverTimeMultiplier = 1f; // Full quality
    }
}
```

### Shader LOD Tiers

```csharp
// Quality settings drive shader complexity
if (QualitySettings.GetQualityLevel() < 2) // Low
{
    Shader.DisableKeyword("_RIPPLE_NORMALS");
    Shader.DisableKeyword("_TRACK_EVOLUTION");
}
```

### Dynamic Quality Adjustment

```csharp
// If frame time exceeds budget, reduce weather effects
if (Time.unscaledDeltaTime > targetFrameTime * 1.2f)
{
    ReduceParticleCount(0.5f);
    DisableRippleNormals();
}
```

---

## Implementation Priority

| Priority | Feature | Effort | Impact |
|----------|---------|--------|--------|
| **P1** | Global wetness + grip reduction | Low | High -- core gameplay mechanic |
| **P1** | Weather state machine (Clear/Rain) | Medium | High -- drives all weather systems |
| **P1** | Rain particle system | Low | High -- essential visual feedback |
| **P2** | Wet surface shaders | Medium | Medium -- visual polish |
| **P2** | Dust system with moisture suppression | Low | Medium -- immersion |
| **P2** | Wind physics on vehicle | Low | Medium -- adds depth to racing |
| **P3** | Time of day lighting | Medium | Medium -- atmosphere |
| **P3** | Tire temperature model | Medium | Medium -- simulation depth |
| **P3** | Track evolution (rubber/marbles) | High | Medium -- advanced racing mechanic |
| **P4** | Mud dynamics with dirt accumulation | Medium | Low -- specific to off-road tracks |
| **P4** | Battery cold performance | Low | Low -- niche realism |
| **P4** | Dynamic quality adjustment | Medium | Low -- optimization polish |

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-physics-3d`** | General physics foundations that grip/drag calculations build on |
| **`unity-physics-tuning`** | PhysX configuration for the vehicle that weather modifiers apply to |
| **`unity-particles-vfx`** | Particle system fundamentals for rain, dust, and mud effects |
| **`unity-3d-materials`** | Shader and material basics for wet surface rendering |
