# Debug/Diagnostics/

Runtime diagnostic checks that validate environment and configuration assumptions.
Used by the debug boot sequence and in-editor tooling.

## Files

| File | Purpose |
|------|---------|
| `TerrainDiagnosticChecks.cs` | Validates terrain setup: layer assignments, surface zone coverage, and collider presence |

## Relevant Skills

- **`debug-system`** — Debug overlay, diagnostic check patterns, and `DebugLogSink` integration
