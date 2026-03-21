#if UNITY_EDITOR || DEBUG
using System.IO;
using Mono.Data.Sqlite;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Thread-safe lazy SQLite connection manager. Database file: {project_root}/Logs/physics_audit.db.
    /// Schema SQL is in <see cref="AuditSchema"/>. Lifecycle hooks are in <see cref="AuditDbLifecycle"/>.
    /// </summary>
    public static class AuditDb
    {
        // ---- Private Fields ----

        private static SqliteConnection _connection;
        private static readonly object _lock = new object();
        private static bool _initialized;


        // ---- Editor Startup ----

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void OnEditorStartup() => AuditDbLifecycle.RegisterEditorCallbacks();
#endif


        // ---- Public API ----

        /// <summary>Returns the open SQLite connection, creating database and schema on first call. Thread-safe.</summary>
        public static SqliteConnection GetConnection()
        {
            lock (_lock)
            {
                if (!_initialized)
                    Initialize();

                return _connection;
            }
        }

        /// <summary>Initialises the database connection and creates the schema. Safe to call multiple times.</summary>
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
                _connection = new SqliteConnection($"URI=file:{dbPath}");
                _connection.Open();

                ExecuteNonQuery(AuditSchema.k_CreateConformanceRuns);
                ExecuteNonQuery(AuditSchema.k_CreateDebugLogs);

                _initialized = true;
            }
        }

        /// <summary>Deletes debug_logs older than <see cref="AuditConstants.k_PurgeHours"/> hours, then VACUUMs.</summary>
        public static void Purge()
        {
            lock (_lock)
            {
                if (!_initialized || _connection == null)
                    return;

                string sql = string.Format(AuditSchema.k_PurgeSql, AuditConstants.k_PurgeHours);
                ExecuteNonQuery(sql);
                ExecuteNonQuery("VACUUM");
            }
        }

        /// <summary>Executes a non-query SQL statement. Parameters are alternating name/value pairs.</summary>
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

        /// <summary>Executes a query and returns a reader. Caller disposes. Copy results before releasing the lock.</summary>
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


        /// <summary>Closes and disposes the connection. Called by <see cref="AuditDbLifecycle"/> on domain unload.</summary>
        public static void CloseConnection()
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
    }
}
#endif
