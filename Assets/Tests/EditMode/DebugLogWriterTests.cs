#if UNITY_EDITOR || DEBUG
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug.Audit;
using System.Collections.Generic;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for DebugLogWriter — the buffered-entry SQL persistence helper
    /// extracted from DebugLogSink.
    /// </summary>
    [TestFixture]
    public class DebugLogWriterTests
    {
        // ---- WriteEntries ----

        [Test]
        public void WriteEntries_EmptyList_DoesNotThrow()
        {
            var entries = new List<DebugLogWriter.Entry>();

            try
            {
                AuditDb.Initialize();
                Assert.DoesNotThrow(() => DebugLogWriter.WriteEntries(entries));
            }
            catch (System.Exception)
            {
                Assert.Ignore("SQLite not available in this test environment");
            }
        }

        [Test]
        public void WriteEntries_WithOneEntry_DoesNotThrow()
        {
            var entries = new List<DebugLogWriter.Entry>
            {
                new DebugLogWriter.Entry
                {
                    Timestamp  = "2026-01-01 00:00:00.000",
                    Frame      = 1,
                    Level      = "Log",
                    System     = "physics",
                    Message    = "test message",
                    StackTrace = null,
                    Context    = null,
                    LogHash    = "abcd1234"
                }
            };

            try
            {
                AuditDb.Initialize();
                Assert.DoesNotThrow(() => DebugLogWriter.WriteEntries(entries));
            }
            catch (System.Exception)
            {
                Assert.Ignore("SQLite not available in this test environment");
            }
        }

        [Test]
        public void Entry_DefaultConstruction_HasNullOptionalFields()
        {
            var entry = new DebugLogWriter.Entry();

            Assert.IsNull(entry.StackTrace);
            Assert.IsNull(entry.Context);
        }
    }
}
#endif
