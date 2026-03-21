# Environment Art Pipeline

> Part of the `unity-3d-world-building` skill. See [SKILL.md](SKILL.md) for the overview.

## Environment Art Pipeline

### Importing FBX Models

```
Import Settings:
  - Scale Factor: 1.0 (if DCC tool uses meters)
    - Blender: export with "Apply Transform" and scale 1.0
    - Maya: uses cm, so Factor = 0.01 (or set to meters in Maya)
  - Convert Units: usually ON
  - Import BlendShapes: OFF if not using morph targets
  - Mesh Compression: Off for hero, Low-Medium for props
  - Generate Colliders: OFF (add manually for control)

1 Unity unit = 1 meter. All DCC exports should target this.
```

### Material Remapping

```
When importing FBX with materials:
  1. Import settings > Materials tab
  2. Material Creation Mode: Import via MaterialDescription (default)
  3. Location: Use Embedded Materials (initial), then Extract Materials
  4. Remap to project materials in the Remapped Materials list

Best practice:
  - Extract materials once, assign project materials
  - Don't edit embedded materials (they reset on reimport)
```

### Scale Reference

```
Standard measurements for scale consistency:
  - Door: 2.0m tall, 0.9m wide
  - Ceiling: 2.7-3.0m
  - Character: 1.8m (average human)
  - Step: 0.15-0.2m high, 0.3m deep
  - Railing: 0.9-1.0m
  - Road lane: 3.5m wide
  - Sidewalk: 1.5-2.0m wide

Keep a reference cube (1x1x1m) in the scene while building.
```

