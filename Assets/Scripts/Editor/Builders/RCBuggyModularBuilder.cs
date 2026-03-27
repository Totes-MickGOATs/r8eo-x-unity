#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Decomposes the monolithic RCBuggy.prefab into nested prefabs per logical
    /// sub-assembly (body meshes + four wheels).  Actual extraction logic lives in
    /// <see cref="RCBuggyNestedPrefabExtractor"/>.
    ///
    /// Idempotency: if all nested prefabs already exist at their canonical paths
    /// the builder returns immediately without modifying any asset.
    ///
    /// Batchmode:
    ///   Unity -batchmode -nographics -quit -projectPath &lt;proj&gt;
    ///         -executeMethod R8EOX.Editor.Builders.RCBuggyModularBuilder.RunFromCommandLine
    ///
    /// Menu: R8EOX -> Build -> Modularize RCBuggy
    /// </summary>
    internal static class RCBuggyModularBuilder
    {
        internal const string k_RootPrefabPath = "Assets/Prefabs/RCBuggy.prefab";
        internal const string k_NestedDir      = "Assets/Prefabs/RCBuggy";
        internal const string k_BodyPrefabPath = k_NestedDir + "/RCBody.prefab";
        internal const string k_WheelFLPath    = k_NestedDir + "/RCWheelFL.prefab";
        internal const string k_WheelFRPath    = k_NestedDir + "/RCWheelFR.prefab";
        internal const string k_WheelRLPath    = k_NestedDir + "/RCWheelRL.prefab";
        internal const string k_WheelRRPath    = k_NestedDir + "/RCWheelRR.prefab";

        // ---- Menu entry ----

        [MenuItem("R8EOX/Build/Modularize RCBuggy")]
        internal static void ModularizeFromMenu()
        {
            Run();
            Debug.Log("[RCBuggyModularBuilder] Done.");
        }

        // ---- Batchmode entry ----

        public static void RunFromCommandLine()
        {
            Run();
            Debug.Log("[RCBuggyModularBuilder] Batchmode complete.");
        }

        // ---- Core logic ----

        /// <summary>
        /// Idempotent: exits early if all five nested prefabs already exist.
        /// Otherwise instantiates the root prefab into a staging scene, extracts
        /// sub-assemblies, and writes the modified root back to disk.
        /// </summary>
        internal static void Run()
        {
            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_RootPrefabPath);
            if (rootPrefab == null)
            {
                Debug.LogError("[RCBuggyModularBuilder] Root prefab not found: " + k_RootPrefabPath);
                return;
            }

            if (AllNestedPrefabsExist())
            {
                Debug.Log("[RCBuggyModularBuilder] Already modularized — nothing to do.");
                return;
            }

            EnsureNestedDirectory();

            // Instantiate into editor for safe in-memory edits
            var staging = PrefabUtility.InstantiatePrefab(rootPrefab) as GameObject;
            if (staging == null)
            {
                Debug.LogError("[RCBuggyModularBuilder] Could not instantiate root prefab.");
                return;
            }

            try
            {
                RCBuggyNestedPrefabExtractor.ExtractWheels(staging);
                RCBuggyNestedPrefabExtractor.ExtractBody(staging);

                PrefabUtility.SaveAsPrefabAsset(staging, k_RootPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[RCBuggyModularBuilder] RCBuggy modularized successfully.");
            }
            finally
            {
                Object.DestroyImmediate(staging);
            }
        }

        // ---- Idempotency helpers ----

        internal static bool AllNestedPrefabsExist() =>
            AssetDatabase.LoadAssetAtPath<GameObject>(k_BodyPrefabPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(k_WheelFLPath)    != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(k_WheelFRPath)    != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(k_WheelRLPath)    != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(k_WheelRRPath)    != null;

        static void EnsureNestedDirectory()
        {
            if (!AssetDatabase.IsValidFolder(k_NestedDir))
            {
                string parent = Path.GetDirectoryName(k_NestedDir).Replace('\\', '/');
                string folder = Path.GetFileName(k_NestedDir);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
#endif
