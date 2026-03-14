namespace R8EOX.GameFlow
{
    using System;

    /// <summary>
    /// Contract for game flow coordination. Consumed by UI and scene systems.
    /// </summary>
    public interface IGameFlowService
    {
        /// <summary>Current game state.</summary>
        GameState CurrentState { get; }

        /// <summary>Current session configuration. Null until configured.</summary>
        SessionConfig CurrentSession { get; }

        /// <summary>Fired when game state changes.</summary>
        event Action<GameState, GameState> OnStateChanged;

        /// <summary>Request a state transition. Throws if invalid.</summary>
        void RequestTransition(GameState target);

        /// <summary>Check if a transition is valid from current state.</summary>
        bool CanTransitionTo(GameState target);

        /// <summary>Set the session configuration for the current play session.</summary>
        void SetSession(SessionConfig config);

        /// <summary>Clear session and return to menu.</summary>
        void ReturnToMenu();
    }
}
