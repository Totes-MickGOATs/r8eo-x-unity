namespace R8EOX.GameFlow
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Runtime game flow coordinator. Owns the state machine, navigation stack,
    /// and session config. Persists across scene loads via DontDestroyOnLoad.
    /// Created by SceneBootstrapper if not already present.
    /// </summary>
    public sealed class GameFlowManager : MonoBehaviour, IGameFlowService, IScreenNavigator
    {
        /// <summary>Static instance for bootstrapper access. Not a service locator —
        /// prefer serialized references for normal usage.</summary>
        public static GameFlowManager Instance { get; private set; }

        private GameFlowStateMachine _stateMachine;
        private NavigationStack _navStack;
        private SessionConfig _currentSession;

        // IGameFlowService
        /// <summary>Current game state.</summary>
        public GameState CurrentState => _stateMachine.CurrentState;

        /// <summary>Current session configuration. Null until configured.</summary>
        public SessionConfig CurrentSession => _currentSession;

        /// <summary>Fired when game state changes.</summary>
        public event Action<GameState, GameState> OnStateChanged;

        // IScreenNavigator
        /// <summary>Current screen ID on top of the stack.</summary>
        public string CurrentScreen => _navStack.Current;

        /// <summary>Whether there's a screen to go back to.</summary>
        public bool CanGoBack => _navStack.CanGoBack;

        /// <summary>Fired when a screen is pushed.</summary>
        public event Action<string> OnScreenPushed;

        /// <summary>Fired when a screen is popped.</summary>
        public event Action<string> OnScreenPopped;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _stateMachine = new GameFlowStateMachine();
            _navStack = new NavigationStack();

            // Wire internal events to public interface
            _stateMachine.OnStateChanged += (prev, next) => OnStateChanged?.Invoke(prev, next);
            _navStack.OnScreenPushed += id => OnScreenPushed?.Invoke(id);
            _navStack.OnScreenPopped += id => OnScreenPopped?.Invoke(id);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // IGameFlowService
        /// <summary>Request a state transition. Throws if invalid.</summary>
        public void RequestTransition(GameState target) => _stateMachine.TransitionTo(target);

        /// <summary>Check if a transition is valid from current state.</summary>
        public bool CanTransitionTo(GameState target) => _stateMachine.CanTransitionTo(target);

        /// <summary>Set the session configuration for the current play session.</summary>
        public void SetSession(SessionConfig config)
        {
            _currentSession = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Clear session and return to menu.</summary>
        public void ReturnToMenu()
        {
            _currentSession = null;
            _navStack.Clear();

            // Navigate back to MainMenu from any valid state
            if (CanTransitionTo(GameState.MainMenu))
            {
                _stateMachine.TransitionTo(GameState.MainMenu);
            }
        }

        // IScreenNavigator
        /// <summary>Navigate to a new screen (pushes onto stack).</summary>
        public void NavigateTo(string screenId)
        {
            _navStack.Push(screenId);
        }

        /// <summary>Go back to the previous screen.</summary>
        public void GoBack()
        {
            if (_navStack.CanGoBack)
            {
                _navStack.Pop();
            }
        }

        /// <summary>Get the breadcrumb trail (bottom to top).</summary>
        public string[] GetBreadcrumbs() => _navStack.GetBreadcrumbs();

        /// <summary>
        /// Fast-forward the state machine to Playing state for standalone scene launch.
        /// Skips the menu flow entirely.
        /// </summary>
        public void BootDirectToPlaying()
        {
            // Walk through required transitions: Boot -> Splash -> MainMenu -> Loading -> Playing
            if (_stateMachine.CurrentState == GameState.Boot)
            {
                _stateMachine.TransitionTo(GameState.Splash);
            }

            if (_stateMachine.CurrentState == GameState.Splash)
            {
                _stateMachine.TransitionTo(GameState.MainMenu);
            }

            if (_stateMachine.CurrentState == GameState.MainMenu)
            {
                _stateMachine.TransitionTo(GameState.Loading);
            }

            if (_stateMachine.CurrentState == GameState.Loading)
            {
                _stateMachine.TransitionTo(GameState.Playing);
            }
        }
    }
}
