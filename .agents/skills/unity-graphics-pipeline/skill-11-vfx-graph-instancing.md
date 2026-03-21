# 11. VFX Graph Instancing

> Part of the `unity-graphics-pipeline` skill. See [SKILL.md](SKILL.md) for the overview.

## 11. VFX Graph Instancing

Unity 6 supports instanced VFX Graph effects, allowing a single VFX asset to drive multiple emission points (e.g., one per wheel) with automatic GPU batching.

**Setup for Per-Wheel Effects:**

```csharp
using UnityEngine;
using UnityEngine.VFX;

public class WheelVFXInstancing : MonoBehaviour
{
    [SerializeField] private VisualEffect[] wheelEffects; // 4 components, same asset

    public void UpdateWheelEffect(int wheelIndex, float slipMagnitude, Vector3 position)
    {
        var vfx = wheelEffects[wheelIndex];

        vfx.SetFloat("SlipMagnitude", slipMagnitude);
        vfx.SetVector3("EmitPosition", position);

        // VFX Graph handles instancing — these 4 components batch into
        // a single GPU draw call automatically in Unity 6
    }
}
```

**Architecture:**
- Create ONE VFX Graph asset for tire effects (smoke, dirt spray, water splash)
- Add 4 `VisualEffect` components — one per wheel
- All 4 reference the same VFX asset
- Unity 6 auto-batches identical VFX assets into a single indirect draw
- Per-instance properties (position, slip, surface type) are set via `SetFloat`/`SetVector3`

**Exposed Properties in VFX Graph:**
- `SlipMagnitude` (float): drives emission rate and particle size
- `EmitPosition` (Vector3): world-space wheel contact point
- `SurfaceType` (int): selects particle color/texture (0=asphalt, 1=dirt, 2=gravel, 3=grass)
- `VehicleVelocity` (Vector3): inherited velocity for particles

**Performance Benefit:**
- Legacy approach: 4 Particle Systems = 4 draw calls + 4 CPU simulation threads
- VFX Graph instanced: 1 draw call + GPU simulation (shared compute dispatch)
- Savings: ~0.5ms CPU per vehicle at 4 wheels with active effects

---

