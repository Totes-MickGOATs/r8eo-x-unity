using System;
using System.Collections.Generic;
using System.Linq;

namespace R8EOX.GameFlow
{
    /// <summary>
    /// Stack-based screen navigation with breadcrumbs and events.
    /// </summary>
    public sealed class NavigationStack
    {
        private readonly Stack<string> _screens = new Stack<string>();

        /// <summary>Fired when a screen is pushed onto the stack.</summary>
        public event Action<string> OnScreenPushed;

        /// <summary>Fired when a screen is popped from the stack.</summary>
        public event Action<string> OnScreenPopped;

        /// <summary>The current (topmost) screen, or null if empty.</summary>
        public string Current => _screens.Count > 0 ? _screens.Peek() : null;

        /// <summary>Whether there is a screen to go back to (more than one entry).</summary>
        public bool CanGoBack => _screens.Count > 1;

        /// <summary>Number of screens on the stack.</summary>
        public int Count => _screens.Count;

        /// <summary>
        /// Push a screen onto the navigation stack.
        /// </summary>
        /// <param name="screenId">Non-null, non-empty screen identifier.</param>
        /// <exception cref="ArgumentException">If screenId is null, empty, or whitespace.</exception>
        public void Push(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                throw new ArgumentException("Screen ID cannot be null, empty, or whitespace.", nameof(screenId));
            }

            _screens.Push(screenId);
            OnScreenPushed?.Invoke(screenId);
        }

        /// <summary>
        /// Pop the topmost screen from the stack.
        /// </summary>
        /// <returns>The removed screen identifier.</returns>
        /// <exception cref="InvalidOperationException">If the stack is empty.</exception>
        public string Pop()
        {
            if (_screens.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop from an empty navigation stack.");
            }

            string popped = _screens.Pop();
            OnScreenPopped?.Invoke(popped);
            return popped;
        }

        /// <summary>
        /// Returns all screens as an array ordered from bottom (oldest) to top (newest).
        /// </summary>
        public string[] GetBreadcrumbs()
        {
            string[] result = _screens.ToArray();
            Array.Reverse(result);
            return result;
        }

        /// <summary>
        /// Remove all screens from the stack.
        /// </summary>
        public void Clear()
        {
            _screens.Clear();
        }
    }
}
