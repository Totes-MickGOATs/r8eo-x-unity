using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace R8EOX.Editor
{
    /// <summary>
    /// Advisory asset/scene lint runner. Invoked via Unity -batchmode.
    /// Exit 0 always (findings are advisory). Nonzero only on tool failure.
    /// Report written to Logs/asset_lint_report.json.
    /// </summary>
    public static class AssetLintRunner
    {
        // CLI entrypoint: Unity -executeMethod R8EOX.Editor.AssetLintRunner.RunFromCommandLine
        public static void RunFromCommandLine()
        {
            var report = RunAllChecks();
            WriteReport(report);

            int total = report.Findings.Count;
            Debug.Log($"[AssetLintRunner] {total} finding(s). Report: Logs/asset_lint_report.json");

            // Exit 0 always — findings are advisory
            EditorApplication.Exit(0);
        }

        // Testable: returns findings without writing report or calling Exit
        public static LintReport RunAllChecks()
        {
            var report = new LintReport();
            report.Findings.AddRange(CheckMissingScripts());
            report.Findings.AddRange(CheckBuildSettingsScenes());
            report.Findings.AddRange(CheckLayerTagDrift());
            return report;
        }

        public static List<LintFinding> CheckMissingScripts()
        {
            var findings = new List<LintFinding>();
            // Find all prefab GUIDs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // Check all components on all children
                foreach (var obj in prefab.GetComponentsInChildren<Transform>(true))
                {
                    var components = obj.gameObject.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        if (comp == null) // Missing script
                        {
                            findings.Add(new LintFinding
                            {
                                Rule = "MISSING_SCRIPT",
                                Path = path,
                                Message = $"Missing script on GameObject '{obj.name}' in prefab",
                                Severity = "warning"
                            });
                        }
                    }
                }
            }
            return findings;
        }

        public static List<LintFinding> CheckBuildSettingsScenes()
        {
            var findings = new List<LintFinding>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!File.Exists(scene.path))
                {
                    findings.Add(new LintFinding
                    {
                        Rule = "MISSING_BUILD_SCENE",
                        Path = scene.path,
                        Message = $"Build Settings references scene that does not exist on disk",
                        Severity = "warning"
                    });
                }
            }
            return findings;
        }

        public static List<LintFinding> CheckLayerTagDrift()
        {
            var findings = new List<LintFinding>();
            // Read TagManager.asset via AssetDatabase
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            if (tagManager == null) return findings;

            // Check layers for empty names in important slots (8-31)
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers != null)
            {
                for (int i = 8; i < Mathf.Min(layers.arraySize, 32); i++)
                {
                    string layerName = layers.GetArrayElementAtIndex(i).stringValue;
                    if (!string.IsNullOrEmpty(layerName) && layerName.Length > 0)
                    {
                        // Layer exists — just report it for visibility (not a violation)
                        findings.Add(new LintFinding
                        {
                            Rule = "LAYER_AUDIT",
                            Path = "ProjectSettings/TagManager.asset",
                            Message = $"Layer {i}: '{layerName}'",
                            Severity = "info"
                        });
                    }
                }
            }
            return findings;
        }

        private static void WriteReport(LintReport report)
        {
            Directory.CreateDirectory("Logs");
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("{");
            lines.AppendLine($"  \"timestamp\": \"{System.DateTime.UtcNow:O}\",");
            lines.AppendLine($"  \"total\": {report.Findings.Count},");
            lines.AppendLine("  \"findings\": [");
            for (int i = 0; i < report.Findings.Count; i++)
            {
                var f = report.Findings[i];
                string comma = i < report.Findings.Count - 1 ? "," : "";
                lines.AppendLine($"    {{\"rule\":\"{f.Rule}\",\"path\":\"{f.Path}\",\"message\":\"{EscapeJson(f.Message)}\",\"severity\":\"{f.Severity}\"}}{comma}");
            }
            lines.AppendLine("  ]");
            lines.AppendLine("}");
            File.WriteAllText("Logs/asset_lint_report.json", lines.ToString());
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }

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
