namespace R8EOX.GameFlow
{
    using System;

    /// <summary>
    /// Contract for screen navigation. Consumed by UI screens to navigate.
    /// </summary>
    public interface IScreenNavigator
    {
        /// <summary>Current screen ID on top of the stack.</summary>
        string CurrentScreen { get; }

        /// <summary>Whether there's a screen to go back to.</summary>
        bool CanGoBack { get; }

        /// <summary>Navigate to a new screen (pushes onto stack).</summary>
        void NavigateTo(string screenId);

        /// <summary>Go back to the previous screen.</summary>
        void GoBack();

        /// <summary>Get the breadcrumb trail (bottom to top).</summary>
        string[] GetBreadcrumbs();

        /// <summary>Fired when a screen is pushed.</summary>
        event Action<string> OnScreenPushed;

        /// <summary>Fired when a screen is popped.</summary>
        event Action<string> OnScreenPopped;
    }
}
