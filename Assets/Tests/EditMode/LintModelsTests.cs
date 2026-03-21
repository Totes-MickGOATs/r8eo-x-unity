using NUnit.Framework;
using R8EOX.Editor;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for LintReport and LintFinding data models extracted from AssetLintRunner.
    /// These are plain data classes — no Unity lifecycle needed.
    /// </summary>
    public class LintModelsTests
    {
        [Test]
        public void LintFinding_DefaultValues_AreNull()
        {
            var f = new LintFinding();
            Assert.IsNull(f.Rule);
            Assert.IsNull(f.Path);
            Assert.IsNull(f.Message);
            Assert.IsNull(f.Severity);
        }

        [Test]
        public void LintFinding_SetProperties_RoundTrip()
        {
            var f = new LintFinding
            {
                Rule = "MY_RULE",
                Path = "Assets/Foo.prefab",
                Message = "a message",
                Severity = "info"
            };
            Assert.AreEqual("MY_RULE", f.Rule);
            Assert.AreEqual("Assets/Foo.prefab", f.Path);
            Assert.AreEqual("a message", f.Message);
            Assert.AreEqual("info", f.Severity);
        }

        [Test]
        public void LintReport_Findings_InitiallyEmpty()
        {
            var r = new LintReport();
            Assert.IsNotNull(r.Findings);
            Assert.AreEqual(0, r.Findings.Count);
        }

        [Test]
        public void LintReport_AddFinding_CountIncreases()
        {
            var r = new LintReport();
            r.Findings.Add(new LintFinding { Rule = "R1", Path = "p", Message = "m", Severity = "warning" });
            r.Findings.Add(new LintFinding { Rule = "R2", Path = "p", Message = "m", Severity = "info" });
            Assert.AreEqual(2, r.Findings.Count);
        }

        [Test]
        public void LintReport_CalledTwice_EachInstanceIndependent()
        {
            // Idempotency: two independent LintReport instances don't share state.
            var r1 = new LintReport();
            var r2 = new LintReport();
            r1.Findings.Add(new LintFinding { Rule = "X", Path = "p", Message = "m", Severity = "warning" });
            Assert.AreEqual(1, r1.Findings.Count);
            Assert.AreEqual(0, r2.Findings.Count);
        }
    }
}
