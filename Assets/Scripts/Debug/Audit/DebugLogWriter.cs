#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using UnityEngine;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Writes buffered <see cref="DebugLogSink"/> entries to the SQLite audit database.
    /// Extracted from <see cref="DebugLogSink"/> to isolate the persistence concern.
    /// </summary>
    public static class DebugLogWriter
    {
        // ---- SQL ----

        private const string k_InsertSql = @"
INSERT INTO debug_logs
    (timestamp, frame, level, system, message, stack_trace, context, log_hash)
VALUES
    (@timestamp, @frame, @level, @system, @message, @stack_trace, @context, @log_hash)";

        // ---- Nested Type ----

        /// <summary>One captured log message ready to persist.</summary>
        public struct Entry
        {
            public string Timestamp;
            public int    Frame;
            public string Level;
            public string System;
            public string Message;
            public string StackTrace;
            public string Context;
            public string LogHash;
        }

        // ---- Public API ----

        /// <summary>
        /// Writes all entries to the database in a single transaction.
        /// Silently no-ops if the list is empty.
        /// Logs a warning (without a recognised tag) if the write fails.
        /// </summary>
        public static void WriteEntries(List<Entry> entries)
        {
            if (entries == null || entries.Count == 0)
                return;

            try
            {
                var conn = AuditDb.GetConnection();
                using (var transaction = conn.BeginTransaction())
                {
                    foreach (var entry in entries)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = k_InsertSql;
                            cmd.Parameters.AddWithValue("@timestamp",   entry.Timestamp);
                            cmd.Parameters.AddWithValue("@frame",       entry.Frame);
                            cmd.Parameters.AddWithValue("@level",       entry.Level);
                            cmd.Parameters.AddWithValue("@system",      entry.System);
                            cmd.Parameters.AddWithValue("@message",     entry.Message);
                            cmd.Parameters.AddWithValue("@stack_trace",
                                (object)entry.StackTrace ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@context",
                                (object)entry.Context ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@log_hash",    entry.LogHash);
                        }
                    }
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                // Avoid infinite recursion — do not use a recognised tag here.
                Debug.LogWarning($"[AuditDb] Flush failed: {ex.Message}");
            }
        }
    }
}
#endif
