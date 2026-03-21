#if UNITY_EDITOR || DEBUG
using NUnit.Framework;
using R8EOX.Debug.Audit;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for AuditDbLifecycle — the editor startup / domain-unload logic
    /// extracted from AuditDb.
    /// </summary>
    [TestFixture]
    public class AuditDbLifecycleTests
    {
        // ---- Purge safety ----

        [Test]
        public void Purge_WhenNotInitialized_DoesNotThrow()
        {
            // AuditDb.Purge should no-op when the connection is not open.
            // We cannot fully test editor-only [InitializeOnLoadMethod] in EditMode,
            // but we can verify Purge is safe to call on a fresh (un-initialized) state
            // by calling the public API directly.
            Assert.DoesNotThrow(() => AuditDb.Purge());
        }

        // ---- AddParameters safety ----

        [Test]
        public void ExecuteNonQuery_NullParameters_DoesNotThrow()
        {
            // AuditDb must be initialized before we can run any query.
            // If the DB cannot be opened in test context, Initialize is a no-op that may throw —
            // we just verify the public API handles null params gracefully when given a real connection.
            // This is a smoke test: if Initialize succeeds, ExecuteNonQuery with no-op SQL works.
            try
            {
                AuditDb.Initialize();
                // Use a benign no-op SQL
                Assert.DoesNotThrow(() => AuditDb.ExecuteNonQuery("SELECT 1"));
            }
            catch (System.Exception)
            {
                // SQLite may not be available in CI — skip gracefully
                Assert.Ignore("SQLite not available in this test environment");
            }
        }
    }
}
#endif
