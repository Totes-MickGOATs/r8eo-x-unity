# Renderer Module

> Part of the `unity-particles-vfx` skill. See [SKILL.md](SKILL.md) for the overview.

## Renderer Module

### Render Modes

| Mode | Behavior | Use For |
|------|----------|---------|
| Billboard | Always faces camera | Most particles (fire, smoke, sparks) |
| Stretched Billboard | Stretches along velocity | Rain, speed lines, tracers |
| Horizontal Billboard | Flat on XZ plane | Ground decals, puddles |
| Vertical Billboard | Faces camera but stays upright | Grass, small foliage |
| Mesh | 3D mesh per particle | Debris, leaves, shrapnel |
| None | Invisible (trail only) | Used with Trail Renderer |

### Stretched Billboard Settings

```
Renderer:
  Render Mode: Stretched Billboard
  Speed Scale: 0.1        // Stretch based on speed
  Length Scale: 1.0        // Base stretch length
  Camera Scale: 0          // Stretch based on camera distance
```

### Trail Renderer

Enable **Trails** module for particles that leave trails:

```
Trails:
  Mode: Particles
  Ratio: 1.0              // Fraction of particles with trails
  Lifetime: 0.5           // Trail duration
  Minimum Vertex Distance: 0.1
  Width Over Trail: Curve (1 to 0)  // Taper off
  Color Over Trail: Gradient
  Inherit Particle Color: true
```

## Particle System API

```csharp
public class ParticleController : MonoBehaviour
{
    [SerializeField] ParticleSystem _ps;

    void Start()
    {
        // Read/modify main module (struct -- must assign back)
        var main = _ps.main;
        main.startColor = Color.red;
        main.startLifetime = 2f;
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Modify emission
        var emission = _ps.emission;
        emission.rateOverTime = 100f;

        // Add a burst
        emission.SetBurst(0, new ParticleSystem.Burst(0f, 50));
    }

    // Playback control
    public void Play() => _ps.Play();
    public void Stop() => _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    public void StopImmediate() => _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    public void Pause() => _ps.Pause();

    // Manual emission
    public void EmitBurst(int count)
    {
        var emitParams = new ParticleSystem.EmitParams();
        emitParams.position = transform.position;
        emitParams.startColor = Color.yellow;
        emitParams.startSize = 0.5f;
        _ps.Emit(emitParams, count);
    }

    // Read particle count
    void Update()
    {
        int alive = _ps.particleCount;
        bool isPlaying = _ps.isPlaying;
    }
}
```

## VFX Graph (GPU Particles)

Visual effect authoring tool using a node-based graph. Runs on the GPU via compute shaders.

### Package

```json
"com.unity.visualeffectgraph": "17.0.3"
```

Requires URP or HDRP. Does not work with the Built-in Render Pipeline.

### Architecture

VFX Graph uses four sequential contexts:

| Context | Purpose | Runs |
|---------|---------|------|
| **Spawn** | Controls particle creation rate/bursts | Every frame (CPU) |
| **Initialize** | Set initial properties (position, velocity, color, size) | Per particle, at birth (GPU) |
| **Update** | Modify properties each frame (forces, collisions, aging) | Per particle, every frame (GPU) |
| **Output** | Render particles (mesh, quad, strip, point) | Per particle, every frame (GPU) |

### Creating a VFX Graph

1. **Assets > Create > VFX > Visual Effect Graph**
2. Add a **Visual Effect** component to a GameObject
3. Assign the VFX Graph asset
4. Open the graph in the VFX Graph editor window

### Key Nodes

```
Spawn Context:
  Constant Spawn Rate: 1000
  Periodic Burst: Count 200, Delay 0.5

Initialize Context:
  Set Position (Shape: Sphere, Radius: 2)
  Set Velocity (Direction: Random, Speed: 5)
  Set Lifetime (Random: 1.0 - 2.0)
  Set Size (Random: 0.05 - 0.2)
  Set Color (Gradient Over Lifetime)

Update Context:
  Gravity (Force: -9.81)
  Turbulence (Intensity: 3, Frequency: 2)
  Collision (Plane/Sphere/Mesh)
  Age Particles
  Reap Particles (removes dead)

Output Context:
  Output Particle Quad (Billboard)
  Set Size Over Lifetime
  Set Color Over Lifetime
  Main Texture: particle_soft
```

### Exposed Properties

Expose graph properties to control from code:

```csharp
VisualEffect _vfx;

void Start()
{
    _vfx = GetComponent<VisualEffect>();
    _vfx.SetFloat("SpawnRate", 500f);
    _vfx.SetVector3("EmitterPosition", transform.position);
    _vfx.SetGradient("ColorGradient", myGradient);
    _vfx.SetTexture("MainTexture", myTexture);
}

// Events (trigger spawn bursts)
_vfx.SendEvent("OnHit");
_vfx.SendEvent("OnExplosion");
```

