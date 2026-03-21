# Rigidbody Fundamentals

> Part of the `unity-physics-3d` skill. See [SKILL.md](SKILL.md) for the overview.

## Rigidbody Fundamentals

> **Unity 6:** `Rigidbody.drag` was renamed to `Rigidbody.linearDamping` and `Rigidbody.angularDrag` was renamed to `Rigidbody.angularDamping`. The old names are removed and will cause compile errors.

The `Rigidbody` component makes a GameObject participate in physics simulation.

### Key Properties

| Property | Default | Purpose |
|----------|---------|---------|
| `mass` | 1 | Mass in kg. Affects force calculations, not gravity fall speed |
| `linearDamping` | 0 | Linear air resistance. 0 = no drag, higher = slower movement |
| `angularDamping` | 0.05 | Rotational air resistance |
| `useGravity` | true | Whether gravity affects this body |
| `isKinematic` | false | If true, not driven by physics — moved only via transform or `MovePosition` |
| `interpolation` | None | Smoothing between physics steps for visual jitter reduction |
| `collisionDetectionMode` | Discrete | CCD mode for fast-moving objects |

### Interpolation Modes

```csharp
// None — no smoothing, can appear jittery at low fixed timestep
rb.interpolation = RigidbodyInterpolation.None;

// Interpolate — smooths based on previous frame positions (slight lag, most stable)
rb.interpolation = RigidbodyInterpolation.Interpolate;

// Extrapolate — predicts next position (no lag, can overshoot)
rb.interpolation = RigidbodyInterpolation.Extrapolate;
```

**Rule of thumb:** Use `Interpolate` on the player-controlled body. Leave others at `None`.

### Kinematic vs Dynamic

```csharp
// Dynamic — physics engine moves it, responds to forces/collisions
rb.isKinematic = false;

// Kinematic — you move it, still generates collision events
// Use for: moving platforms, doors, elevators
rb.isKinematic = true;
rb.MovePosition(targetPos);   // Use this, NOT transform.position
rb.MoveRotation(targetRot);   // Use this, NOT transform.rotation
```

### Constraints

Lock position or rotation on specific axes:

```csharp
rb.constraints = RigidbodyConstraints.FreezePositionY
               | RigidbodyConstraints.FreezeRotationX
               | RigidbodyConstraints.FreezeRotationZ;
```

## Colliders

Every physics-interacting object needs at least one collider.

### Primitive Colliders (Preferred for Performance)

```
BoxCollider       — walls, crates, floors, platforms
SphereCollider    — balls, pickups, spherical triggers
CapsuleCollider   — characters, projectiles, cylindrical objects
```

### MeshCollider

```csharp
// Convex — required for Rigidbody, max 255 triangles, closed shape
meshCollider.convex = true;

// Non-convex (concave) — static geometry only (no Rigidbody)
// Use for: terrain-like static meshes, level geometry
meshCollider.convex = false;
```

**Performance hierarchy:** Sphere > Capsule > Box > Convex Mesh > Concave Mesh.

### Compound Colliders

Attach multiple primitive colliders on child GameObjects. Physics treats them as one body:

```
Player (Rigidbody)
  ├── Body (BoxCollider)
  ├── Head (SphereCollider)
  └── Feet (SphereCollider)
```

### Terrain Collider

Automatically generated from Unity Terrain heightmap. Efficient for large terrains but cannot be convex.

## Collision Layers and Layer Masks

### Setup in Project Settings

Edit via **Edit > Project Settings > Physics > Layer Collision Matrix**.

```csharp
// Assign layer in code
gameObject.layer = LayerMask.NameToLayer("Vehicles");

// Create layer mask for raycasts
int groundMask = LayerMask.GetMask("Ground", "Terrain");
int everythingExceptUI = ~LayerMask.GetMask("UI");
```

### Recommended Layer Layout

| Layer | Name | Collides With |
|-------|------|---------------|
| 0 | Default | Everything |
| 6 | Terrain | Vehicles, Players, Projectiles |
| 7 | Vehicles | Terrain, Vehicles, Obstacles |
| 8 | Players | Terrain, Obstacles, Pickups |
| 9 | Projectiles | Terrain, Obstacles, Players (not self) |
| 10 | Triggers | Players, Vehicles |
| 11 | Obstacles | Everything except Triggers |

### Layer Mask in Raycasts

```csharp
int layerMask = LayerMask.GetMask("Ground", "Obstacles");
if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
{
    Debug.Log($"Hit {hit.collider.name} at {hit.point}");
}
```

