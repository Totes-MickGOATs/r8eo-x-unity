#!/usr/bin/env python3
"""Patch RCBuggy.prefab: scale body visual meshes from 1/4 to 1:1 (4x multiplier)."""

import sys

PREFAB_PATH = "Assets/Prefabs/RCBuggy.prefab"

replacements = [
    # ChassisPlate position (unique value in file)
    ("m_LocalPosition: {x: 0, y: -0.233, z: 0}",
     "m_LocalPosition: {x: 0, y: -0.932, z: 0}"),
    # ChassisPlate scale
    ("m_LocalScale: {x: 0.52, y: 0.032, z: 1.36}",
     "m_LocalScale: {x: 2.08, y: 0.128, z: 5.44}"),
    # FrontBumperMesh position
    ("m_LocalPosition: {x: 0, y: -0.153, z: 0.78}",
     "m_LocalPosition: {x: 0, y: -0.612, z: 3.12}"),
    # FrontBumperMesh scale
    ("m_LocalScale: {x: 0.48, y: 0.12, z: 0.1}",
     "m_LocalScale: {x: 1.92, y: 0.48, z: 0.4}"),
    # RearBumperMesh position
    ("m_LocalPosition: {x: 0, y: -0.153, z: -0.72}",
     "m_LocalPosition: {x: 0, y: -0.612, z: -2.88}"),
    # RearBumperMesh scale
    ("m_LocalScale: {x: 0.4, y: 0.16, z: 0.16}",
     "m_LocalScale: {x: 1.6, y: 0.64, z: 0.64}"),
    # FrontShockTower position
    ("m_LocalPosition: {x: 0, y: -0.073, z: 0.48}",
     "m_LocalPosition: {x: 0, y: -0.292, z: 1.92}"),
    # FrontShockTower scale
    ("m_LocalScale: {x: 0.4, y: 0.24, z: 0.02}",
     "m_LocalScale: {x: 1.6, y: 0.96, z: 0.08}"),
    # RearShockTower position
    ("m_LocalPosition: {x: 0, y: -0.073, z: -0.48}",
     "m_LocalPosition: {x: 0, y: -0.292, z: -1.92}"),
    # RearShockTower scale
    ("m_LocalScale: {x: 0.32, y: 0.24, z: 0.02}",
     "m_LocalScale: {x: 1.28, y: 0.96, z: 0.08}"),
    # BodyShell position
    ("m_LocalPosition: {x: 0, y: -0.05, z: 0.08}",
     "m_LocalPosition: {x: 0, y: -0.2, z: 0.32}"),
    # BodyShell scale
    ("m_LocalScale: {x: 0.48, y: 0.16, z: 1.12}",
     "m_LocalScale: {x: 1.92, y: 0.64, z: 4.48}"),
    # RearWing position
    ("m_LocalPosition: {x: 0, y: 0.167, z: -0.6}",
     "m_LocalPosition: {x: 0, y: 0.668, z: -2.4}"),
    # RearWing scale
    ("m_LocalScale: {x: 0.48, y: 0.008, z: 0.16}",
     "m_LocalScale: {x: 1.92, y: 0.032, z: 0.64}"),
    # FrontArmL position
    ("m_LocalPosition: {x: -0.26, y: -0.213, z: 0.68}",
     "m_LocalPosition: {x: -1.04, y: -0.852, z: 2.72}"),
    # FrontArmR position
    ("m_LocalPosition: {x: 0.26, y: -0.213, z: 0.68}",
     "m_LocalPosition: {x: 1.04, y: -0.852, z: 2.72}"),
    # RearArmL position
    ("m_LocalPosition: {x: -0.26, y: -0.213, z: -0.68}",
     "m_LocalPosition: {x: -1.04, y: -0.852, z: -2.72}"),
    # RearArmR position
    ("m_LocalPosition: {x: 0.26, y: -0.213, z: -0.68}",
     "m_LocalPosition: {x: 1.04, y: -0.852, z: -2.72}"),
    # Arms scale (all 4 share the same value)
    ("m_LocalScale: {x: 0.26, y: 0.02, z: 0.08}",
     "m_LocalScale: {x: 1.04, y: 0.08, z: 0.32}"),
]

with open(PREFAB_PATH, encoding="utf-8") as f:
    content = f.read()

original_content = content
all_ok = True

for old, new in replacements:
    count = content.count(old)
    print(f"Count={count}  '{old[:60].strip()}'")
    if count == 0:
        print("  ERROR: pattern not found!", file=sys.stderr)
        all_ok = False
    content = content.replace(old, new)

if not all_ok:
    print("\nABORTED: One or more patterns not found. No file written.", file=sys.stderr)
    sys.exit(1)

with open(PREFAB_PATH, "w", encoding="utf-8") as f:
    f.write(content)

print(f"\nPatched {PREFAB_PATH} successfully.")
