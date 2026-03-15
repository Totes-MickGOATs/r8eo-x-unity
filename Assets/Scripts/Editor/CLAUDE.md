# Assets/Scripts/Editor/

Editor-only scripts: menu items, debug tools, and scene setup automation. These scripts only run inside the Unity Editor and are excluded from builds.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `SceneSetup.cs` | `SceneSetup` | Menu item to create/configure the TestTrack scene with terrain, lighting, and vehicle |
| `OutpostTrackSetup.cs` | `OutpostTrackSetup` | Menu item to create/configure the OutpostTrack scene |
| `TerrainDebug.cs` | `TerrainDebug` | Editor terrain debug visualization utilities |
| `TerrainSpawnCheck.cs` | `TerrainSpawnCheck` | Editor tool to validate spawn point placement on terrain |
| `RCCarEditor.cs` | `RCCarEditor` | Custom inspector for RCCar — foldout groups, range sliders, preset warning, unit-converted fields (km/h, kgf, deg, N/mm) |
| `DrivetrainEditor.cs` | `DrivetrainEditor` | Custom inspector for Drivetrain — hides AWD sections in RWD, disables preload when not BallDiff |
| `R8EOX.Editor.asmdef` | — | Assembly definition (Editor-only platform) |

## Conventions

- Namespace: `R8EOX.Editor`
- All scripts use `[MenuItem]` attributes for Unity menu integration
- Assembly definition targets Editor platform only — excluded from player builds
- `RCCarEditor` depends on `R8EOX.Shared` (`Assets/Scripts/Shared/`) for unit conversion helpers — no UnityEditor code lives in Shared

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

### 2. Wrap every `AssetDatabase.CreateAsset` in a delete-before-create helper

`AssetDatabase.CreateAsset` silently fails if the asset already exists, leaving the object
non-persisted. This causes "works first run, invisible on re-run" bugs.

Every setup script must use a helper like `SaveOrReplaceAsset` (see `OutpostTrackSetup.cs`):

```csharp
static void SaveOrReplaceAsset(UnityEngine.Object obj, string assetPath)
{
    if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
        AssetDatabase.DeleteAsset(assetPath);
    AssetDatabase.CreateAsset(obj, assetPath);
}
```

Bare `AssetDatabase.CreateAsset(...)` calls are not permitted in this directory.

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

## Relevant Skills

- **`unity-editor-scripting`** — Custom editor tools, menu items, and inspector extensions
