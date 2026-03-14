namespace R8EOX.UI
{
    using System;
    using System.Collections;

    /// <summary>
    /// Contract for a UI screen. Screens are entered with optional data,
    /// animate in/out, and signal when they want to navigate elsewhere.
    /// </summary>
    public interface IScreen
    {
        /// <summary>Unique screen identifier (matches ScreenId constants).</summary>
        string Id { get; }

        /// <summary>Called when the screen becomes active. Data is screen-specific.</summary>
        void Enter(object data = null);

        /// <summary>Called when the screen should animate in. Returns when done.</summary>
        IEnumerator AnimateIn();

        /// <summary>Called when the screen should animate out. Returns when done.</summary>
        IEnumerator AnimateOut();

        /// <summary>Called when the screen is removed.</summary>
        void Exit();

        /// <summary>Raised when this screen wants to navigate to another screen.</summary>
        event Action<string, object> OnNavigationRequested;
    }
}
