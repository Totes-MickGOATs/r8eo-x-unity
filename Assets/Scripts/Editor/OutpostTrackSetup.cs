#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using R8EOX.Editor.Builders;

[assembly: InternalsVisibleTo("R8EOX.Tests.EditMode")]

namespace R8EOX.Editor
{
    /// <summary>
    /// Editor tool to build the Outpost test track terrain.
    /// Imports heightmap, configures terrain layers with edge mask blending,
    /// and applies desert environment (skybox, fog, ambient).
    /// Use: Menu -> RC Buggy -> Build Outpost Track
    /// </summary>
    public static class OutpostTrackSetup
    {
        // ---- Constants ----

        const string k_TerrainAssetPath = "Assets/Terrain/Outpost";
        const string k_TexturePath      = k_TerrainAssetPath + "/Textures";
        const string k_DataPath         = k_TerrainAssetPath + "/Data";
        const string k_TerrainDataAsset = k_DataPath + "/OutpostTerrainData.asset";

        const int   k_HeightmapRes = 2049;  // 2^11 + 1
        const float k_TerrainWidth  = 500f; // metres
        const float k_TerrainLength = 500f; // metres
        const float k_TerrainHeight = 10f;  // height scale in metres (10m max over 500m footprint)
        const int   k_DetailRes    = 1024;
        const int   k_AlphamapRes  = 2048;  // Resolution for splatmap blending
        const int   k_BaseMapRes   = 1024;

        const float k_DirtTileSize = 25f;   // Repeating dirt texture tile size in metres

        const string k_TerrainMaterialPath = k_DataPath + "/TerrainMaterial.mat";

        const string k_SkyboxPath         = k_TexturePath + "/Skybox";
        const string k_SkyboxHdriPath     = k_SkyboxPath  + "/goegap_2k.hdr";
        const string k_SkyboxMaterialPath = k_DataPath    + "/DesertSkybox.mat";

        const float k_FogDensity   = 0.005f;
        const float k_SunIntensity = 1.2f;


        // ---- Menu Item ----

        [MenuItem("RC Buggy/Build Outpost Track")]
        static void BuildOutpostTrack() => BuildOutpostTrackInternal();

        // Internal entry point — accessible to EditMode tests via InternalsVisibleTo
        internal static void BuildOutpostTrackInternal()
        {
            UnityEngine.Debug.Log("[OutpostTrack] Building Outpost track...");

            // Load existing TerrainData or create fresh — NEVER delete-and-recreate.
            // TerrainData must be a persisted asset before the Terrain GO is created,
            // otherwise the Terrain component's reference becomes invalid after asset refresh.
            TerrainData terrainData = TerrainBuilder.LoadOrCreateTerrainData(
                k_TerrainDataAsset, k_HeightmapRes,
                k_TerrainWidth, k_TerrainHeight, k_TerrainLength,
                k_AlphamapRes, k_DetailRes, k_BaseMapRes);

            TerrainBuilder.ImportHeightmap(terrainData, k_DataPath, k_HeightmapRes);
            TerrainLayerBuilder.ConfigureTerrainLayers(terrainData, k_DataPath, k_TexturePath, k_DirtTileSize);
            TerrainBuilder.ApplyEdgeMaskSplatmap(terrainData, k_TexturePath);
            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets(); // Persist before creating GO

            GameObject terrainGO = TerrainBuilder.CreateTerrainGameObject(
                terrainData, k_TerrainWidth, k_TerrainLength);
            TerrainBuilder.ConfigureTerrain(terrainGO, k_TerrainMaterialPath, k_TexturePath);

            EnvironmentBuilder.SetupDesertEnvironment(
                k_SkyboxHdriPath, k_SkyboxMaterialPath, k_FogDensity, k_SunIntensity);

            // Flush all pending asset writes (terrain material, layers, terrain data, skybox)
            AssetDatabase.SaveAssets();

            // Mark scene dirty so Unity prompts the user to save — do not auto-save as that
            // can be disruptive (e.g. overwriting an unsaved scene with other work).
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            UnityEngine.Debug.Log("[OutpostTrack] Outpost track built successfully!");
            UnityEngine.Debug.Log($"  Terrain: {k_TerrainWidth}x{k_TerrainLength}m, height={k_TerrainHeight}m");
            UnityEngine.Debug.Log($"  Heightmap: {k_HeightmapRes}x{k_HeightmapRes}");
            Selection.activeGameObject = terrainGO;
        }
    }
}
#endif
