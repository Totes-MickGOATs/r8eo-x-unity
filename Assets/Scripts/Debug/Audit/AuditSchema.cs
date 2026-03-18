#if UNITY_EDITOR || DEBUG
namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// SQL schema strings for the physics audit database.
    /// Extracted from <see cref="AuditDb"/> to keep schema declarations separate from
    /// connection lifecycle logic.
    /// </summary>
    internal static class AuditSchema
    {
        internal const string k_CreateConformanceRuns = @"
CREATE TABLE IF NOT EXISTS conformance_runs (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id      TEXT NOT NULL,
    timestamp   TEXT NOT NULL,
    git_sha     TEXT,
    branch      TEXT,
    category    TEXT NOT NULL,
    check_id    TEXT NOT NULL,
    check_name  TEXT NOT NULL,
    expected    REAL,
    actual      REAL,
    tolerance   REAL,
    tier        TEXT,
    passed      INTEGER NOT NULL,
    metadata    TEXT
);
CREATE INDEX IF NOT EXISTS idx_conformance_time ON conformance_runs(timestamp);
CREATE INDEX IF NOT EXISTS idx_conformance_check ON conformance_runs(check_id);
CREATE INDEX IF NOT EXISTS idx_conformance_tier ON conformance_runs(tier);";

        internal const string k_CreateDebugLogs = @"
CREATE TABLE IF NOT EXISTS debug_logs (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp   TEXT NOT NULL,
    frame       INTEGER,
    level       TEXT NOT NULL,
    system      TEXT,
    message     TEXT NOT NULL,
    stack_trace TEXT,
    context     TEXT,
    log_hash    TEXT
);
CREATE INDEX IF NOT EXISTS idx_logs_time ON debug_logs(timestamp);
CREATE INDEX IF NOT EXISTS idx_logs_level ON debug_logs(level);
CREATE INDEX IF NOT EXISTS idx_logs_system ON debug_logs(system);
CREATE INDEX IF NOT EXISTS idx_logs_hash ON debug_logs(log_hash);";

        internal const string k_PurgeSql =
            "DELETE FROM debug_logs WHERE timestamp < datetime('now', '-{0} hours')";
    }
}
#endif
