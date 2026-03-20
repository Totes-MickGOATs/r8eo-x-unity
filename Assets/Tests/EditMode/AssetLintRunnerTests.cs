using System.Collections.Generic;
using NUnit.Framework;
using R8EOX.Editor;

namespace R8EOX.Tests.EditMode
{
    public class AssetLintRunnerTests
    {
        [Test]
        public void CheckBuildSettingsScenes_ReturnsListType()
        {
            // Smoke test — method returns a List<LintFinding>
            var findings = AssetLintRunner.CheckBuildSettingsScenes();
            Assert.IsNotNull(findings);
            Assert.IsInstanceOf<List<LintFinding>>(findings);
        }

        [Test]
        public void CheckMissingScripts_ReturnsListType()
        {
            var findings = AssetLintRunner.CheckMissingScripts();
            Assert.IsNotNull(findings);
        }

        [Test]
        public void LintFinding_HasRequiredProperties()
        {
            var f = new LintFinding
            {
                Rule = "TEST",
                Path = "foo/bar.prefab",
                Message = "test message",
                Severity = "warning"
            };
            Assert.AreEqual("TEST", f.Rule);
            Assert.AreEqual("foo/bar.prefab", f.Path);
            Assert.AreEqual("test message", f.Message);
            Assert.AreEqual("warning", f.Severity);
        }

        [Test]
        public void LintReport_StartsEmpty()
        {
            var report = new LintReport();
            Assert.AreEqual(0, report.Findings.Count);
        }

        [Test]
        public void RunAllChecks_ReturnsReport()
        {
            // In editor context, RunAllChecks should not throw
            Assert.DoesNotThrow(() => AssetLintRunner.RunAllChecks());
        }
    }
}
