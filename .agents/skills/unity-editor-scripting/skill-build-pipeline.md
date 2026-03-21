# Build Pipeline

> Part of the `unity-editor-scripting` skill. See [SKILL.md](SKILL.md) for the overview.

## Build Pipeline

```csharp
public static class BuildScript
{
    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray(),
            locationPathName = "Builds/Windows/Game.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"Build succeeded: {report.summary.totalSize / (1024 * 1024)} MB");
        else
            Debug.LogError($"Build failed: {report.summary.totalErrors} errors");
    }
}

// Pre-build hook
public class BuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        // Set version, validate settings, etc.
        PlayerSettings.bundleVersion = "1.0.0";
        Debug.Log($"Building for {report.summary.platform}");
    }
}
```

## Editor Coroutines

Unity editor does not have a game loop, so standard coroutines do not work. Use EditorApplication.update or the EditorCoroutineUtility package:

```csharp
// Manual approach using EditorApplication.update
public static class EditorTask
{
    public static void DelayedAction(float delaySeconds, System.Action action)
    {
        double targetTime = EditorApplication.timeSinceStartup + delaySeconds;

        void Check()
        {
            if (EditorApplication.timeSinceStartup >= targetTime)
            {
                EditorApplication.update -= Check;
                action();
            }
        }

        EditorApplication.update += Check;
    }
}

// Usage
EditorTask.DelayedAction(2f, () => Debug.Log("2 seconds later"));
```

With the `com.unity.editorcoroutines` package:

```csharp
using Unity.EditorCoroutines.Editor;

public class MyEditorWindow : EditorWindow
{
    private void OnGUI()
    {
        if (GUILayout.Button("Process"))
        {
            EditorCoroutineUtility.StartCoroutine(ProcessAsync(), this);
        }
    }

    private IEnumerator ProcessAsync()
    {
        for (int i = 0; i < 100; i++)
        {
            EditorUtility.DisplayProgressBar("Processing", $"Step {i}/100", i / 100f);
            // Do work...
            yield return null; // yield one editor frame
        }
        EditorUtility.ClearProgressBar();
    }
}
```

## ScriptableWizard

Quick multi-field dialog for one-shot operations:

```csharp
public class CreateEnemyWizard : ScriptableWizard
{
    public string enemyName = "New Enemy";
    public float health = 100;
    public float speed = 3f;
    public GameObject meshPrefab;

    [MenuItem("Tools/Create Enemy Wizard")]
    static void Open()
    {
        DisplayWizard<CreateEnemyWizard>("Create Enemy", "Create", "Apply Defaults");
    }

    // "Create" button clicked
    private void OnWizardCreate()
    {
        var go = new GameObject(enemyName);
        var enemy = go.AddComponent<Enemy>();
        enemy.maxHealth = health;
        enemy.moveSpeed = speed;
        Undo.RegisterCreatedObjectUndo(go, "Create Enemy");
    }

    // "Apply Defaults" button clicked
    private void OnWizardOtherButton()
    {
        health = 100;
        speed = 3f;
        enemyName = "New Enemy";
    }

    // Validation
    private void OnWizardUpdate()
    {
        helpString = "Configure the new enemy properties.";
        errorString = string.IsNullOrEmpty(enemyName) ? "Name cannot be empty" : "";
        isValid = !string.IsNullOrEmpty(enemyName) && meshPrefab != null;
    }
}
```
