# Surface Detection at Runtime

> Part of the `unity-terrain-track-creation` skill. See [SKILL.md](SKILL.md) for the overview.

## Surface Detection at Runtime

### Terrain Splatmap Sampling

```csharp
// Cache alphamaps — do NOT call every frame
// Sample at 0.1-0.2 second intervals
private float[,,] _cachedAlphamaps;
private float _nextSampleTime;

void UpdateSurfaceType(Vector3 worldPos)
{
    if (Time.time < _nextSampleTime) return;
    _nextSampleTime = Time.time + 0.15f;

    TerrainData td = terrain.terrainData;
    Vector3 terrainPos = worldPos - terrain.transform.position;
    int mapX = Mathf.RoundToInt(terrainPos.x / td.size.x * td.alphamapWidth);
    int mapZ = Mathf.RoundToInt(terrainPos.z / td.size.z * td.alphamapHeight);
    _cachedAlphamaps = td.GetAlphamaps(mapX, mapZ, 1, 1);

    // Find dominant layer
    int dominant = 0;
    float maxWeight = 0f;
    for (int i = 0; i < td.alphamapLayers; i++)
    {
        if (_cachedAlphamaps[0, 0, i] > maxWeight)
        {
            maxWeight = _cachedAlphamaps[0, 0, i];
            dominant = i;
        }
    }
    CurrentSurface = (SurfaceType)dominant;
}
```

### Trigger Zone Overlay

For hard-surface sections (bridges, ramps built with ProBuilder), use trigger colliders with a `SurfaceZone` component. These override terrain splatmap detection when the vehicle is inside the trigger.

### Physics Materials Per Surface

| Surface | Static Friction | Dynamic Friction | Friction Combine |
|---------|----------------|-----------------|-----------------|
| Packed dirt | 0.8 | 0.6 | Average |
| Loose gravel | 0.5 | 0.3 | Minimum |
| Grass | 0.6 | 0.4 | Average |
| Wet packed dirt | 0.5 | 0.35 | Average |
| Concrete/tarmac | 1.0 | 0.8 | Average |

> **WARNING:** Unity WheelCollider ignores the PhysicsMaterial on its own collider. Surface friction must be applied via `WheelCollider.sidewaysFriction` and `forwardFriction` stiffness multipliers at runtime based on detected surface type.

---

