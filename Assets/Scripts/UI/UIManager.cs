namespace R8EOX.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using R8EOX.GameFlow;
    using R8EOX.Shared;
    using UnityEngine;

    /// <summary>
    /// Manages UI screen lifecycle, canvas layers, and overlay stack.
    /// Persists across scene loads. Listens to IGameFlowService for state changes
    /// and IScreenNavigator for navigation events.
    /// Coroutine helpers are in UIManagerTransitions.cs (partial class).
    /// </summary>
    public sealed partial class UIManager : MonoBehaviour
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
                _gameFlow.OnStateChanged += HandleStateChanged;

            if (_navigator != null)
                _navigator.OnScreenPushed += HandleScreenPushed;
        }

        private void OnDisable()
        {
            if (_gameFlow != null)
                _gameFlow.OnStateChanged -= HandleStateChanged;

            if (_navigator != null)
                _navigator.OnScreenPushed -= HandleScreenPushed;
        }

        private void HandleScreenPushed(string screenId) => ShowScreen(screenId);

        /// <summary>Show a screen by ID. Replaces the current main screen.</summary>
        public void ShowScreen(string screenId, object data = null)
        {
            if (_screenRegistry == null)
            {
                RuntimeLog.LogWarning($"[UIManager] No ScreenRegistry assigned. Cannot show '{screenId}'.");
                return;
            }

            if (!_screenRegistry.TryGetScreen(screenId, out var prefab))
            {
                RuntimeLog.LogWarning($"[UIManager] Screen '{screenId}' not found in registry.");
                return;
            }

            StartCoroutine(TransitionToScreen(prefab, screenId, data));
        }

        /// <summary>Push an overlay screen (e.g., pause menu).</summary>
        public void PushOverlay(string screenId, object data = null)
        {
            if (_screenRegistry == null || !_screenRegistry.TryGetScreen(screenId, out var prefab))
            {
                RuntimeLog.LogWarning($"[UIManager] Overlay '{screenId}' not found in registry.");
                return;
            }

            var instance = Instantiate(prefab, _overlayLayer);
            var screen = instance.GetComponent<IScreen>();
            if (screen == null)
            {
                RuntimeLog.LogError($"[UIManager] Overlay prefab '{screenId}' has no IScreen component.");
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
                return;

            StartCoroutine(PopOverlayRoutine(_overlayStack.Pop()));
        }

        /// <summary>Clear all overlays.</summary>
        public void ClearOverlays()
        {
            while (_overlayStack.Count > 0)
                ExitAndDestroy(_overlayStack.Pop());
        }

        private void HandleStateChanged(GameState previous, GameState current)
        {
            switch (current)
            {
                case GameState.Playing:
                    ClearOverlays();
                    if (_activeScreen != null)
                    {
                        ExitAndDestroy(_activeScreen);
                        _activeScreen = null;
                    }

                    break;
                case GameState.Paused:
                    PushOverlay(ScreenId.Pause);
                    break;
            }
        }
    }
}
