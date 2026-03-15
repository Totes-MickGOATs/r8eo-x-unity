# Assets/Scripts/Editor/

Editor-only scripts: menu items, debug tools, and scene setup automation. These scripts only run inside the Unity Editor and are excluded from builds.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `SceneSetup.cs` | `SceneSetup` | Menu item to create/configure the TestTrack scene with terrain, lighting, and vehicle |
| `OutpostTrackSetup.cs` | `OutpostTrackSetup` | Menu item to create/configure the OutpostTrack scene |
| `OutpostTerrainConfig.cs` | `OutpostTerrainConfig` | ScriptableObject holding configurable terrain dimensions (Width, Length, MaxHeight, DirtTileSize); auto-created by OutpostTrackSetup if absent |
| `R8EOX.Editor.asmdef` | — | Assembly definition (Editor-only platform) |

## Conventions

- Namespace: `R8EOX.Editor`
- All scripts use `[MenuItem]` attributes for Unity menu integration
- Assembly definition targets Editor platform only — excluded from player builds

## Editor Script Rules (Mandatory)

These rules exist to prevent two recurring bug classes discovered in OutpostTrackSetup.

### 1. Never hand-author `.meta` files — use `TextureImporter` in code

Any texture whose import settings matter (normal maps, linear textures, HDRIs) must be
configured programmatically via `TextureImporter`. Hand-authored `.meta` files bypass
Unity's validation and cannot be tested.

Pattern already used for normal maps — extend it to all textures:

```csharp
TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
importer.textureType   = TextureImporterType.Default;
importer.sRGBTexture   = false;                            // for linear/HDR textures
importer.textureShape  = TextureImporterShape.Texture2D;   // not Cube for equirectangular
importer.SaveAndReimport();
```

### 2. Use load-and-update for assets referenced by components; SaveOrReplace only for generated assets

`AssetDatabase.CreateAsset` silently fails if the asset already exists. The correct fix
depends on whether anything references the asset by GUID:

**TerrainData, TerrainLayers, and any asset a Component serializes by GUID** — use load-and-update.
Deleting and recreating these assets changes their GUID, which can invalidate Terrain component
references after asset refresh. Load the existing asset and modify it in place:

```csharp
static TerrainData LoadOrCreateTerrainData()
{
    var existing = AssetDatabase.LoadAssetAtPath<TerrainData>(k_TerrainDataAsset);
    if (existing != null) { /* update fields */ EditorUtility.SetDirty(existing); return existing; }
    var data = new TerrainData(); /* configure */
    AssetDatabase.CreateAsset(data, k_TerrainDataAsset);
    return data;
}
```

**Generated assets nothing references by GUID** (e.g. skybox materials) — `SaveOrReplaceAsset` is fine:

```csharp
static void SaveOrReplaceAsset(UnityEngine.Object obj, string assetPath)
{
    if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
        AssetDatabase.DeleteAsset(assetPath);
    AssetDatabase.CreateAsset(obj, assetPath);
}
```

**Critical ordering rule:** Save TerrainData to disk (via `AssetDatabase.CreateAsset` or
`EditorUtility.SetDirty` + `AssetDatabase.SaveAssets()`) BEFORE calling
`Terrain.CreateTerrainGameObject(terrainData)`. Creating the GO with an unsaved TerrainData
can cause the Terrain component reference to become invalid after asset refresh.

### 3. Require an EditMode idempotency test for every setup method

Any method that writes to `AssetDatabase` must have a test that calls it twice and asserts
no errors and consistent output. This catches "works first time, invisible on re-run" bugs.

```csharp
[Test]
public void BuildMethod_CalledTwice_ProducesNoErrors()
{
    LogAssert.NoUnexpectedReceived();
    MyEditorSetup.BuildMethod();
    MyEditorSetup.BuildMethod(); // second call must not throw or log errors
}
```

### 4. Never assign `new Material()` or any unsaved object to a serialized scene field

`new Material(shader)` creates an in-memory object with no asset GUID. Unity cannot serialize
a GUID-less object into a scene file, so the field silently saves as null and reverts to null
on every domain reload — terrain becomes invisible, components lose their material, etc.

**Always save the object as an asset file before assigning it:**

```csharp
// BAD — reverts to null on reload:
terrain.materialTemplate = new Material(shader);

// GOOD — survives reload:
var mat = AssetDatabase.LoadAssetAtPath<Material>(k_TerrainMaterialPath);
if (mat == null) { mat = new Material(shader); SaveOrReplaceAsset(mat, k_TerrainMaterialPath); }
else             { mat.shader = shader; EditorUtility.SetDirty(mat); }
terrain.materialTemplate = mat;
EditorUtility.SetDirty(terrain);
AssetDatabase.SaveAssets();
```

This applies to Materials, ScriptableObjects, and any other Unity object assigned to a
`[SerializeField]` or public field that Unity serializes into the scene.

## Relevant Skills

- **`unity-editor-scripting`** — Custom editor tools, menu items, and inspector extensions
