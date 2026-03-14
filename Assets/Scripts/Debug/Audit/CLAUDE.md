# Debug/Audit/

Physics conformance tracking and debug log persistence via local SQLite database.

## Files

| File | Role |
|------|------|
| `AuditDb.cs` | SQLite connection management, schema init, purge |
| `ConformanceRecorder.cs` | Records conformance check results with tolerance/tier |
| `DebugLogSink.cs` | Hooks Unity console, persists tagged logs to DB |
| `AuditConstants.cs` | Named constants for DB paths, thresholds, tags |
| `R8EOX.Debug.Audit.asmdef` | Assembly definition referencing R8EOX.Debug |

## How It Works

- DB file: `{project_root}/Logs/physics_audit.db` (gitignored)
- Conformance data: kept long-term for trend tracking
- Debug logs: auto-purged after 48 hours on editor startup
- Log correlation: each persisted log gets a `[db:HASH]` suffix in the Unity console, searchable in DB via `log_hash`

## Relevant Skills

- `physics-conformance-audit` -- Full conformance check catalogue (93 checks)
- `debug-system` -- Structured logging patterns, overlay registry
- `unity-physics-tuning` -- RC-specific physics configuration
