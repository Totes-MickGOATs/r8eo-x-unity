# Build Pipeline Basics

> Part of the `unity-project-foundations` skill. See [SKILL.md](SKILL.md) for the overview.

## Build Pipeline Basics

```csharp
// Editor script for automated builds
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildScript
{
    [MenuItem("Build/Windows")]
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = GetBuildScenes(),
            locationPathName = "Builds/Windows/Game.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception($"Build failed: {report.summary.totalErrors} errors");
        }
    }

    private static string[] GetBuildScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }
}
#endif
```

