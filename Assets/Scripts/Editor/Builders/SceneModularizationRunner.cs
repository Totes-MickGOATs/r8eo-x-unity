#if UNITY_EDITOR
using UnityEngine;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Unified batchmode entry point that runs all scene/prefab modularization
    /// builders in the correct order.
    ///
    /// Called by <c>just scene-modularize</c>:
    ///   Unity -batchmode -nographics -quit -projectPath &lt;proj&gt;
    ///         -executeMethod R8EOX.Editor.Builders.SceneModularizationRunner.RunAll
    ///         -logFile Logs/scene-modularize.log
    ///
    /// Order of operations
    /// -------------------
    ///   1. RCBuggyModularBuilder.Run()       — extracts nested wheel/body prefabs
    ///   2. TrackSceneModularBuilder.ModularizeTestTrack()    — decomposes TestTrack.unity
    ///   3. TrackSceneModularBuilder.ModularizeOutpostTrack() — decomposes OutpostTrack.unity
    ///
    /// All builders are idempotent — calling RunAll a second time is a no-op.
    /// </summary>
    public static class SceneModularizationRunner
    {
        public static void RunAll()
        {
            Debug.Log("[SceneModularizationRunner] Starting scene/prefab modularization...");

            Debug.Log("[SceneModularizationRunner] Step 1/3 — RCBuggy nested prefabs");
            RCBuggyModularBuilder.Run();

            Debug.Log("[SceneModularizationRunner] Step 2/3 — TestTrack additive scenes");
            TrackSceneModularBuilder.ModularizeTestTrack();

            Debug.Log("[SceneModularizationRunner] Step 3/3 — OutpostTrack additive scenes");
            TrackSceneModularBuilder.ModularizeOutpostTrack();

            Debug.Log("[SceneModularizationRunner] All modularization complete.");
        }
    }
}
#endif
