# Sub-Emitters

> Part of the `unity-particles-vfx` skill. See [SKILL.md](SKILL.md) for the overview.

## Sub-Emitters

Child particle systems triggered by parent particle events:

| Trigger | When | Example |
|---------|------|---------|
| Birth | Particle is created | Trail behind each spark |
| Collision | Particle hits a surface | Sparks on impact |
| Death | Particle expires | Smoke puff at end of fire particle |
| Trigger | Particle enters trigger volume | Sizzle in water |

Setup: Add Sub-Emitters module, select trigger type, assign a child ParticleSystem.

```
// Firework setup:
Parent (launch particle):
  Sub-Emitters:
    On Death -> ExplosionPS

ExplosionPS:
  Emission: Burst of 50
  Shape: Sphere
  Sub-Emitters:
    On Death -> SparkPS

SparkPS:
  Emission: Burst of 3
  Start Lifetime: 0.3
  Color Over Lifetime: Yellow -> Red -> Transparent
```

## Collision Module

### World Collision

```
Collision:
  Type: World
  Mode: 3D
  Dampen: 0.2          // Speed reduction on bounce (0-1)
  Bounce: 0.5           // Bounciness (0-1)
  Lifetime Loss: 0.1    // Fraction of remaining lifetime lost per bounce
  Min Kill Speed: 0.5   // Particles slower than this are killed
  Collision Quality: High
  Collides With: Default, Ground  // Layer mask
  Send Collision Messages: true   // Enables OnParticleCollision
```

### Receiving Collision Events

```csharp
public class ParticleCollisionHandler : MonoBehaviour
{
    ParticleSystem _ps;
    List<ParticleCollisionEvent> _collisionEvents = new();

    void Awake() => _ps = GetComponent<ParticleSystem>();

    void OnParticleCollision(GameObject other)
    {
        int count = _ps.GetCollisionEvents(other, _collisionEvents);
        for (int i = 0; i < count; i++)
        {
            Vector3 hitPoint = _collisionEvents[i].intersection;
            Vector3 hitNormal = _collisionEvents[i].normal;
            // Spawn decal, play sound, apply damage, etc.
            SpawnImpactDecal(hitPoint, hitNormal);
        }
    }
}
```

