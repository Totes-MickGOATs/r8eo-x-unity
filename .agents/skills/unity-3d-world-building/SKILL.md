---
name: unity-3d-world-building
description: Unity 3D World Building
---


# Unity 3D World Building

Use this skill when building 3D environments, sculpting terrain, placing props, or setting up scene geometry in Unity.

## Prefab Workflows

### Prefab Basics

```
Create: drag GameObject from Hierarchy to Project window
  - Creates .prefab asset
  - Instances in scene are linked to the prefab
  - Blue text in Hierarchy = prefab instance

Edit:
  - Double-click prefab asset → Prefab Mode (isolated editing)
  - Or select instance → Inspector > Overrides > Open Prefab

Apply/Revert overrides:
  - Instance changes appear as bold properties
  - Apply: push instance changes back to prefab
  - Revert: discard instance changes
```

### Nested Prefabs

```
Prefabs containing other prefab instances:
  Building (prefab)
    ├── Wall (prefab)
    ├── Door (prefab)
    └── Window (prefab)

Changes to Door prefab automatically propagate to all Buildings.
Override specific instances as needed.
```

### Prefab Variants

```
Create: right-click prefab > Create > Prefab Variant
  - Inherits everything from base prefab
  - Override specific properties
  - Base changes propagate to variant (unless overridden)

Example:
  EnemyBase (prefab) → EnemyFast (variant, speed override)
                      → EnemyTank (variant, health override)
```

### Prefab Best Practices

```
- Root object should have the main script component
- Keep transform at origin (0,0,0) in prefab mode
- Use prefab variants for color/size/behavior variations
- Don't nest too deeply (3 levels max for sanity)
- Unpack prefab instance if you need to break the link permanently
```

## Object Placement

### Snapping

```
Grid Snapping:
  - Hold Ctrl while moving (increment snap)
  - Edit > Grid and Snap Settings
  - Set Move/Rotate/Scale snap values

Surface Snapping:
  - Hold Ctrl+Shift while moving
  - Object snaps to surface below cursor
  - Aligns to surface normal (rotation)

Vertex Snapping:
  - Hold V, hover over vertex, drag to target vertex
  - Precise alignment of mesh edges/corners
```

### ProGrids (Package)

```
Persistent grid snapping:
  - Visible 3D grid in Scene view
  - Objects snap to grid automatically
  - Toggle grid sizes (0.25, 0.5, 1.0 meters)
  - Push to Grid: align selected object to nearest grid point
```



## Topic Pages

- [Unity Terrain](skill-unity-terrain.md)
- [LOD Groups](skill-lod-groups.md)
- [Environment Art Pipeline](skill-environment-art-pipeline.md)
- [Workflow Tips](skill-workflow-tips.md)

