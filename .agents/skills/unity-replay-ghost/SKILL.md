---
name: unity-replay-ghost
description: Unity Replay & Ghost System -- RC Racing
---


# Unity Replay & Ghost System -- RC Racing

Use this skill when implementing replay recording, ghost car playback, lap ghost storage/sharing, or replay camera systems. Covers the full pipeline from state capture through compressed storage to interpolated playback with Cinemachine cameras.

---

## Table of Contents

1. [Why State Recording, Not Input Recording](#1-why-state-recording-not-input-recording)
2. [GhostFrame Struct](#2-ghostframe-struct)
3. [Recording Pipeline](#3-recording-pipeline)
4. [Compression Pipeline](#4-compression-pipeline)
5. [Storage Format](#5-storage-format)
6. [Ghost Playback](#6-ghost-playback)
7. [Ghost Visual Representation](#7-ghost-visual-representation)
8. [Cinemachine Replay Cameras](#8-cinemachine-replay-cameras)
9. [Time Scrubbing](#9-time-scrubbing)
10. [Async I/O](#10-async-io)
11. [File Versioning](#11-file-versioning)
12. [Anti-Cheat Validation](#12-anti-cheat-validation)
13. [Networking and Distribution](#13-networking-and-distribution)
14. [Performance Budget](#14-performance-budget)
15. [Testing Strategy](#15-testing-strategy)

---

## 1. Why State Recording, Not Input Recording

PhysX is **non-deterministic** across runs. Floating-point accumulation order varies
with frame timing, thread scheduling, and solver iteration counts. Replaying the
same input sequence produces divergent results within seconds -- unacceptable for a
ghost that must match the original lap exactly.

**State recording** captures the vehicle transform and key dynamic values each
tick. Playback reconstructs the visual path without re-simulating physics. This
trades storage size for correctness -- a worthwhile tradeoff given the compression
pipeline below.

| Approach | Determinism Required | PhysX Compatible | Recommended |
|----------|---------------------|------------------|-------------|
| Input recording | Yes -- identical physics each playback | No -- PhysX non-deterministic | NO |
| State recording | No -- replays actual positions | Yes -- records what happened | YES |

### What We Do NOT Record

- Raw input axes (non-deterministic replay)
- Full PhysX solver state (massive, version-coupled)
- Other vehicles state (ghost is single-vehicle)
- Audio/VFX triggers (derived from playback state at runtime)

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-camera-systems`** | Cinemachine setup, virtual camera configuration, camera blending |
| **`unity-save-load`** | Serialization patterns, persistent data, file I/O |
| **`unity-performance-optimization`** | Async patterns, memory management, compression |


## Topic Pages

- [2. GhostFrame Struct](skill-2-ghostframe-struct.md)

