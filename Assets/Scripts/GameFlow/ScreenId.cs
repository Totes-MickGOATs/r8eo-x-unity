namespace R8EOX.GameFlow
{
    /// <summary>
    /// Well-known screen identifiers. Use these constants instead of magic strings.
    /// New screens: add a constant here, then register in UIManager.
    /// </summary>
    public static class ScreenId
    {
        public const string Splash = "splash";
        public const string MainMenu = "main_menu";
        public const string ModeSelect = "mode_select";
        public const string CarSelect = "car_select";
        public const string TrackSelect = "track_select";
        public const string Loading = "loading";
        public const string Pause = "pause";
        public const string Options = "options";
        public const string Results = "results";
    }
}
