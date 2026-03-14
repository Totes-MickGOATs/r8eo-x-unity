# Assets/Scripts/Track/

Track and environment systems including surface zones and course layout.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `SurfaceZone.cs` | `SurfaceZone` | Trigger volume that applies surface grip modifiers to wheels |

## Conventions

- Surface zones use trigger colliders, not raycasts
- Each zone references a SurfaceConfig ScriptableObject
- Wheels detect zones via OnTriggerEnter/Exit (future integration)

## Relevant Skills

- **`unity-physics-3d`** — Trigger-based collider patterns for surface detection
