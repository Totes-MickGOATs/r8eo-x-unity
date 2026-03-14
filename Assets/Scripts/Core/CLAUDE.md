# Assets/Scripts/Core/

Shared types, enums, and data definitions used across all systems.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `SurfaceType.cs` | `SurfaceType` | Central enum of all surface types (Dirt, Gravel, Grass, etc.) |
| `SurfaceConfig.cs` | `SurfaceConfig` | ScriptableObject defining friction properties per surface |

## Conventions

- Core types have NO dependencies on other game assemblies
- Enums and ScriptableObjects only — no MonoBehaviours
- Other assemblies reference Core, never the reverse

## Relevant Skills

- **`unity-scriptable-objects`** — SurfaceConfig is a ScriptableObject
- **`unity-architecture-patterns`** — Central enum patterns and shared type conventions
