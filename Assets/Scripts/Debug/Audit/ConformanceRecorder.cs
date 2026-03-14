#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Data.Sqlite;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Records physics conformance check results to the audit database.
    /// Call <see cref="BeginRun"/> before recording, <see cref="Record"/> for each check,
    /// and <see cref="EndRun"/> to flush and summarise.
    /// </summary>
    public static class ConformanceRecorder
    {
        // ---- Nested Types ----

        /// <summary>Summary of pass rates grouped by category from a single run.</summary>
        public struct CategorySummary
        {
            public string Category;
            public int Passed;
            public int Total;
            public string WorstTier;
        }


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
            double tolerance = ComputeTolerance(expected, actual);
            string tier = AuditConstants.ComputeTier(tolerance);
            bool passed = tolerance < AuditConstants.k_TierPoor;

            lock (_lock)
            {
                _pendingRecords.Add(new PendingRecord
                {
                    Category = category,
                    CheckId = checkId,
                    CheckName = checkName,
                    Expected = expected,
                    Actual = actual,
                    Tolerance = tolerance,
                    Tier = tier,
                    Passed = passed,
                    Metadata = metadata
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

        /// <summary>
        /// Returns category-level pass rates from the most recent conformance run.
        /// </summary>
        public static List<CategorySummary> GetLatestRunSummary()
        {
            var summaries = new List<CategorySummary>();

            const string sql = @"
SELECT category,
       SUM(passed) as passed_count,
       COUNT(*) as total_count,
       MIN(CASE
           WHEN tier = 'Broken' THEN 1
           WHEN tier = 'Poor' THEN 2
           WHEN tier = 'Noticeable' THEN 3
           WHEN tier = 'Good' THEN 4
           WHEN tier = 'Excellent' THEN 5
           ELSE 6 END) as worst_tier_rank,
       tier
FROM conformance_runs
WHERE run_id = (SELECT run_id FROM conformance_runs ORDER BY id DESC LIMIT 1)
GROUP BY category
ORDER BY category";

            using (var reader = AuditDb.ExecuteReader(sql))
            {
                while (reader.Read())
                {
                    summaries.Add(new CategorySummary
                    {
                        Category = reader.GetString(0),
                        Passed = reader.GetInt32(1),
                        Total = reader.GetInt32(2),
                        WorstTier = reader.GetString(4)
                    });
                }
            }

            return summaries;
        }


        // ---- Private Helpers ----

        /// <summary>
        /// Computes tolerance as |actual - expected| / |expected|.
        /// Returns 0 when expected is 0 and actual is also 0; returns 1.0 when expected is 0 but actual is not.
        /// </summary>
        private static double ComputeTolerance(double expected, double actual)
        {
            if (Math.Abs(expected) < double.Epsilon)
                return Math.Abs(actual) < double.Epsilon ? 0.0 : 1.0;

            return Math.Abs(actual - expected) / Math.Abs(expected);
        }

        private static void FlushToDatabase()
        {
            var conn = AuditDb.GetConnection();
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            const string insertSql = @"
INSERT INTO conformance_runs
    (run_id, timestamp, git_sha, branch, category, check_id, check_name,
     expected, actual, tolerance, tier, passed, metadata)
VALUES
    (@run_id, @timestamp, @git_sha, @branch, @category, @check_id, @check_name,
     @expected, @actual, @tolerance, @tier, @passed, @metadata)";

            using (var transaction = conn.BeginTransaction())
            {
                foreach (var record in _pendingRecords)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = insertSql;
                        cmd.Parameters.AddWithValue("@run_id", _runId);
                        cmd.Parameters.AddWithValue("@timestamp", timestamp);
                        cmd.Parameters.AddWithValue("@git_sha", _gitSha ?? "unknown");
                        cmd.Parameters.AddWithValue("@branch", _branch ?? "unknown");
                        cmd.Parameters.AddWithValue("@category", record.Category);
                        cmd.Parameters.AddWithValue("@check_id", record.CheckId);
                        cmd.Parameters.AddWithValue("@check_name", record.CheckName);
                        cmd.Parameters.AddWithValue("@expected", record.Expected);
                        cmd.Parameters.AddWithValue("@actual", record.Actual);
                        cmd.Parameters.AddWithValue("@tolerance", record.Tolerance);
                        cmd.Parameters.AddWithValue("@tier", record.Tier);
                        cmd.Parameters.AddWithValue("@passed", record.Passed ? 1 : 0);
                        cmd.Parameters.AddWithValue("@metadata",
                            (object)record.Metadata ?? DBNull.Value);
                    }
                }

                transaction.Commit();
            }
        }

        private static void LogSummary()
        {
            int passed = 0;
            int total = _pendingRecords.Count;
            int worstRank = 5;
            string worstTier = AuditConstants.k_Excellent;

            foreach (var record in _pendingRecords)
            {
                if (record.Passed)
                    passed++;

                int rank = TierToRank(record.Tier);
                if (rank < worstRank)
                {
                    worstRank = rank;
                    worstTier = record.Tier;
                }
            }

            UnityEngine.Debug.Log(
                $"[conformance] Run {_runId[..AuditConstants.k_HashLength]}: " +
                $"{passed}/{total} passed, worst tier: {worstTier}");
        }

        private static int TierToRank(string tier)
        {
            if (tier == AuditConstants.k_Broken) return 1;
            if (tier == AuditConstants.k_Poor) return 2;
            if (tier == AuditConstants.k_Noticeable) return 3;
            if (tier == AuditConstants.k_Good) return 4;
            return 5;
        }

        private static string CaptureGitOutput(string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
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
