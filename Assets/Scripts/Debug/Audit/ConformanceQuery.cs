#if UNITY_EDITOR || DEBUG
using System.Collections.Generic;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Read-side query helpers for the conformance_runs table.
    /// Extracted from <see cref="ConformanceRecorder"/> to separate write (record) from
    /// read (query/aggregate) concerns.
    /// </summary>
    public static class ConformanceQuery
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


        // ---- Public API ----

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
                        Passed   = reader.GetInt32(1),
                        Total    = reader.GetInt32(2),
                        WorstTier = reader.GetString(4)
                    });
                }
            }

            return summaries;
        }

        /// <summary>
        /// Maps a tier name to an integer rank (1 = worst, 5 = best) for comparison.
        /// </summary>
        public static int TierToRank(string tier)
        {
            if (tier == AuditConstants.k_Broken)    return 1;
            if (tier == AuditConstants.k_Poor)       return 2;
            if (tier == AuditConstants.k_Noticeable) return 3;
            if (tier == AuditConstants.k_Good)       return 4;
            return 5;
        }

        /// <summary>
        /// Computes tolerance as |actual - expected| / |expected|.
        /// Returns 0 when both values are zero; returns 1.0 when expected is 0 but actual is not.
        /// </summary>
        public static double ComputeTolerance(double expected, double actual)
        {
            if (System.Math.Abs(expected) < double.Epsilon)
                return System.Math.Abs(actual) < double.Epsilon ? 0.0 : 1.0;

            return System.Math.Abs(actual - expected) / System.Math.Abs(expected);
        }
    }
}
#endif
