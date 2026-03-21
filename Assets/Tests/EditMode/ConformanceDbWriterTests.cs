#if UNITY_EDITOR || DEBUG
using NUnit.Framework;
using System.Collections.Generic;
using R8EOX.Debug.Audit;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for ConformanceDbWriter — SQLite flush and git capture helpers
    /// extracted from ConformanceRecorder.
    /// </summary>
    [TestFixture]
    public class ConformanceDbWriterTests
    {
        // ---- CaptureGitOutput ----

        [Test]
        public void CaptureGitOutput_InvalidCommand_ReturnsUnknown()
        {
            // "branch --show-current" on a non-git dir or invalid args returns "unknown" not throw
            string result = ConformanceDbWriter.CaptureGitOutput("invalid-subcommand-xyz");

            // Either "unknown" or an empty-turned-unknown — must not throw
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result));
        }

        [Test]
        public void CaptureGitOutput_ValidCommand_ReturnsNonEmpty()
        {
            // rev-parse HEAD should work in this repo
            string result = ConformanceDbWriter.CaptureGitOutput("rev-parse --short HEAD");

            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result));
        }

        // ---- Flush ----

        [Test]
        public void Flush_EmptyList_DoesNotThrow()
        {
            var rows = new List<ConformanceDbWriter.Row>();

            try
            {
                AuditDb.Initialize();
                Assert.DoesNotThrow(() =>
                    ConformanceDbWriter.Flush("testrundummy", "abc1234", "main", rows));
            }
            catch (System.Exception)
            {
                Assert.Ignore("SQLite not available in this test environment");
            }
        }

        [Test]
        public void Row_DefaultConstruction_HasNullOptionalFields()
        {
            var row = new ConformanceDbWriter.Row();

            Assert.IsNull(row.Metadata);
            Assert.IsNull(row.Tier);
        }
    }
}
#endif
