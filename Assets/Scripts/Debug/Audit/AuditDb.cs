#if UNITY_EDITOR || DEBUG
using System;
using System.IO;
using Mono.Data.Sqlite;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Manages the SQLite connection lifecycle and schema for the physics audit database.
    /// Thread-safe lazy singleton — the database file lives at {project_root}/Logs/physics_audit.db.
    /// </summary>
    public static class AuditDb
    {
        // ---- Private Fields ----

        private static SqliteConnection _connection;
        private static readonly object _lock = new object();
        private static bool _initialized;


        // ---- Schema SQL ----

        private const string k_CreateConformanceRuns = @"
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

        private const string k_CreateDebugLogs = @"
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

        private const string k_PurgeSql =
            "DELETE FROM debug_logs WHERE timestamp < datetime('now', '-{0} hours')";


        // ---- Editor Startup ----

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void OnEditorStartup()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Initialize();
                    Purge();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[AuditDb] Editor startup purge failed: {ex.Message}");
                }
            };

            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        }
#endif


        // ---- Public API ----

        /// <summary>
        /// Returns the open SQLite connection, creating the database and schema on first call.
        /// Thread-safe.
        /// </summary>
        public static SqliteConnection GetConnection()
        {
            lock (_lock)
            {
                if (!_initialized)
                    Initialize();

                return _connection;
            }
        }

        /// <summary>
        /// Explicitly initialises the database connection and creates the schema.
        /// Safe to call multiple times — subsequent calls are no-ops.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized && _connection != null)
                    return;

                string dbDir = Path.Combine(Application.dataPath, "..", "Logs");
                if (!Directory.Exists(dbDir))
                    Directory.CreateDirectory(dbDir);

                string dbPath = Path.Combine(dbDir, "physics_audit.db");
                string connectionString = $"URI=file:{dbPath}";

                _connection = new SqliteConnection(connectionString);
                _connection.Open();

                ExecuteNonQuery(k_CreateConformanceRuns);
                ExecuteNonQuery(k_CreateDebugLogs);

                _initialized = true;
            }
        }

        /// <summary>
        /// Deletes debug_logs older than <see cref="AuditConstants.k_PurgeHours"/> hours,
        /// then vacuums the database to reclaim space.
        /// </summary>
        public static void Purge()
        {
            lock (_lock)
            {
                if (!_initialized || _connection == null)
                    return;

                string sql = string.Format(k_PurgeSql, AuditConstants.k_PurgeHours);
                ExecuteNonQuery(sql);
                ExecuteNonQuery("VACUUM");
            }
        }

        /// <summary>
        /// Executes a non-query SQL statement (INSERT, UPDATE, DELETE, CREATE, etc.).
        /// </summary>
        /// <param name="sql">The SQL statement to execute.</param>
        /// <param name="parameters">Optional named parameters as alternating name/value pairs.</param>
        /// <returns>Number of rows affected.</returns>
        public static int ExecuteNonQuery(string sql, params object[] parameters)
        {
            lock (_lock)
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Executes a query and returns a reader. Caller is responsible for disposing the reader.
        /// NOTE: The caller must NOT hold the lock while iterating — copy results into a list first.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional named parameters as alternating name/value pairs.</param>
        /// <returns>An open <see cref="SqliteDataReader"/>.</returns>
        public static SqliteDataReader ExecuteReader(string sql, params object[] parameters)
        {
            lock (_lock)
            {
                var cmd = _connection.CreateCommand();
                cmd.CommandText = sql;
                AddParameters(cmd, parameters);
                return cmd.ExecuteReader();
            }
        }


        // ---- Private Helpers ----

        private static void AddParameters(SqliteCommand cmd, object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return;

            for (int i = 0; i < parameters.Length - 1; i += 2)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = parameters[i].ToString();
                param.Value = parameters[i + 1] ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (_connection != null)
                {
                    try
                    {
                        _connection.Close();
                        _connection.Dispose();
                    }
                    catch
                    {
                        // Best-effort cleanup on domain unload.
                    }

                    _connection = null;
                    _initialized = false;
                }
            }
        }
    }
}
#endif
