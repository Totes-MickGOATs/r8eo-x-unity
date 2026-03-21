# Custom Inspector

> Part of the `unity-editor-scripting` skill. See [SKILL.md](SKILL.md) for the overview.

## Custom Inspector

Override how a component appears in the Inspector:

```csharp
using UnityEngine;
using UnityEditor;

// The component
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int maxEnemies = 10;
    public float spawnRadius = 5f;
    public float spawnInterval = 2f;
    public bool useWaves;
    public int enemiesPerWave = 5;
    public float waveCooldown = 10f;
}

// The custom inspector (in Editor/ folder)
[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor
{
    private SerializedProperty _prefab;
    private SerializedProperty _maxEnemies;
    private SerializedProperty _spawnRadius;
    private SerializedProperty _spawnInterval;
    private SerializedProperty _useWaves;
    private SerializedProperty _enemiesPerWave;
    private SerializedProperty _waveCooldown;

    private void OnEnable()
    {
        _prefab = serializedObject.FindProperty("enemyPrefab");
        _maxEnemies = serializedObject.FindProperty("maxEnemies");
        _spawnRadius = serializedObject.FindProperty("spawnRadius");
        _spawnInterval = serializedObject.FindProperty("spawnInterval");
        _useWaves = serializedObject.FindProperty("useWaves");
        _enemiesPerWave = serializedObject.FindProperty("enemiesPerWave");
        _waveCooldown = serializedObject.FindProperty("waveCooldown");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_prefab);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Spawn Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_maxEnemies);
        EditorGUILayout.PropertyField(_spawnRadius);
        EditorGUILayout.PropertyField(_spawnInterval);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_useWaves);

        // Conditional display
        if (_useWaves.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_enemiesPerWave);
            EditorGUILayout.PropertyField(_waveCooldown);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Custom buttons
        if (GUILayout.Button("Spawn Test Enemy"))
        {
            var spawner = (EnemySpawner)target;
            if (spawner.enemyPrefab != null)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(spawner.enemyPrefab);
                enemy.transform.position = spawner.transform.position
                    + Random.insideUnitSphere * spawner.spawnRadius;
                Undo.RegisterCreatedObjectUndo(enemy, "Spawn Test Enemy");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
```

### Multi-Object Editing

Using SerializedProperty handles multi-object editing automatically. If using `target` directly, check:

```csharp
[CustomEditor(typeof(MyComponent))]
[CanEditMultipleObjects] // Required for multi-select
public class MyComponentEditor : Editor
{
    // serializedObject automatically handles multiple targets
    // Only use (MyComponent)target for single-object preview/visualization
}
```

## Property Drawers

Customize how a specific field type or attribute renders in any Inspector:

```csharp
// Custom attribute
public class MinMaxRangeAttribute : PropertyAttribute
{
    public float Min;
    public float Max;

    public MinMaxRangeAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}

// Usage on a field
public class EnemyConfig : MonoBehaviour
{
    [MinMaxRange(0f, 100f)]
    public Vector2 damageRange = new(10f, 30f); // x = min, y = max
}

// The drawer (in Editor/ folder)
[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class MinMaxRangeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var range = (MinMaxRangeAttribute)attribute;
        var value = property.vector2Value;

        float min = value.x;
        float max = value.y;

        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, label);

        float fieldWidth = 50f;
        var minRect = new Rect(position.x, position.y, fieldWidth, position.height);
        var sliderRect = new Rect(position.x + fieldWidth + 5, position.y,
            position.width - fieldWidth * 2 - 10, position.height);
        var maxRect = new Rect(position.xMax - fieldWidth, position.y,
            fieldWidth, position.height);

        min = EditorGUI.FloatField(minRect, min);
        EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, range.Min, range.Max);
        max = EditorGUI.FloatField(maxRect, max);

        property.vector2Value = new Vector2(min, max);

        EditorGUI.EndProperty();
    }
}
```

### Common Custom Attributes

