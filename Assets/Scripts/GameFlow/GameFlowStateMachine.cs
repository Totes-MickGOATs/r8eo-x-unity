using System;
using System.Collections.Generic;

namespace R8EOX.GameFlow
{
    /// <summary>
    /// State machine with validated transitions between game states.
    /// Enforces a strict transition graph; invalid transitions throw.
    /// </summary>
    public sealed class GameFlowStateMachine
    {
        private static readonly Dictionary<GameState, HashSet<GameState>> k_ValidTransitions =
            new Dictionary<GameState, HashSet<GameState>>
            {
                { GameState.Boot, new HashSet<GameState> { GameState.Splash } },
                { GameState.Splash, new HashSet<GameState> { GameState.MainMenu } },
                { GameState.MainMenu, new HashSet<GameState> { GameState.ModeSelect, GameState.Loading } },
                { GameState.ModeSelect, new HashSet<GameState> { GameState.CarSelect, GameState.MainMenu } },
                { GameState.CarSelect, new HashSet<GameState> { GameState.TrackSelect, GameState.ModeSelect } },
                { GameState.TrackSelect, new HashSet<GameState> { GameState.Loading, GameState.CarSelect } },
                { GameState.Loading, new HashSet<GameState> { GameState.Playing } },
                { GameState.Playing, new HashSet<GameState> { GameState.Paused, GameState.Results, GameState.MainMenu } },
                { GameState.Paused, new HashSet<GameState> { GameState.Playing, GameState.MainMenu } },
                { GameState.Results, new HashSet<GameState> { GameState.MainMenu, GameState.Loading } },
            };

        /// <summary>Fired when the state changes. Parameters: (previousState, newState).</summary>
        public event Action<GameState, GameState> OnStateChanged;

        /// <summary>The current game state.</summary>
        public GameState CurrentState { get; private set; } = GameState.Boot;

        /// <summary>
        /// Check whether a transition from the current state to the target is valid.
        /// </summary>
        /// <param name="target">The desired target state.</param>
        /// <returns>True if the transition is allowed.</returns>
        public bool CanTransitionTo(GameState target)
        {
            return k_ValidTransitions.TryGetValue(CurrentState, out HashSet<GameState> targets)
                   && targets.Contains(target);
        }

        /// <summary>
        /// Transition to a new state. Throws if the transition is not valid.
        /// </summary>
        /// <param name="target">The desired target state.</param>
        /// <exception cref="InvalidOperationException">If the transition is not allowed.</exception>
        public void TransitionTo(GameState target)
        {
            if (!CanTransitionTo(target))
            {
                throw new InvalidOperationException(
                    $"Invalid state transition: {CurrentState} -> {target}");
            }

            GameState previous = CurrentState;
            CurrentState = target;
            OnStateChanged?.Invoke(previous, target);
        }
    }
}
