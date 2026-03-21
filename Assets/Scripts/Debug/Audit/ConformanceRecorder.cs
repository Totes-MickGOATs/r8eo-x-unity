#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Records physics conformance check results to the audit database.
    /// Call <see cref="BeginRun"/> before recording, <see cref="Record"/> for each check,
    /// and <see cref="EndRun"/> to flush and summarise.
    /// Query helpers and <see cref="ConformanceQuery.CategorySummary"/> live in
    /// <see cref="ConformanceQuery"/>.
    /// </summary>
    public static class ConformanceRecorder
    {
        // ---- Private Fields ----

        private static string _runId;
        private static string _gitSha;
        private static string _branch;
        private static readonly List<PendingRecord> _pendingRecords = new List<PendingRecord>();
        private static readonly object _lock = new object();

        // ---- Private Structs ----

        private struct PendingRecord
        {
            public string Category;
            public string CheckId;
            public string CheckName;
            public double Expected;
            public double Actual;
            public double Tolerance;
            public string Tier;
            public bool Passed;
            public string Metadata;
        }

        // ---- Public API ----

        /// <summary>
        /// Begins a new conformance run. Generates a GUID run ID and captures git state once.
        /// </summary>
        public static void BeginRun()
        {
            lock (_lock)
            {
                _runId = Guid.NewGuid().ToString("N");
                _gitSha = ConformanceDbWriter.CaptureGitOutput("rev-parse --short HEAD");
                _branch = ConformanceDbWriter.CaptureGitOutput("branch --show-current");
                _pendingRecords.Clear();
            }
        }

        /// <summary>
        /// Records a single conformance check result. Batched in memory until <see cref="EndRun"/>.
        /// </summary>
        /// <param name="category">Check category (e.g. "A", "B").</param>
        /// <param name="checkId">Unique check identifier (e.g. "A1", "B12").</param>
        /// <param name="checkName">Human-readable check name.</param>
        /// <param name="expected">Analytically predicted value.</param>
        /// <param name="actual">Measured value from simulation.</param>
        /// <param name="metadata">Optional JSON metadata string.</param>
        public static void Record(
            string category,
            string checkId,
            string checkName,
            double expected,
            double actual,
            string metadata = null)
        {
            double tolerance = ConformanceQuery.ComputeTolerance(expected, actual);
            string tier = AuditConstants.ComputeTier(tolerance);
            bool passed = tolerance < AuditConstants.k_TierPoor;

            lock (_lock)
            {
                _pendingRecords.Add(new PendingRecord
                {
                    Category  = category,
                    CheckId   = checkId,
                    CheckName = checkName,
                    Expected  = expected,
                    Actual    = actual,
                    Tolerance = tolerance,
                    Tier      = tier,
                    Passed    = passed,
                    Metadata  = metadata
                });
            }
        }

        /// <summary>
        /// Flushes all pending records to the database in a single transaction and logs a summary.
        /// </summary>
        public static void EndRun()
        {
            lock (_lock)
            {
                if (_pendingRecords.Count == 0)
                    return;

                FlushToDatabase();
                LogSummary();
                _pendingRecords.Clear();
            }
        }


        // ---- Private Helpers ----

        private static void FlushToDatabase()
        {
            var rows = new List<ConformanceDbWriter.Row>(_pendingRecords.Count);
            foreach (var r in _pendingRecords)
            {
                rows.Add(new ConformanceDbWriter.Row
                {
                    Category  = r.Category,
                    CheckId   = r.CheckId,
                    CheckName = r.CheckName,
                    Expected  = r.Expected,
                    Actual    = r.Actual,
                    Tolerance = r.Tolerance,
                    Tier      = r.Tier,
                    Passed    = r.Passed,
                    Metadata  = r.Metadata
                });
            }
            ConformanceDbWriter.Flush(_runId, _gitSha, _branch, rows);
        }

        private static void LogSummary()
        {
            var snapshots = new List<ConformanceSummaryLogger.RecordSnapshot>(_pendingRecords.Count);
            foreach (var r in _pendingRecords)
                snapshots.Add(new ConformanceSummaryLogger.RecordSnapshot { Passed = r.Passed, Tier = r.Tier });

            ConformanceSummaryLogger.LogSummary(_runId, snapshots);
        }
    }
}
#endif