```csharp
// ReadOnly — display but don't edit
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.PropertyField(position, property, label);
        EditorGUI.EndDisabledGroup();
    }
}

// Button — shows a button that calls a method
// (Requires reflection to invoke the method by name)

// ConditionalHide — show/hide based on another field's value
[CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
public class ConditionalHideDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (ConditionalHideAttribute)attribute;
        var condProp = property.serializedObject.FindProperty(attr.ConditionalField);

        if (condProp != null && !condProp.boolValue)
            return 0f; // hidden — zero height

        return EditorGUI.GetPropertyHeight(property, label);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (ConditionalHideAttribute)attribute;
        var condProp = property.serializedObject.FindProperty(attr.ConditionalField);

        if (condProp != null && !condProp.boolValue)
            return; // hidden

        EditorGUI.PropertyField(position, property, label, true);
    }
}
```

## Editor Windows

Custom dockable windows for tools:

```csharp
public class LevelDesignTool : EditorWindow
{
    private string _levelName = "New Level";
    private int _width = 10;
    private int _height = 10;
    private float _tileSize = 1f;
    private Vector2 _scrollPos;
    private bool[,] _grid;

    [MenuItem("Tools/Level Design Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<LevelDesignTool>("Level Designer");
        window.minSize = new Vector2(400, 300);
    }

    private void OnEnable()
    {
        _grid = new bool[_width, _height];
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);

        _levelName = EditorGUILayout.TextField("Level Name", _levelName);
        _width = EditorGUILayout.IntSlider("Width", _width, 1, 50);
        _height = EditorGUILayout.IntSlider("Height", _height, 1, 50);
        _tileSize = EditorGUILayout.FloatField("Tile Size", _tileSize);

        if (GUILayout.Button("Reset Grid"))
        {
            _grid = new bool[_width, _height];
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Editor", EditorStyles.boldLabel);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        for (int y = _height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < _width; x++)
            {
                if (x < _grid.GetLength(0) && y < _grid.GetLength(1))
                {
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = _grid[x, y] ? Color.green : Color.gray;
                    if (GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20)))
                    {
                        _grid[x, y] = !_grid[x, y];
                    }
                    GUI.backgroundColor = oldColor;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Level", GUILayout.Height(30)))
        {
            GenerateLevel();
        }
    }

    private void GenerateLevel()
    {
        var parent = new GameObject(_levelName);
        Undo.RegisterCreatedObjectUndo(parent, "Generate Level");

        for (int x = 0; x < _grid.GetLength(0); x++)
        {
            for (int y = 0; y < _grid.GetLength(1); y++)
            {
                if (_grid[x, y])
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.parent = parent.transform;
                    cube.transform.position = new Vector3(x * _tileSize, 0, y * _tileSize);
                    cube.transform.localScale = Vector3.one * _tileSize;
                    Undo.RegisterCreatedObjectUndo(cube, "Generate Level");
                }
            }
        }
    }
}
```

### UI Toolkit Editor Window

```csharp
public class MyToolWindow : EditorWindow
{
    [SerializeField] private VisualTreeAsset windowLayout;

    [MenuItem("Tools/My Tool (UI Toolkit)")]
    public static void ShowWindow() => GetWindow<MyToolWindow>("My Tool");

    public void CreateGUI()
    {
        // UI Toolkit approach — preferred for new tools
        if (windowLayout != null)
        {
            windowLayout.CloneTree(rootVisualElement);
        }
        else
        {
            // Programmatic fallback
            var label = new Label("My Tool");
            label.style.fontSize = 20;
            rootVisualElement.Add(label);

            var button = new Button(() => Debug.Log("Clicked!")) { text = "Do Thing" };
            rootVisualElement.Add(button);
        }
    }
}
```

## Scene View Tools — Handles

Draw interactive handles in the Scene view:

