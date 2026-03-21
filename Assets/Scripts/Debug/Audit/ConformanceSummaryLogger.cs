#if UNITY_EDITOR || DEBUG
using System.Collections.Generic;
using UnityEngine;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Formats and emits the per-run conformance summary log message.
    /// Extracted from <see cref="ConformanceRecorder"/> so it can be tested
    /// in isolation without touching the database.
    /// </summary>
    public static class ConformanceSummaryLogger
    {
        // ---- Nested Types ----

        /// <summary>Immutable snapshot of a single pending record used for summary computation.</summary>
        public struct RecordSnapshot
        {
            public bool   Passed;
            public string Tier;
        }

        // ---- Public API ----

        /// <summary>
        /// Logs a conformance run summary line to the Unity console.
        /// </summary>
        /// <param name="runId">The run GUID (first 8 chars are shown).</param>
        /// <param name="records">All pending records for this run.</param>
        public static void LogSummary(string runId, List<RecordSnapshot> records)
        {
            if (records == null || records.Count == 0)
                return;

            int passed    = 0;
            int total     = records.Count;
            int worstRank = 5;
            string worstTier = AuditConstants.k_Excellent;

            foreach (var record in records)
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

            string shortId = runId.Length >= AuditConstants.k_HashLength
                ? runId[..AuditConstants.k_HashLength]
                : runId;

            Debug.Log(
                $"[conformance] Run {shortId}: {passed}/{total} passed, worst tier: {worstTier}");
        }
    }
}
#endif
