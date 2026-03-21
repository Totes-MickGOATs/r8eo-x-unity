---
name: unity-debugging-profiling
description: Unity Debugging and Profiling
---


# Unity Debugging and Profiling

Use this skill when diagnosing bugs with Debug.Log, profiling frame time or memory usage, or building custom debug visualization tools in Unity.

## Frame Debugger

Open via **Window > Analysis > Frame Debugger**.

Click **Enable** to freeze the current frame and step through every draw call:

- See which objects are drawn in each batch
- Inspect shader properties for each draw call
- Identify why objects are NOT batching (different material, different mesh, etc.)
- See render target switches, shadow passes, post-processing steps

**Common discoveries:**
- Objects not batching because they use different material instances (use `sharedMaterial`)
- Too many shadow-casting lights
- Overdraw from transparent objects
- Unnecessary render target switches

## Memory Profiler

Install package: `com.unity.memoryprofiler`

Open via **Window > Analysis > Memory Profiler**.

### Taking Snapshots

1. Connect to target (Editor or device)
2. Click "Capture New Snapshot"
3. Inspect managed objects, native objects, and allocations
4. Compare two snapshots to find leaks

### Memory Categories

| Category | What it includes |
|----------|-----------------|
| **Managed** | C# objects on the managed heap (GC-tracked) |
| **Native** | Unity engine objects (textures, meshes, AudioClips in VRAM/RAM) |
| **Graphics** | GPU memory (render textures, buffers) |
| **Audio** | Loaded audio data |

### Common Memory Issues

- **Texture too large:** 4K textures loaded for objects that appear small on screen
- **Duplicate textures:** Same texture imported multiple times via different paths
- **Leaked GameObjects:** Instantiated but never Destroyed
- **Event listener leaks:** `+=` without corresponding `-=` prevents GC
- **Addressables not released:** `Addressables.Release()` not called after load

## Physics Debugger

Enable in Scene view: **Gizmos dropdown > Physics > Show All** (or specific options).

Shows: collision shapes, contacts, raycasts, trigger volumes.

Programmatic physics debug:

```csharp
// Visualize all raycasts
Physics.queriesHitBackfaces = false;
Physics.queriesHitTriggers = false;

// In Project Settings > Physics:
// - Contact Pairs Mode: monitor contacts
// - Enable Enhanced Determinism: for replay systems
// - Solver Iterations: increase for complex stacking (default 6)
// - Simulation Mode: Fixed Update (standard) vs Script (manual)
```

## Console Window Tips

- **Collapse** — group identical messages (shows count)
- **Clear on Play** — auto-clear when entering Play mode
- **Error Pause** — pause playback on first error
- **Stack Trace** — set per log type: None, ScriptOnly, Full
  - Use ScriptOnly for Log/Warning (faster)
  - Use Full for Error (need the context)

Filter by custom tags:
```csharp
// Use consistent prefixes so you can filter in Console search bar
Debug.Log("[AI] Pathfinding started");
Debug.Log("[Physics] Collision detected");
Debug.Log("[UI] Menu opened");
```



## Topic Pages

- [Debug Logging](skill-debug-logging.md)
- [Custom Debug Tools](skill-custom-debug-tools.md)