```csharp
[CustomEditor(typeof(PatrolRoute))]
public class PatrolRouteEditor : Editor
{
    private void OnSceneGUI()
    {
        var route = (PatrolRoute)target;

        for (int i = 0; i < route.waypoints.Count; i++)
        {
            EditorGUI.BeginChangeCheck();

            // Position handle
            Vector3 newPos = Handles.PositionHandle(route.waypoints[i], Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(route, "Move Waypoint");
                route.waypoints[i] = newPos;
            }

            // Label
            Handles.Label(route.waypoints[i] + Vector3.up * 0.5f, $"WP {i}");

            // Line to next waypoint
            if (i < route.waypoints.Count - 1)
            {
                Handles.color = Color.yellow;
                Handles.DrawLine(route.waypoints[i], route.waypoints[i + 1], 2f);
            }
        }

        // Disc handle for radius
        Handles.color = new Color(0, 1, 0, 0.2f);
        Handles.DrawSolidDisc(route.transform.position, Vector3.up, route.detectionRadius);
        Handles.color = Color.green;

        EditorGUI.BeginChangeCheck();
        float newRadius = Handles.RadiusHandle(Quaternion.identity,
            route.transform.position, route.detectionRadius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(route, "Change Detection Radius");
            route.detectionRadius = newRadius;
        }
    }
}
```

### SceneView Overlay (Global Scene Tool)

```csharp
[InitializeOnLoad]
public static class SceneOverlay
{
    static SceneOverlay()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();

        var rect = new Rect(10, 10, 200, 60);
        GUI.Box(rect, "Quick Tools");

        if (GUI.Button(new Rect(15, 30, 90, 25), "Snap All"))
            SnapSelectedToGround();
        if (GUI.Button(new Rect(110, 30, 90, 25), "Align"))
            AlignSelected();

        Handles.EndGUI();
    }

    private static void SnapSelectedToGround() { /* ... */ }
    private static void AlignSelected() { /* ... */ }
}
```

## Menu Items

```csharp
public static class CustomMenuItems
{
    // Tools menu
    [MenuItem("Tools/Clear PlayerPrefs")]
    private static void ClearPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("PlayerPrefs cleared");
    }

    // With keyboard shortcut (% = Ctrl/Cmd, # = Shift, & = Alt)
    [MenuItem("Tools/Quick Build %#b")]
    private static void QuickBuild()
    {
        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            "Builds/Game.exe",
            BuildTarget.StandaloneWindows64,
            BuildOptions.None);
    }

    // Context menu on component
    [MenuItem("CONTEXT/Transform/Reset Position")]
    private static void ResetPosition(MenuCommand command)
    {
        var transform = (Transform)command.context;
        Undo.RecordObject(transform, "Reset Position");
        transform.localPosition = Vector3.zero;
    }

    // Validation — grey out if condition not met
    [MenuItem("Tools/Merge Selected", validate = true)]
    private static bool ValidateMerge() => Selection.gameObjects.Length >= 2;

    [MenuItem("Tools/Merge Selected")]
    private static void MergeSelected() { /* ... */ }

    // Priority controls ordering (lower = higher in menu)
    [MenuItem("Tools/Group A/Item 1", priority = 1)]
    private static void Item1() { }
    [MenuItem("Tools/Group A/Item 2", priority = 2)]
    private static void Item2() { }
    // Priority gap of 11+ creates a separator line
    [MenuItem("Tools/Group A/Item 3", priority = 20)]
    private static void Item3() { }
}
```

## AssetPostprocessor

Automatically process assets on import:

```csharp
public class TexturePostprocessor : AssetPostprocessor
{
    // Runs before texture import
    private void OnPreprocessTexture()
    {
        var importer = (TextureImporter)assetImporter;

        // Auto-configure sprites in UI folder
        if (assetPath.Contains("/UI/"))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 100;
        }

        // Max size for environment textures
        if (assetPath.Contains("/Environment/"))
        {
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }

    // Runs before model import
    private void OnPreprocessModel()
    {
        var importer = (ModelImporter)assetImporter;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.isReadable = false;
    }

    // Runs after ALL assets finish importing
    private static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (var path in importedAssets)
        {
            if (path.EndsWith(".fbx"))
                Debug.Log($"Model imported: {path}");
        }
    }
}
```

