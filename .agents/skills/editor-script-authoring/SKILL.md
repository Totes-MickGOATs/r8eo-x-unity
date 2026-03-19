---
name: editor-script-authoring
description: Writes Unity Editor scripts in Assets/Scripts/Editor/ with [MenuItem] attributes, TextureImporter pipeline, and idempotency test requirements. Use when creating scene builders, track setup tools, or [CustomEditor] inspectors. Trigger phrases: 'editor menu item', 'build track', 'setup scene', 'custom inspector', 'OutpostTrackSetup pattern'. Key capabilities: TextureImporter for normal/HDRI/ARM textures, load-and-update for TerrainData/TerrainLayer GUID preservation, AssetDatabase.SaveAssets() ordering, double-call idempotency via LogAssert.NoUnexpectedReceived(). Do NOT use for runtime scripts.
---
# Editor Script Authoring

## Critical

- **Never commit on `main`.** Run `bash scripts/tools/safe-worktree-init.sh <task>` first.
- **GUID preservation rule:** `TerrainData`, `TerrainLayer`, and any asset referenced by GUID must use load-and-update — never `DeleteAsset` + `CreateAsset` on them. Use `SaveOrReplaceAsset` only for ephemeral generated assets (e.g., skybox material) nothing else serializes by GUID.
- **Flush order:** Call `AssetDatabase.SaveAssets()` to persist `TerrainData` to disk **before** calling `Terrain.CreateTerrainGameObject()`. Unsaved `TerrainData` produces dangling references.
- **Every setup method must have an idempotency EditMode test** — call the method twice, assert no duplicate GameObjects, call `LogAssert.NoUnexpectedReceived()`.
- **`#if UNITY_EDITOR` guard** required on all files except those whose `.asmdef` already restricts to `"includePlatforms": ["Editor"]` (e.g., `AddBuggyMaterials.cs`).

---

## Instructions

### Step 1 — Scaffold the file

Create `Assets/Scripts/Editor/<ClassName>.cs`. Use this exact header:

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
// Add UnityEditor.SceneManagement, System.IO, etc. as needed
// Add [assembly: InternalsVisibleTo("R8EOX.Tests.EditMode")] here if tests need internal access

namespace R8EOX.Editor
{
    public static class MySetup
    {
        [MenuItem("RC Buggy/My Setup Action")]
        static void RunSetup() => RunSetupInternal();

        internal static void RunSetupInternal()
        {
            // implementation
        }
    }
}
#endif
```

Menu paths follow the pattern `"RC Buggy/<Action>"` for game tools, `"Tools/RC Buggy/<Action>"` for utility tools. The `[MenuItem]` method is a one-liner delegating to `internal static <Name>Internal()` so EditMode tests can call it via `InternalsVisibleTo`.

Verify the file appears in Unity without compilation errors (`read_console` for errors) before proceeding.

### Step 2 — Declare constants at the top of the class

All asset paths and numeric parameters are `const` fields prefixed `k_`:

```csharp
const string k_TerrainDataAsset  = "Assets/Data/Terrain/OutpostTerrainData.asset";
const string k_DataPath          = "Assets/Data/Terrain";
const int    k_HeightmapRes      = 513;
const float  k_TerrainWidth      = 500f;
```

No bare string literals or magic numbers in method bodies.

### Step 3 — Implement TextureImporter pipeline (if applicable)

Always use `AssetImporter.GetAtPath(path) as TextureImporter` and only call `SaveAndReimport()` when the setting actually differs:

```csharp
// Normal map
TextureImporter ti = AssetImporter.GetAtPath(normalPath) as TextureImporter;
if (ti != null && ti.textureType != TextureImporterType.NormalMap)
{
    ti.textureType = TextureImporterType.NormalMap;
    ti.SaveAndReimport();
}
Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

// ARM / linear texture
if (ti != null && ti.sRGBTexture)
{
    ti.sRGBTexture = false;
    ti.SaveAndReimport();
}

// HDRI (equirectangular panorama)
bool needsReimport = ti.textureShape != TextureImporterShape.Texture2D || ti.sRGBTexture;
if (needsReimport)
{
    ti.textureShape = TextureImporterShape.Texture2D;
    ti.sRGBTexture  = false;
    ti.SaveAndReimport();
}
```

After `SaveAndReimport()`, always re-call `AssetDatabase.LoadAssetAtPath<Texture2D>()` — the in-memory reference may be stale.

### Step 4 — Implement load-and-update for persistent assets

```csharp
static TerrainData LoadOrCreateTerrainData()
{
    var existing = AssetDatabase.LoadAssetAtPath<TerrainData>(k_TerrainDataAsset);
    if (existing != null)
    {
        existing.heightmapResolution = k_HeightmapRes;
        existing.size = new Vector3(k_TerrainWidth, k_TerrainHeight, k_TerrainLength);
        return existing;  // GUID preserved
    }
    var data = new TerrainData();
    // configure data...
    AssetDatabase.CreateAsset(data, k_TerrainDataAsset);
    return data;
}

