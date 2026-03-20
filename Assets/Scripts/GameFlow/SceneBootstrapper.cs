namespace R8EOX.GameFlow
{
    using UnityEngine;
    using R8EOX.Shared;

    /// <summary>
    /// Place on a GameObject in every gameplay scene. On Awake, checks whether
    /// GameFlowManager exists. If not (scene launched directly from editor),
    /// creates a minimal manager and fast-forwards to Playing state.
    /// When launched via the full flow (Boot -> Splash -> Menu -> Load), this
    /// component does nothing — the manager already exists.
    /// </summary>
    public sealed class SceneBootstrapper : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("SceneRegistry asset for looking up scene metadata")]
        private SceneRegistry _sceneRegistry;

        [SerializeField]
        [Tooltip("If true, skip straight to Playing state when bootstrapping standalone")]
        private bool _directToPlaying = true;

        /// <summary>Whether this bootstrapper created the manager (standalone mode).</summary>
        public bool IsStandaloneMode { get; private set; }

        private void Awake()
        {
            if (GameFlowManager.Instance != null)
            {
                // Full flow — manager already exists, nothing to do
                IsStandaloneMode = false;
                return;
            }

            // Standalone mode — create minimal manager
            IsStandaloneMode = true;
            RuntimeLog.Log("[SceneBootstrapper] No GameFlowManager found. Creating standalone manager.");

            var managerGo = new GameObject("[GameFlowManager]");
            var manager = managerGo.AddComponent<GameFlowManager>();

            // Set up a default session for standalone play
            string sceneName = gameObject.scene.name;
            var session = new SessionConfig(
                modeId: "testing",
                trackId: sceneName,
                trackScene: gameObject.scene.path,
                carId: "rc_buggy",
                totalLaps: 0,
                aiDifficulty: 0
            );
            manager.SetSession(session);

            if (_directToPlaying)
            {
                manager.BootDirectToPlaying();
            }
        }
    }
}
