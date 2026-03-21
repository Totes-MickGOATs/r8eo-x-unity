#if UNITY_EDITOR || DEBUG
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug.Audit;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for ConformanceSummaryLogger — run-summary logging extracted from ConformanceRecorder.
    /// </summary>
    [TestFixture]
    public class ConformanceSummaryLoggerTests
    {
        // ---- LogSummary ----

        [Test]
        public void LogSummary_NoPendingRecords_DoesNotThrow()
        {
            // Empty pending list — should not throw or log
            Assert.DoesNotThrow(() =>
                ConformanceSummaryLogger.LogSummary("abc12345", new System.Collections.Generic.List<ConformanceSummaryLogger.RecordSnapshot>()));
        }

        [Test]
        public void LogSummary_AllPassed_LogsPassCount()
        {
            var runId = "feedbeef";
            var records = new System.Collections.Generic.List<ConformanceSummaryLogger.RecordSnapshot>
            {
                new ConformanceSummaryLogger.RecordSnapshot { Passed = true,  Tier = AuditConstants.k_Excellent },
                new ConformanceSummaryLogger.RecordSnapshot { Passed = true,  Tier = AuditConstants.k_Good },
            };

            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"\[conformance\].*2/2 passed"));

            ConformanceSummaryLogger.LogSummary(runId, records);
        }

        [Test]
        public void LogSummary_SomeFailed_LogsPartialCount()
        {
            var runId = "deadc0de";
            var records = new System.Collections.Generic.List<ConformanceSummaryLogger.RecordSnapshot>
            {
                new ConformanceSummaryLogger.RecordSnapshot { Passed = true,  Tier = AuditConstants.k_Good },
                new ConformanceSummaryLogger.RecordSnapshot { Passed = false, Tier = AuditConstants.k_Broken },
            };

            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"\[conformance\].*1/2 passed"));

            ConformanceSummaryLogger.LogSummary(runId, records);
        }

        [Test]
        public void LogSummary_WorstTierReported_InLogMessage()
        {
            var runId = "cafebabe";
            var records = new System.Collections.Generic.List<ConformanceSummaryLogger.RecordSnapshot>
            {
                new ConformanceSummaryLogger.RecordSnapshot { Passed = true, Tier = AuditConstants.k_Excellent },
                new ConformanceSummaryLogger.RecordSnapshot { Passed = false, Tier = AuditConstants.k_Poor },
            };

            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"worst tier: Poor"));

            ConformanceSummaryLogger.LogSummary(runId, records);
        }
    }
}
#endif
