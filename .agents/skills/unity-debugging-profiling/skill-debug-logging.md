# Debug Logging

> Part of the `unity-debugging-profiling` skill. See [SKILL.md](SKILL.md) for the overview.

## Debug Logging

### Structured Logging Patterns

```csharp
// Basic logging
Debug.Log("Player spawned at position " + transform.position);
Debug.LogWarning("Health below 10% — entering critical state");
Debug.LogError("Failed to load save file: " + filePath);

// Rich text in Console
Debug.Log("<color=green><b>SPAWN</b></color> Player at " + pos);

// Context object — clicking the log selects this object in Hierarchy
Debug.Log("Damage taken: " + amount, gameObject);

// Structured tag-based logging
public static class GameLog
{
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD"),
     System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(string tag, string message, Object context = null)
    {
        Debug.Log($"[<color=cyan>{tag}</color>] {message}", context);
    }

    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD"),
     System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Warn(string tag, string message, Object context = null)
    {
        Debug.LogWarning($"[{tag}] {message}", context);
    }

    public static void Error(string tag, string message, Object context = null)
    {
        // Errors always log (no conditional)
        Debug.LogError($"[{tag}] {message}", context);
    }
}

// Usage
GameLog.Log("AI", "Pathfinding complete, 23 nodes explored", gameObject);
GameLog.Warn("Physics", "Rigidbody sleeping unexpectedly");
GameLog.Error("Save", $"Corrupt save file: {path}");
```

### Conditional Compilation

```csharp
#if UNITY_EDITOR
    // Only in the Unity Editor
    Debug.Log("Editor-only debug info");
#endif

#if DEVELOPMENT_BUILD
    // In development builds (Player Settings > Development Build checkbox)
    ShowDebugOverlay();
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Both editor and dev builds
    DrawDebugGizmos();
#endif

// Custom defines (Project Settings > Player > Scripting Define Symbols)
#if ENABLE_CHEAT_CONSOLE
    ProcessCheatCommand(input);
#endif

// Platform checks
#if UNITY_STANDALONE_WIN
    // Windows-specific code
#elif UNITY_STANDALONE_OSX
    // macOS-specific code
#elif UNITY_WEBGL
    // WebGL-specific code
#endif
```

**Important:** `[System.Diagnostics.Conditional("SYMBOL")]` is better than `#if` for methods — the entire call site is stripped, not just the method body. No allocation for string parameters when the symbol is not defined.

## Runtime Visualization

### Debug.DrawRay / Debug.DrawLine

Visible in Scene view (and Game view if Gizmos are enabled):

```csharp
private void FixedUpdate()
{
    // Visualize forward direction
    Debug.DrawRay(transform.position, transform.forward * 5f, Color.blue);

    // Visualize velocity
    Debug.DrawRay(transform.position, rb.linearVelocity, Color.red);

    // Raycast with debug visualization
    if (Physics.Raycast(transform.position, Vector3.down, out var hit, 10f))
    {
        Debug.DrawLine(transform.position, hit.point, Color.green);
        Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.yellow);
    }
    else
    {
        Debug.DrawRay(transform.position, Vector3.down * 10f, Color.red);
    }
}

// Duration parameter — line persists for N seconds
Debug.DrawLine(a, b, Color.magenta, duration: 2f);

// depthTest parameter — false means visible through walls
Debug.DrawRay(pos, dir, Color.white, 0f, depthTest: false);
```

### Gizmos — Editor Visualization

```csharp
public class WaypointNode : MonoBehaviour
{
    public float radius = 2f;
    public WaypointNode nextWaypoint;

    // Always drawn when object is visible in Scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }

    // Only drawn when this object is selected
    private void OnDrawGizmosSelected()
    {
        // Wireframe sphere showing trigger range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        // Line to next waypoint
        if (nextWaypoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, nextWaypoint.transform.position);

            // Arrow head
            Vector3 dir = (nextWaypoint.transform.position - transform.position).normalized;
            Vector3 mid = Vector3.Lerp(transform.position, nextWaypoint.transform.position, 0.5f);
            Gizmos.DrawRay(mid, Quaternion.Euler(0, 30, 0) * -dir * 0.5f);
            Gizmos.DrawRay(mid, Quaternion.Euler(0, -30, 0) * -dir * 0.5f);
        }
    }
}
```

### Gizmo Utilities

```csharp
// Draw shapes
Gizmos.DrawCube(center, size);
Gizmos.DrawWireCube(center, size);
Gizmos.DrawSphere(center, radius);
Gizmos.DrawWireSphere(center, radius);
Gizmos.DrawLine(from, to);
Gizmos.DrawRay(origin, direction);
Gizmos.DrawFrustum(center, fov, maxRange, minRange, aspect);
Gizmos.DrawMesh(mesh, position, rotation, scale);
Gizmos.DrawIcon(position, iconName, allowScaling); // icon in Assets/Gizmos/

// Transform-aware gizmos
Gizmos.matrix = transform.localToWorldMatrix;
Gizmos.DrawWireCube(Vector3.zero, Vector3.one); // draws in local space
Gizmos.matrix = Matrix4x4.identity; // reset
```

## Unity Profiler

Open via **Window > Analysis > Profiler** (or Ctrl+7).

### Profiler Modules

| Module | What it shows |
|--------|--------------|
| **CPU Usage** | Frame time breakdown by category (scripts, rendering, physics, animation, GC) |
| **GPU Usage** | GPU frame time, shader passes |
| **Rendering** | Draw calls, triangles, vertices, set pass calls, batches |
| **Memory** | Managed heap, native allocations, GC activity |
| **Physics** | Active bodies, contacts, solver iterations, broadphase pairs |
| **Audio** | Playing sources, DSP load, channel count |
| **UI** | Canvas rebuilds, layout passes, batch count |

### CPU Usage — Reading the Timeline

The timeline shows each frame as a stacked bar. Key things to look for:

- **Tall spikes** — frame time exceeding budget (16.6ms for 60fps, 33.3ms for 30fps)
- **GC.Alloc** markers — garbage collection allocations (orange markers)
- **WaitForTargetFPS** — idle time when VSync is on (this is NOT a problem)
- **Gfx.WaitForPresent** — CPU waiting for GPU (GPU-bound)
- **Scripts** section — your C# code time

### Custom Profiler Markers

```csharp
using Unity.Profiling;

public class AIManager : MonoBehaviour
{
    static readonly ProfilerMarker s_PathfindMarker = new("AI.Pathfinding");
    static readonly ProfilerMarker s_DecisionMarker = new("AI.DecisionMaking");

    private void Update()
    {
        s_PathfindMarker.Begin();
        RunPathfinding();
        s_PathfindMarker.End();

        // Using block for auto-end
        using (s_DecisionMarker.Auto())
        {
            MakeDecisions();
        }
    }
}

// Legacy API (still works, less efficient)
private void Update()
{
    Profiler.BeginSample("MyExpensiveOperation");
    DoExpensiveThing();
    Profiler.EndSample();
}
```

### Deep Profiling

Enable via Profiler window toolbar. Records every method call with timing.

**Warning:** Deep profiling adds significant overhead (10-100x slower). Use it to find which method is slow, then switch back to normal profiling for accurate timing.

Better alternative: Add `ProfilerMarker` to suspected methods manually. Much less overhead.

