using System.Collections.Generic;

namespace R8EOX.Editor
{
    /// <summary>
    /// Data model types for the asset lint system.
    /// Extracted from AssetLintRunner.cs to keep each file under 150 lines.
    /// </summary>
    public class LintReport
    {
        public List<LintFinding> Findings { get; } = new List<LintFinding>();
    }

    public class LintFinding
    {
        public string Rule { get; set; }
        public string Path { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; } // "warning" | "info"
    }
}
