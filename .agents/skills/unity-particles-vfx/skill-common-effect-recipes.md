# Common Effect Recipes

> Part of the `unity-particles-vfx` skill. See [SKILL.md](SKILL.md) for the overview.

## Common Effect Recipes

### Fire

```
Main: Lifetime 0.5-1.0, Speed 1-3, Gravity -0.5 (float up)
Shape: Cone, Angle 15, Radius 0.2
Color Over Lifetime: Yellow (0%) -> Orange (40%) -> Red (70%) -> Black transparent (100%)
Size Over Lifetime: 1.0 -> 1.5 -> 0.0 (grow then shrink)
Noise: Strength 0.5, Frequency 3 (flicker)
Renderer: Billboard, Additive material
```

### Smoke

```
Main: Lifetime 2-4, Speed 0.5-1, Gravity -0.1, Start Size 0.5-1.5
Shape: Cone, Angle 20
Color Over Lifetime: Dark gray 50% alpha -> Light gray 0% alpha
Size Over Lifetime: 0.5 -> 2.0 (expand)
Noise: Strength 1, Frequency 1, Octaves 3 (turbulence)
Rotation Over Lifetime: 30-90 deg/s (tumble)
Renderer: Billboard, Alpha blended material
```

### Dust Trail (Behind Vehicle)

```
Main: Lifetime 1.5-3, Start Size 0.2-0.5, Simulation Space World
Emission: Rate Over Distance 15
Shape: Edge, Radius 0.5
Color Over Lifetime: Tan 80% alpha -> Transparent
Size Over Lifetime: 0.3 -> 2.0
Velocity Over Lifetime: Small upward drift (Y: 0.3)
Noise: Strength 0.3, Frequency 2
```

### Sparks (On Impact)

```
Main: Lifetime 0.3-0.8, Speed 3-8, Gravity 3, Start Size 0.02-0.05
Emission: Burst of 10-30
Shape: Hemisphere, Radius 0.1
Color Over Lifetime: White -> Yellow -> Orange -> Transparent
Size Over Lifetime: 1.0 -> 0.0
Collision: World, Bounce 0.3, Lifetime Loss 0.3
Renderer: Stretched Billboard, Speed Scale 0.2
Trails: Ratio 0.5, Lifetime 0.2, Width Taper
```

### Rain

```
Main: Lifetime 1-2, Speed 15-20, Start Size 0.02-0.04
Emission: Rate Over Time 2000
Shape: Box, Scale (30, 0, 30), position above camera
Color: Light blue-white, 60% alpha
Renderer: Stretched Billboard, Length Scale 2, Speed Scale 0.5
Collision: World (optional), spawn splash sub-emitter on collision
```

### Explosion

```
// Core flash
Main: Lifetime 0.1, Speed 0, Start Size 3
Emission: Burst 1
Color: White -> Orange -> Transparent
Renderer: Billboard, Additive

// Debris
Main: Lifetime 1-2, Speed 5-15, Gravity 5, Start Size 0.1-0.3
Emission: Burst 20-40
Shape: Sphere, Radius 0.5
Collision: World, Bounce 0.4
Renderer: Mesh (rock/debris mesh), Random rotation

// Smoke cloud (delayed)
Main: Start Delay 0.1, Lifetime 2-4, Speed 1-2, Start Size 1-3
Emission: Burst 10-15
Shape: Sphere, Radius 1
Color Over Lifetime: Dark gray -> Transparent
Size Over Lifetime: Grow
```

## Optimization

### Key Settings

| Setting | Impact | Recommendation |
|---------|--------|----------------|
| Max Particles | Memory + overdraw | Set to the minimum you actually need |
| Simulation Space | CPU cost | World for persistent, Local for attached |
| Collision Quality | CPU (major) | Use Medium or Low for non-critical effects |
| Culling | GPU | Enable on Renderer: Automatic or custom bounds |
| GPU Instancing | Draw calls | Enable on Renderer for mesh particles |
| Ring Buffer | Memory | Enable for effects where oldest particles should be recycled |

### LOD for Particles

```csharp
// Reduce particle count based on distance
public class ParticleLOD : MonoBehaviour
{
    [SerializeField] ParticleSystem _ps;
    [SerializeField] float _fullDetailDistance = 20f;
    [SerializeField] float _cullDistance = 80f;
    [SerializeField] float _maxEmission = 100f;

    Transform _camera;
    ParticleSystem.EmissionModule _emission;

    void Start()
    {
        _camera = Camera.main.transform;
        _emission = _ps.emission;
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, _camera.position);
        if (dist > _cullDistance)
        {
            if (_ps.isPlaying) _ps.Stop();
            return;
        }

        if (!_ps.isPlaying) _ps.Play();
        float t = Mathf.InverseLerp(_fullDetailDistance, _cullDistance, dist);
        _emission.rateOverTime = Mathf.Lerp(_maxEmission, _maxEmission * 0.1f, t);
    }
}
```

### Overdraw Reduction

- Use **smaller particles** with higher emission rates rather than fewer large particles
- Use **alpha cutoff** materials instead of alpha blend where possible
- Limit **overlapping transparent particles** in the same screen area
- Use the **Scene View Overdraw mode** to identify hotspots
