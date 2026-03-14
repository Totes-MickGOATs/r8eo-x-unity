#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Hooks Unity's log system and persists tagged debug messages to the audit SQLite database.
    /// Only messages with a recognised system tag (e.g. [physics], [grip]) are captured.
    /// Each persisted log receives a correlation hash appended to the console output as [db:HASH].
    /// </summary>
    public class DebugLogSink : MonoBehaviour
    {
        // ---- Constants ----

        /// <summary>Regex matching a system tag at the start of a log message: [tagname]</summary>
        private static readonly Regex k_TagPattern = new Regex(
            @"^\[(\w+)\]\s*(.*)$", RegexOptions.Singleline);

        /// <summary>Regex matching trailing JSON context: { ... }</summary>
        private static readonly Regex k_JsonContextPattern = new Regex(
            @"\{[^{}]+\}\s*$", RegexOptions.Singleline);


        // ---- Private Fields ----

        private readonly List<LogEntry> _buffer = new List<LogEntry>();
        private readonly object _bufferLock = new object();
        private float _lastFlushTime;
        private static SHA256 _hasher;


        // ---- Nested Types ----

        private struct LogEntry
        {
            public string Timestamp;
            public int Frame;
            public string Level;
            public string System;
            public string Message;
            public string StackTrace;
            public string Context;
            public string LogHash;
        }


        // ---- Runtime Bootstrap ----

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindAnyObjectByType<DebugLogSink>() != null)
                return;

            var go = new GameObject("[DebugLogSink]");
            go.AddComponent<DebugLogSink>();
            DontDestroyOnLoad(go);
        }


        // ---- Unity Lifecycle ----

        void OnEnable()
        {
            _hasher = SHA256.Create();
            _lastFlushTime = Time.realtimeSinceStartup;
            Application.logMessageReceived += HandleLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            Flush();

            if (_hasher != null)
            {
                _hasher.Dispose();
                _hasher = null;
            }
        }

        void Update()
        {
            float elapsed = Time.realtimeSinceStartup - _lastFlushTime;
            if (elapsed >= AuditConstants.k_FlushIntervalSeconds)
                Flush();
        }


        // ---- Private Methods ----

        private void HandleLog(string logMessage, string stackTrace, LogType logType)
        {
            if (string.IsNullOrEmpty(logMessage))
                return;

            var tagMatch = k_TagPattern.Match(logMessage);
            if (!tagMatch.Success)
                return;

            string tag = tagMatch.Groups[1].Value.ToLowerInvariant();
            if (!AuditConstants.k_RecognizedTags.Contains(tag))
                return;

            string messageBody = tagMatch.Groups[2].Value;
            string level = LogTypeToLevel(logType);
            string logHash = ComputeHash(level, tag, messageBody);

            // Extract optional JSON context from the end of the message.
            string context = null;
            var jsonMatch = k_JsonContextPattern.Match(messageBody);
            if (jsonMatch.Success)
                context = jsonMatch.Value.Trim();

            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Frame = Time.frameCount,
                Level = level,
                System = tag,
                Message = messageBody,
                StackTrace = stackTrace,
                Context = context,
                LogHash = logHash
            };

            lock (_bufferLock)
            {
                _buffer.Add(entry);

                if (_buffer.Count >= AuditConstants.k_RingBufferCapacity)
                    FlushLocked();
            }

            // Re-emit with correlation hash so developers can search the DB.
            // Guard against infinite recursion: the re-emitted message won't match
            // a recognised tag because it starts with "  -> " not "[tag]".
            UnityEngine.Debug.Log($"  -> {logMessage} [db:{logHash}]");
        }

        private void Flush()
        {
            lock (_bufferLock)
            {
                FlushLocked();
            }
        }

        /// <summary>
        /// Writes all buffered entries to the database. Must be called under <see cref="_bufferLock"/>.
        /// </summary>
        private void FlushLocked()
        {
            if (_buffer.Count == 0)
                return;

            _lastFlushTime = Time.realtimeSinceStartup;

            try
            {
                var conn = AuditDb.GetConnection();

                const string insertSql = @"
INSERT INTO debug_logs
    (timestamp, frame, level, system, message, stack_trace, context, log_hash)
VALUES
    (@timestamp, @frame, @level, @system, @message, @stack_trace, @context, @log_hash)";

                using (var transaction = conn.BeginTransaction())
                {
                    foreach (var entry in _buffer)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = insertSql;
                            cmd.Parameters.AddWithValue("@timestamp", entry.Timestamp);
                            cmd.Parameters.AddWithValue("@frame", entry.Frame);
                            cmd.Parameters.AddWithValue("@level", entry.Level);
                            cmd.Parameters.AddWithValue("@system", entry.System);
                            cmd.Parameters.AddWithValue("@message", entry.Message);
                            cmd.Parameters.AddWithValue("@stack_trace",
                                (object)entry.StackTrace ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@context",
                                (object)entry.Context ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@log_hash", entry.LogHash);
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                // Avoid infinite recursion — don't use tagged logging here.
                UnityEngine.Debug.LogWarning($"[AuditDb] Flush failed: {ex.Message}");
            }

            _buffer.Clear();
        }

        private static string ComputeHash(string level, string system, string message)
        {
            string input = level + system + message;
            byte[] hashBytes = _hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(AuditConstants.k_HashLength);
            for (int i = 0; i < AuditConstants.k_HashLength / 2; i++)
                sb.Append(hashBytes[i].ToString("x2"));
            return sb.ToString();
        }

        private static string LogTypeToLevel(LogType logType)
        {
            switch (logType)
            {
                case LogType.Error:
                case LogType.Exception:
                    return "error";
                case LogType.Assert:
                    return "assert";
                case LogType.Warning:
                    return "warning";
                default:
                    return "info";
            }
        }
    }
}
#endif
