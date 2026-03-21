# Rain System

> Part of the `unity-weather-conditions` skill. See [SKILL.md](SKILL.md) for the overview.

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

