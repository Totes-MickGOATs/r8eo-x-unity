#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using R8EOX.Vehicle;
using R8EOX.Debug;
using R8EOX.Editor.Builders;

namespace R8EOX.Editor
{
    /// <summary>
    /// Editor menu item that builds the complete test scene and RC Buggy prefab.
    /// Use: Menu -> RC Buggy -> Build Test Scene
    ///
    /// Scene building is delegated to focused builder classes:
    ///   - PhysicsConfigurator  — physics + project settings
    ///   - RCBuggyBuilder       — car hierarchy, wheels, components
    ///   - TestTrackBuilder     — ground, ramps, obstacles, camera, lighting
    /// </summary>
    public static class SceneSetup
    {
        // ---- Constants ----

        const string k_ScenePath = "Assets/Scenes/TestTrack.unity";
        const string k_PrefabPath = "Assets/Prefabs/RCBuggy.prefab";
        const float k_DefaultCarHeight = 0.5f;

        // ---- Menu Items ----

        [MenuItem("RC Buggy/Build Test Scene")]
        static void BuildTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            PhysicsConfigurator.ConfigurePhysics();
            PhysicsConfigurator.ConfigureProjectSettings();

            GameObject car = RCBuggyBuilder.BuildRCBuggy();
            car.transform.position = new Vector3(0f, k_DefaultCarHeight, 0f);

            TestTrackBuilder.BuildTestTrack();
            TestTrackBuilder.SetupCamera(car);
            TestTrackBuilder.SetupLighting();

            GameObject hudGO = new GameObject("TelemetryHUD");
            var hud = hudGO.AddComponent<TelemetryHUD>();
            // TelemetryHUD._car is [SerializeField] private — set via SerializedObject
            var so = new SerializedObject(hud);
            so.FindProperty("_car").objectReferenceValue = car.GetComponent<RCCar>();
            so.ApplyModifiedProperties();

            System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
            EditorSceneManager.SaveScene(scene, k_ScenePath);

            System.IO.Directory.CreateDirectory(Application.dataPath + "/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(car, k_PrefabPath);

            UnityEngine.Debug.Log("[SceneSetup] Test scene and prefab created successfully!");
            UnityEngine.Debug.Log($"  Scene: {k_ScenePath}");
            UnityEngine.Debug.Log($"  Prefab: {k_PrefabPath}");
        }

        [MenuItem("RC Buggy/Configure Physics Settings")]
        static void ConfigurePhysicsMenuItem() => PhysicsConfigurator.ConfigurePhysics();
    }
}
#endif