static TerrainLayer LoadOrConfigureTerrainLayer(string name, string folder, float tileSize)
{
    string path   = $"{k_DataPath}/TerrainLayer_{name}.asset";
    var    layer  = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
    bool   isNew  = layer == null;
    if (isNew) layer = new TerrainLayer();
    // set all properties...
    if (isNew) AssetDatabase.CreateAsset(layer, path);
    else       EditorUtility.SetDirty(layer);
    return layer;
}
```

For ephemeral generated assets (e.g., a skybox material that nothing serializes by GUID):

```csharp
static void SaveOrReplaceAsset(UnityEngine.Object obj, string path)
{
    if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
        AssetDatabase.DeleteAsset(path);
    AssetDatabase.CreateAsset(obj, path);
}
```

### Step 5 — Flush in the correct order

```csharp
internal static void RunSetupInternal()
{
    var terrainData = LoadOrCreateTerrainData();  // load-and-update
    EditorUtility.SetDirty(terrainData);
    AssetDatabase.SaveAssets();                   // flush TerrainData BEFORE creating GO

    var go = Terrain.CreateTerrainGameObject(terrainData);
    // ... configure terrain layers, materials ...

    AssetDatabase.SaveAssets();                   // final flush for all remaining dirty assets
}
```

### Step 6 — Add EditMode tests with idempotency

Create `Assets/Tests/EditMode/<ClassName>Tests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Editor;

namespace R8EOX.Tests.EditMode
{
    public class MySetupTests
    {
        [TearDown]
        public void TearDown()
        {
            var t = Object.FindObjectOfType<Terrain>();
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        [Test]
        public void RunSetup_CreatesTerrainGameObject()
        {
            MySetup.RunSetupInternal();
            Assert.IsNotNull(Object.FindObjectOfType<Terrain>());
        }

        [Test]
        public void RunSetup_IsIdempotent()
        {
            MySetup.RunSetupInternal();
            MySetup.RunSetupInternal();  // second call must not error or duplicate
            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual(1, Object.FindObjectsOfType<Terrain>().Length);
        }
    }
}
```

Run with `just test` and verify RED before implementing, GREEN after.

### Step 7 — Custom Inspector (if applicable)

```csharp
[CustomEditor(typeof(MyComponent))]
public class MyComponentEditor : UnityEditor.Editor
{
    SerializedProperty _speed;
    const string k_FoldMain = "MyComponentEditor.FoldMain";

    void OnEnable() => _speed = serializedObject.FindProperty("_speed");

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        bool open = SessionState.GetBool(k_FoldMain, true);
        bool next = EditorGUILayout.Foldout(open, "Settings", true, EditorStyles.foldoutHeader);
        if (next != open) SessionState.SetBool(k_FoldMain, next);
        if (next) EditorGUILayout.PropertyField(_speed, new GUIContent("Speed (m/s)"));
        serializedObject.ApplyModifiedProperties();
    }
}
```

Cache all `SerializedProperty` in `OnEnable()`. Wrap `OnInspectorGUI()` body in `Update()` / `ApplyModifiedProperties()`.

---

## Examples

**User says:** "Add a menu item that builds the outpost terrain"

**Actions taken:**
1. Create `Assets/Scripts/Editor/OutpostTrackSetup.cs` with `[assembly: InternalsVisibleTo("R8EOX.Tests.EditMode")]` at top, class in `namespace R8EOX.Editor`.
2. Declare `k_TerrainDataAsset`, `k_TerrainWidth`, etc. as `const` fields.
3. `[MenuItem("RC Buggy/Build Outpost Track")] static void BuildOutpostTrack() => BuildOutpostTrackInternal();`
4. `internal static void BuildOutpostTrackInternal()` calls `LoadOrCreateTerrainData()` → `SaveAssets()` → `Terrain.CreateTerrainGameObject(terrainData)` → `SaveAssets()`.
5. Configure terrain layers using `LoadOrConfigureTerrainLayer()` (load-and-update, `EditorUtility.SetDirty`).
6. Set skybox material via `SaveOrReplaceAsset` (ephemeral, no GUID reference chain).
7. Create `Assets/Tests/EditMode/OutpostTrackSetupTests.cs` with `BuildOutpostTrack_IsIdempotent` test using `LogAssert.NoUnexpectedReceived()`.

**Result:** Menu item idempotently builds terrain; double-call leaves exactly one `Terrain` in the scene and no log errors.

---

## Common Issues

**"Object of type TerrainData has been destroyed"** after double-call:
- Root cause: `SaveOrReplaceAsset` used on `TerrainData` — deletes and recreates the asset, breaking the existing `Terrain` reference.
- Fix: Switch to `LoadOrCreateTerrainData()` (load-and-update).

**Texture still shows as Albedo (not Normal) after import:**
- Root cause: `SaveAndReimport()` not called, or called before checking the condition.
- Fix: Always gate on `ti.textureType != TextureImporterType.NormalMap` and call `SaveAndReimport()` inside the `if`, then re-load the texture asset.

**`InternalsVisibleTo` compilation error: "Assembly 'R8EOX.Tests.EditMode' not found":**
- The `[assembly: ...]` attribute must be placed *outside* any `namespace` block at file top level.
- Verify `R8EOX.Tests.EditMode.asmdef` exists and references `R8EOX.Editor`.

**Tests call `BuildOutpostTrackInternal()` but get `inaccessible due to protection level`:**
- The `[assembly: InternalsVisibleTo("R8EOX.Tests.EditMode")]` attribute is missing from the editor script file.
- Ensure it is placed outside any namespace declaration.

**Terrain has null `materialTemplate` after rebuild:**
- Root cause: `new Material()` assigned directly to `terrain.materialTemplate` without persisting first.
- Fix: Call `SaveOrReplaceAsset(mat, k_TerrainMaterialPath)` (or load-and-update if serialized by GUID), then assign.