#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
                _gitSha = CaptureGitOutput("rev-parse --short HEAD");
                _branch = CaptureGitOutput("branch --show-current");
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
            var conn = AuditDb.GetConnection();
            string ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            const string sql = @"
INSERT INTO conformance_runs
    (run_id, timestamp, git_sha, branch, category, check_id, check_name,
     expected, actual, tolerance, tier, passed, metadata)
VALUES
    (@run_id, @timestamp, @git_sha, @branch, @category, @check_id, @check_name,
     @expected, @actual, @tolerance, @tier, @passed, @metadata)";

            using (var transaction = conn.BeginTransaction())
            {
                foreach (var r in _pendingRecords)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = sql;
                        cmd.Parameters.AddWithValue("@run_id",     _runId);
                        cmd.Parameters.AddWithValue("@timestamp",  ts);
                        cmd.Parameters.AddWithValue("@git_sha",    _gitSha ?? "unknown");
                        cmd.Parameters.AddWithValue("@branch",     _branch ?? "unknown");
                        cmd.Parameters.AddWithValue("@category",   r.Category);
                        cmd.Parameters.AddWithValue("@check_id",   r.CheckId);
                        cmd.Parameters.AddWithValue("@check_name", r.CheckName);
                        cmd.Parameters.AddWithValue("@expected",   r.Expected);
                        cmd.Parameters.AddWithValue("@actual",     r.Actual);
                        cmd.Parameters.AddWithValue("@tolerance",  r.Tolerance);
                        cmd.Parameters.AddWithValue("@tier",       r.Tier);
                        cmd.Parameters.AddWithValue("@passed",     r.Passed ? 1 : 0);
                        cmd.Parameters.AddWithValue("@metadata",   (object)r.Metadata ?? DBNull.Value);
                    }
                }
                transaction.Commit();
            }
        }

        private static void LogSummary()
        {
            int passed = 0;
            int total  = _pendingRecords.Count;
            int worstRank = 5;
            string worstTier = AuditConstants.k_Excellent;

            foreach (var record in _pendingRecords)
            {
                if (record.Passed)
                    passed++;

                int rank = ConformanceQuery.TierToRank(record.Tier);
                if (rank < worstRank)
                {
                    worstRank = rank;
                    worstTier = record.Tier;
                }
            }

            UnityEngine.Debug.Log($"[conformance] Run {_runId[..AuditConstants.k_HashLength]}: {passed}/{total} passed, worst tier: {worstTier}");
        }

        private static string CaptureGitOutput(string arguments)
        {
            try
            {
                var process = new Process { StartInfo = new ProcessStartInfo {
                    FileName = "git", Arguments = arguments,
                    RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
                } };
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return string.IsNullOrEmpty(output) ? "unknown" : output;
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
#endif
