#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Writes pending conformance records to the SQLite database and captures
    /// git context. Extracted from <see cref="ConformanceRecorder"/> to isolate
    /// the persistence and process-launch concerns.
    /// </summary>
    public static class ConformanceDbWriter
    {
        // ---- SQL ----

        private const string k_InsertSql = @"
INSERT INTO conformance_runs
    (run_id, timestamp, git_sha, branch, category, check_id, check_name,
     expected, actual, tolerance, tier, passed, metadata)
VALUES
    (@run_id, @timestamp, @git_sha, @branch, @category, @check_id, @check_name,
     @expected, @actual, @tolerance, @tier, @passed, @metadata)";

        // ---- Nested Type ----

        public struct Row
        {
            public string Category;
            public string CheckId;
            public string CheckName;
            public double Expected;
            public double Actual;
            public double Tolerance;
            public string Tier;
            public bool   Passed;
            public string Metadata;
        }

        // ---- Public API ----

        /// <summary>Writes all rows to the database in a single transaction.</summary>
        public static void Flush(string runId, string gitSha, string branch, List<Row> rows)
        {
            var conn = AuditDb.GetConnection();
            string ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            using (var transaction = conn.BeginTransaction())
            {
                foreach (var r in rows)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = k_InsertSql;
                        cmd.Parameters.AddWithValue("@run_id",     runId);
                        cmd.Parameters.AddWithValue("@timestamp",  ts);
                        cmd.Parameters.AddWithValue("@git_sha",    gitSha ?? "unknown");
                        cmd.Parameters.AddWithValue("@branch",     branch ?? "unknown");
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

        /// <summary>Runs git with the given arguments and returns trimmed stdout, or "unknown".</summary>
        public static string CaptureGitOutput(string arguments)
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
