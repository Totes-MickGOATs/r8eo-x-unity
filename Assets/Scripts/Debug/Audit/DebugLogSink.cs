#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Hooks Unity's log system and persists tagged debug messages to the audit SQLite database.
    /// Only messages with a recognised system tag (e.g. [physics], [grip]) are captured.
    /// Each persisted log receives a correlation hash appended to the console output as [db:HASH].
    /// Log parsing and hash logic live in <see cref="LogParser"/>.
    /// Persistence logic lives in <see cref="DebugLogWriter"/>.
    /// </summary>
    public class DebugLogSink : MonoBehaviour
    {
        // ---- Private Fields ----

        private readonly List<DebugLogWriter.Entry> _buffer = new List<DebugLogWriter.Entry>();
        private readonly object _bufferLock = new object();
        private float _lastFlushTime;
        private SHA256 _hasher;


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

            if (!LogParser.TryParseTag(logMessage, out string tag, out string messageBody))
                return;

            string level   = LogParser.LogTypeToLevel(logType);
            string logHash = LogParser.ComputeHash(_hasher, level, tag, messageBody);
            string context = LogParser.ExtractJsonContext(messageBody);

            var entry = new DebugLogWriter.Entry
            {
                Timestamp  = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Frame      = Time.frameCount,
                Level      = level,
                System     = tag,
                Message    = messageBody,
                StackTrace = stackTrace,
                Context    = context,
                LogHash    = logHash
            };

            lock (_bufferLock)
            {
                _buffer.Add(entry);

                if (_buffer.Count >= AuditConstants.k_RingBufferCapacity)
                    FlushLocked();
            }

            // Re-emit with correlation hash so developers can search the DB.
            // Guard against infinite recursion: the re-emitted message starts with "  -> ",
            // which won't match a recognised tag.
            UnityEngine.Debug.Log($"  -> {logMessage} [db:{logHash}]");
        }

        private void Flush()
        {
            lock (_bufferLock)
            {
                FlushLocked();
            }
        }

        /// <summary>Writes all buffered entries to the database. Must be called under _bufferLock.</summary>
        private void FlushLocked()
        {
            if (_buffer.Count == 0)
                return;

            _lastFlushTime = Time.realtimeSinceStartup;
            DebugLogWriter.WriteEntries(_buffer);
            _buffer.Clear();
        }
    }
}
#endif
