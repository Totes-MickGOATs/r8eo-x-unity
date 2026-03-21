---
name: debug-system
description: Debug System Skill
---


# Debug System Skill

Use this skill when building debug overlays, structured logging, runtime inspection tools, or F-key diagnostic displays. Covers the debug autoload pattern, overlay management, and CI integration.

## Physics Audit Database

The audit system persists physics debug data to a local SQLite database at `Logs/physics_audit.db`. It consists of:

- **`DebugLogSink`** — MonoBehaviour that hooks `Application.logMessageReceived` and persists tagged log messages. Use the format `[system] message` for any physics-related debug output, where system is one of: `physics`, `grip`, `suspension`, `drivetrain`, `air`, `esc`, `input`, `surface`, `conformance`.
- **`ConformanceRecorder`** — Records conformance check results with tolerance tiers (Excellent/Good/Noticeable/Poor/Broken).
- **`AuditDb`** — Manages SQLite connection lifecycle, schema creation, and automatic purge of old logs.

Source: `Assets/Scripts/Debug/Audit/`

---

## Architecture: Debug Autoload

The debug system is a singleton (autoload) that manages all debug state, logging, and overlays. It is always available and safe to call from anywhere in the codebase.

### Core Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **Structured logging** | Tagged, leveled log output with filtering |
| **Overlay management** | Toggle debug displays on/off with function keys |
| **Runtime inspection** | Read and display variable values in real-time |
| **Performance metrics** | FPS, frame time, memory usage |
| **CI integration** | Structured output for headless test environments |

### Design Principles

1. **Zero-cost when disabled** — debug checks should short-circuit immediately when the debug system is off
2. **Always safe to call** — `Debug.log()` must never crash, even if the debug system isn't initialized
3. **No gameplay impact** — debug overlays use `PROCESS_MODE_ALWAYS` and don't pause or alter game state
4. **Survives scene changes** — as an autoload, debug state persists across scene transitions

## Implementation Checklist

When building a debug system for a new project:

- [ ] Debug autoload registered in project settings
- [ ] `Debug.log(tag, message)` API implemented with level filtering
- [ ] Tag enable/disable support
- [ ] F-key overlay registry
- [ ] Performance overlay (FPS, frame time, memory)
- [ ] `Debug.watch()` variable display
- [ ] Visual debug drawing (lines, spheres, labels)
- [ ] CI/headless mode detection with adapted output
- [ ] Zero-cost when disabled (early return checks)
- [ ] Documentation of F-key assignments visible in-game or in project docs
- [ ] No bare `print()` calls in non-tool scripts (enforced by lint rule or code review)


## Topic Pages

- [Structured Logging](skill-structured-logging.md)

