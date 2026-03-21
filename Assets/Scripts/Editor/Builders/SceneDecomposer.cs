#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Low-level scene decomposition utilities used by <see cref="TrackSceneModularBuilder"/>.
    /// Classifies root GameObjects by category (Terrain / Props / Lighting), creates an
    /// additive sub-scene for each category, and moves matching objects into it.
    /// </summary>
    internal static class SceneDecomposer
    {
        internal enum Category { Terrain, Props, Lighting }

        // ---- Classification sets ----

        static readonly HashSet<string> k_TerrainNames = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            "Ground", "Terrain", "TerrainRoot", "Landscape"
        };

        static readonly HashSet<string> k_PropNames = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            "SmallRamp", "LargeRamp",
            "Box1", "Box2", "Box3", "Box4", "Box5",
            "Obstacle", "Props", "PropsRoot"
        };

        static readonly HashSet<string> k_LightNames = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase)
        {
            "Directional Light", "Sun", "DirectionalLight",
            "LightProbeGroup", "ReflectionProbe"
        };

        // ---- Public API ----

        /// <summary>
        /// Opens <paramref name="scenePath"/> in Single mode.
        /// Returns the scene on success, or <c>null</c> on failure after logging an error.
        /// </summary>
        internal static Scene? OpenOrFail(string scenePath)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError("[SceneDecomposer] Scene not found: " + scenePath);
                return null;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[SceneDecomposer] Could not open: " + scenePath);
                return null;
            }

            return scene;
        }

        /// <summary>
        /// Collects all root GameObjects in <paramref name="source"/> matching
        /// <paramref name="category"/>, moves them to a new scene saved at
        /// <paramref name="destPath"/>, then closes the additive scene.
        /// </summary>
        internal static void ExtractCategory(Scene source, string destPath, Category category)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destPath) != null)
            {
                Debug.Log("[SceneDecomposer] " + destPath + " exists — skipping.");
                return;
            }

            var objects = CollectByCategory(source, category);
            CreateAdditiveScene(destPath, objects);
        }

        // ---- Private helpers ----

        static List<GameObject> CollectByCategory(Scene scene, Category category)
        {
            var result = new List<GameObject>();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (Matches(go, category)) result.Add(go);
            }

            return result;
        }

        static bool Matches(GameObject go, Category category)
        {
            switch (category)
            {
                case Category.Terrain:
                    return k_TerrainNames.Contains(go.name) ||
                           go.GetComponentInChildren<Terrain>() != null;
                case Category.Props:
                    return k_PropNames.Contains(go.name);
                case Category.Lighting:
                    return k_LightNames.Contains(go.name) ||
                           go.GetComponent<Light>() != null;
                default:
                    return false;
            }
        }

        static void CreateAdditiveScene(string destPath, List<GameObject> objects)
        {
            var newScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            foreach (var go in objects)
                SceneManager.MoveGameObjectToScene(go, newScene);

            EditorSceneManager.SaveScene(newScene, destPath);
            EditorSceneManager.CloseScene(newScene, removeScene: true);

            Debug.Log("[SceneDecomposer] Created " + destPath +
                      " (" + objects.Count + " root objects)");
        }
    }
}
#endif
