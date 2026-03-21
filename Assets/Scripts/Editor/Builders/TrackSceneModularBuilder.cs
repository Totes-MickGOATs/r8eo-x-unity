#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Decomposes TestTrack.unity and OutpostTrack.unity into additive sub-scenes
    /// (terrain, props, lighting) so no single .unity file exceeds 150 lines.
    /// Extraction logic lives in <see cref="SceneDecomposer"/>.
    ///
    /// Idempotency: skips any track whose sub-scene files already exist.
    ///
    /// Batchmode:
    ///   Unity -batchmode -nographics -quit -projectPath &lt;proj&gt;
    ///         -executeMethod R8EOX.Editor.Builders.TrackSceneModularBuilder.RunFromCommandLine
    ///
    /// Menu: R8EOX -> Build -> Modularize TestTrack / OutpostTrack
    /// </summary>
    internal static class TrackSceneModularBuilder
    {
        // ---- TestTrack paths ----
        internal const string k_TestTrackScene    = "Assets/Scenes/TestTrack.unity";
        internal const string k_TestTrackTerrain  = "Assets/Scenes/TestTrack-Terrain.unity";
        internal const string k_TestTrackProps    = "Assets/Scenes/TestTrack-Props.unity";
        internal const string k_TestTrackLighting = "Assets/Scenes/TestTrack-Lighting.unity";

        // ---- OutpostTrack paths ----
        internal const string k_OutpostScene    = "Assets/Scenes/OutpostTrack.unity";
        internal const string k_OutpostTerrain  = "Assets/Scenes/OutpostTrack-Terrain.unity";
        internal const string k_OutpostLighting = "Assets/Scenes/OutpostTrack-Lighting.unity";

        // ---- Menu entries ----

        [MenuItem("R8EOX/Build/Modularize TestTrack")]
        internal static void ModularizeTestTrackFromMenu()
        {
            ModularizeTestTrack();
            Debug.Log("[TrackSceneModularBuilder] TestTrack done.");
        }

        [MenuItem("R8EOX/Build/Modularize OutpostTrack")]
        internal static void ModularizeOutpostTrackFromMenu()
        {
            ModularizeOutpostTrack();
            Debug.Log("[TrackSceneModularBuilder] OutpostTrack done.");
        }

        // ---- Batchmode entry ----

        public static void RunFromCommandLine()
        {
            ModularizeTestTrack();
            ModularizeOutpostTrack();
        }

        // ---- Public API ----

        internal static void ModularizeTestTrack()
        {
            if (TestTrackAlreadyModularized())
            {
                Debug.Log("[TrackSceneModularBuilder] TestTrack already modularized.");
                return;
            }

            var masterScene = SceneDecomposer.OpenOrFail(k_TestTrackScene);
            if (!masterScene.HasValue) return;

            SceneDecomposer.ExtractCategory(
                masterScene.Value, k_TestTrackTerrain,
                SceneDecomposer.Category.Terrain);
            SceneDecomposer.ExtractCategory(
                masterScene.Value, k_TestTrackProps,
                SceneDecomposer.Category.Props);
            SceneDecomposer.ExtractCategory(
                masterScene.Value, k_TestTrackLighting,
                SceneDecomposer.Category.Lighting);

            EditorSceneManager.SaveScene(masterScene.Value);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TrackSceneModularBuilder] TestTrack decomposed into 4 scenes.");
        }

        internal static void ModularizeOutpostTrack()
        {
            if (OutpostTrackAlreadyModularized())
            {
                Debug.Log("[TrackSceneModularBuilder] OutpostTrack already modularized.");
                return;
            }

            var masterScene = SceneDecomposer.OpenOrFail(k_OutpostScene);
            if (!masterScene.HasValue) return;

            SceneDecomposer.ExtractCategory(
                masterScene.Value, k_OutpostTerrain,
                SceneDecomposer.Category.Terrain);
            SceneDecomposer.ExtractCategory(
                masterScene.Value, k_OutpostLighting,
                SceneDecomposer.Category.Lighting);

            EditorSceneManager.SaveScene(masterScene.Value);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TrackSceneModularBuilder] OutpostTrack decomposed into 3 scenes.");
        }

        // ---- Idempotency guards ----

        internal static bool TestTrackAlreadyModularized() =>
            AssetDatabase.LoadAssetAtPath<SceneAsset>(k_TestTrackTerrain)  != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(k_TestTrackProps)    != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(k_TestTrackLighting) != null;

        internal static bool OutpostTrackAlreadyModularized() =>
            AssetDatabase.LoadAssetAtPath<SceneAsset>(k_OutpostTerrain)  != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(k_OutpostLighting) != null;
    }
}
#endif
