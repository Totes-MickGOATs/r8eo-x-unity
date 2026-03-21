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

## Topic Pages

- [Instructions](skill-instructions.md)

