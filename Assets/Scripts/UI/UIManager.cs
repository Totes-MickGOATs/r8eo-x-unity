namespace R8EOX.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using R8EOX.GameFlow;
    using UnityEngine;

    /// <summary>
    /// Manages UI screen lifecycle, canvas layers, and overlay stack.
    /// Persists across scene loads. Listens to IGameFlowService for state changes
    /// and IScreenNavigator for navigation events.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Registry mapping screen IDs to prefabs")]
        private ScreenRegistry _screenRegistry;

        [SerializeField]
        [Tooltip("Transform parent for menu screens")]
        private Transform _menuLayer;

        [SerializeField]
        [Tooltip("Transform parent for overlay screens (pause, results)")]
        private Transform _overlayLayer;

        private IScreen _activeScreen;
        private readonly Stack<IScreen> _overlayStack = new();
        private IGameFlowService _gameFlow;
        private IScreenNavigator _navigator;

        /// <summary>The currently active main screen.</summary>
        public IScreen ActiveScreen => _activeScreen;

        /// <summary>Number of overlays currently showing.</summary>
        public int OverlayCount => _overlayStack.Count;

        /// <summary>Initialize with service references. Called by GameFlowManager or bootstrapper.</summary>
        public void Init(IGameFlowService gameFlow, IScreenNavigator navigator)
        {
            _gameFlow = gameFlow ?? throw new ArgumentNullException(nameof(gameFlow));
            _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        private void OnEnable()
        {
            if (_gameFlow != null)
            {
                _gameFlow.OnStateChanged += HandleStateChanged;
            }

            if (_navigator != null)
            {
                _navigator.OnScreenPushed += HandleScreenPushed;
            }
        }

        private void OnDisable()
        {
            if (_gameFlow != null)
            {
                _gameFlow.OnStateChanged -= HandleStateChanged;
            }

            if (_navigator != null)
            {
                _navigator.OnScreenPushed -= HandleScreenPushed;
            }
        }

        /// <summary>Show a screen by ID. Replaces the current main screen.</summary>
        public void ShowScreen(string screenId, object data = null)
        {
            if (_screenRegistry == null)
            {
                Debug.LogWarning($"[UIManager] No ScreenRegistry assigned. Cannot show '{screenId}'.");
                return;
            }

            if (!_screenRegistry.TryGetScreen(screenId, out var prefab))
            {
                Debug.LogWarning($"[UIManager] Screen '{screenId}' not found in registry.");
                return;
            }

            StartCoroutine(TransitionToScreen(prefab, screenId, data));
        }

        /// <summary>Push an overlay screen (e.g., pause menu).</summary>
        public void PushOverlay(string screenId, object data = null)
        {
            if (_screenRegistry == null || !_screenRegistry.TryGetScreen(screenId, out var prefab))
            {
                Debug.LogWarning($"[UIManager] Overlay '{screenId}' not found in registry.");
                return;
            }

            var instance = Instantiate(prefab, _overlayLayer);
            var screen = instance.GetComponent<IScreen>();
            if (screen == null)
            {
                Debug.LogError($"[UIManager] Overlay prefab '{screenId}' has no IScreen component.");
                Destroy(instance);
                return;
            }

            _overlayStack.Push(screen);
            screen.Enter(data);
            StartCoroutine(screen.AnimateIn());
        }

        /// <summary>Pop the topmost overlay.</summary>
        public void PopOverlay()
        {
            if (_overlayStack.Count == 0)
            {
                return;
            }

            var overlay = _overlayStack.Pop();
            StartCoroutine(PopOverlayRoutine(overlay));
        }

        /// <summary>Clear all overlays.</summary>
        public void ClearOverlays()
        {
            while (_overlayStack.Count > 0)
            {
                var overlay = _overlayStack.Pop();
                overlay.Exit();
                if (overlay is MonoBehaviour mb)
                {
                    Destroy(mb.gameObject);
                }
            }
        }

        private IEnumerator TransitionToScreen(GameObject prefab, string screenId, object data)
        {
            // Animate out current screen
            if (_activeScreen != null)
            {
                yield return _activeScreen.AnimateOut();
                _activeScreen.Exit();
                if (_activeScreen is MonoBehaviour mb)
                {
                    Destroy(mb.gameObject);
                }
            }

            // Instantiate and animate in new screen
            var instance = Instantiate(prefab, _menuLayer);
            var screen = instance.GetComponent<IScreen>();
            if (screen == null)
            {
                Debug.LogError($"[UIManager] Prefab '{screenId}' has no IScreen component.");
                Destroy(instance);
                yield break;
            }

            _activeScreen = screen;
            screen.Enter(data);
            yield return screen.AnimateIn();
        }

        private IEnumerator PopOverlayRoutine(IScreen overlay)
        {
            yield return overlay.AnimateOut();
            overlay.Exit();
            if (overlay is MonoBehaviour mb)
            {
                Destroy(mb.gameObject);
            }
        }

        private void HandleStateChanged(GameState previous, GameState current)
        {
            // When transitioning to menu states, show appropriate screens
            // When transitioning to Playing, hide menus
            switch (current)
            {
                case GameState.Playing:
                    ClearOverlays();
                    if (_activeScreen != null)
                    {
                        _activeScreen.Exit();
                        if (_activeScreen is MonoBehaviour mb)
                        {
                            Destroy(mb.gameObject);
                        }

                        _activeScreen = null;
                    }

                    break;
                case GameState.Paused:
                    PushOverlay(ScreenId.Pause);
                    break;
            }
        }

        private void HandleScreenPushed(string screenId)
        {
            ShowScreen(screenId);
        }
    }
}
