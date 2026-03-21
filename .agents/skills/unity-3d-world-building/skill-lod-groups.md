# LOD Groups

> Part of the `unity-3d-world-building` skill. See [SKILL.md](SKILL.md) for the overview.

## LOD Groups

Level of Detail system for rendering simpler meshes at distance.

### Setup

```
1. Add LOD Group component to parent GameObject
2. Child objects assigned to LOD levels:
   - LOD 0: full detail (closest to camera)
   - LOD 1: reduced detail
   - LOD 2: low detail
   - Culled: invisible (beyond max distance)

3. Set transition percentages in LOD Group Inspector
   - Percentage = screen height occupied by object
   - LOD 0: 60%+, LOD 1: 30-60%, LOD 2: 10-30%, Culled: <10%
```

### Cross-Fade

```
Fade Mode:
  - None: hard switch (pop)
  - Cross Fade: smooth blend between LODs
  - Speed Tree: specialized for SpeedTree assets

Fade Transition Width: 0.0-1.0
  - How much overlap during transition
  - Higher = smoother but more overdraw
```

### LOD Generation

```
Manual: create 3+ mesh variants in DCC tool (Blender decimate, Simplygon)
Automatic:
  - Unity LOD Generator (Package Manager)
  - Simplygon (plugin)
  - Mantis LOD Editor (Asset Store)
```

### LOD for Imposters

```
At extreme distance, replace 3D mesh with a billboard:
  - Pre-render the object from multiple angles
  - Display as a textured quad facing the camera
  - Significant performance gain for trees, distant buildings
  - Amplify Impostors (Asset Store) or custom solution
```

## Occlusion Culling

Prevents rendering objects hidden behind other objects.

### Baking

```
1. Mark occluders: Static > Occluder Static (walls, floors, large objects)
2. Mark occludees: Static > Occludee Static (objects that can be hidden)
3. Window > Rendering > Occlusion Culling
4. Set cell size and smallest occluder
5. Bake

Cell Size: spatial subdivision granularity
  - Smaller = more accurate, larger bake data
  - Start at 4-8 for indoor, 16-32 for outdoor
```

### Occlusion Areas

```
Manual override zones:
  - Add Occlusion Area component to empty GameObject
  - Size the box to cover an area
  - Mark "Is View Volume" for areas the camera can be in
  - Increases accuracy within that volume
```

### Occlusion Portals

```
For doorways and openings between rooms:
  - Occlusion Portal component
  - When portal is closed (Open = false), everything behind is culled
  - Dynamically open/close in code
```

```csharp
OcclusionPortal portal = GetComponent<OcclusionPortal>();
portal.open = doorIsOpen;
```

### Visualization

```
In Scene view: select Occlusion Culling window > Visualization tab
  - Move camera around to see what's culled (grayed out)
  - Green = visible, dark = culled
```

## Scene Organization

### Hierarchy Structure

```
--- Level_Desert ---
[Environment]
    Terrain
    Skybox
    Lighting
        DirectionalLight_Sun
        LightProbeGroup
        ReflectionProbe_Main
[Geometry]
    Buildings/
        Building_A (prefab)
        Building_B (prefab)
    Props/
        Barrel_01
        Crate_01
    Foliage/
        Tree_Cluster_01
[Gameplay]
    SpawnPoints/
        SpawnPoint_Player
        SpawnPoint_Enemy_01
    Triggers/
        Trigger_BossRoom
    Checkpoints/
        Checkpoint_01
[Audio]
    AmbientZone_Wind
    MusicTrigger_Combat
[Navigation]
    NavMesh Surface
    NavMesh Obstacles
[UI]
    Canvas_HUD
    Canvas_Pause
```

### Naming Conventions

```
PascalCase for GameObjects: Building_Warehouse_01
  - Type_Variant_Number pattern
  - Prefix categories: Trigger_, Spawn_, Audio_, Light_
  - Number suffix for duplicates: _01, _02

Folders in Hierarchy use empty GameObjects:
  - [Brackets] for organizational folders (no components)
  - Reset transform to origin
```

## Spawn Points and Markers

### Using Empty Transforms

```csharp
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private SpawnType type;
    [SerializeField] private int teamIndex;

    public enum SpawnType { Player, Enemy, Item, Vehicle }

    // Visual indicator in Scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = type switch
        {
            SpawnType.Player => Color.green,
            SpawnType.Enemy => Color.red,
            SpawnType.Item => Color.yellow,
            SpawnType.Vehicle => Color.cyan,
            _ => Color.white
        };
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}
```

### Finding Spawn Points

```csharp
// Via tag
GameObject[] spawns = GameObject.FindGameObjectsWithTag("SpawnPoint");

// Via component (preferred — type-safe)
SpawnPoint[] spawns = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
SpawnPoint playerSpawn = spawns.First(s => s.type == SpawnType.Player);

// Via parent container
Transform container = GameObject.Find("[SpawnPoints]").transform;
for (int i = 0; i < container.childCount; i++)
{
    Transform spawn = container.GetChild(i);
}
```

## NavMesh

AI navigation system for pathfinding on walkable surfaces.

### Baking NavMesh

```
1. Add NavMesh Surface component to a GameObject (or use legacy Window > Navigation)
2. Configure:
   - Agent Type: Humanoid, or create custom (radius, height, step height, slope)
   - Include Layers: which layers are walkable
   - Use Geometry: Render Meshes or Physics Colliders
3. Click Bake

NavMesh Surface (AI Navigation package — recommended over legacy):
  - Component-based, supports runtime baking
  - Multiple surfaces for different agent types
```

### NavMesh Agent

```csharp
NavMeshAgent agent = GetComponent<NavMeshAgent>();

// Set destination
agent.SetDestination(targetPosition);

// Check if arrived
if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
{
    // Reached destination
}

// Properties
agent.speed = 5f;
agent.acceleration = 8f;
agent.angularSpeed = 120f;
agent.stoppingDistance = 1f;
agent.avoidancePriority = 50;  // 0 = highest priority, 99 = lowest
```

### NavMesh Obstacle

```
Dynamic obstacles that carve holes in the NavMesh:
  - Add NavMesh Obstacle component
  - Shape: Box or Capsule
  - Carve: enable for real-time NavMesh modification
  - Carve Only Stationary: enable to avoid per-frame carving (expensive)

Use for: doors, destructible walls, parked vehicles
```

### Off-Mesh Links

```
Connections between disconnected NavMesh areas:
  - Jump gaps, ladders, teleporters
  - Add Off Mesh Link component
  - Set Start and End transforms
  - Bidirectional or one-way
  - Auto-traverse or manual (for animations)

Auto-generation:
  - NavMesh Surface > Generate Links: enable
  - Drop Height: max fall distance for auto-links
  - Jump Distance: max horizontal gap for auto-links
```

