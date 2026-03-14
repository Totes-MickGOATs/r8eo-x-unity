namespace R8EOX.UI
{
    using UnityEngine;

    /// <summary>
    /// Centralized design tokens for the R8EO-X UI. All visual constants
    /// (colors, sizes, timing) live here. Never use magic numbers in UI code.
    /// Ported from Godot's theme_constants.gd.
    /// </summary>
    public static class ThemeConstants
    {
        // ---- Colors ----
        public static readonly Color BgNoir = new(0.05f, 0.06f, 0.08f, 1f);
        public static readonly Color BgSurface = new(0.08f, 0.09f, 0.11f, 1f);
        public static readonly Color BgBlack = new(0f, 0f, 0f, 1f);
        public static readonly Color AccentCyan = new(0f, 0.784f, 1f, 1f);
        public static readonly Color TextWhite = new(0.93f, 0.93f, 0.93f, 1f);
        public static readonly Color TextMuted = new(0.5f, 0.52f, 0.55f, 1f);
        public static readonly Color Cyan50 = new(0f, 0.784f, 1f, 0.5f);
        public static readonly Color Cyan15 = new(0f, 0.784f, 1f, 0.15f);
        public static readonly Color DangerRed = new(1f, 0.3f, 0.3f, 1f);
        public static readonly Color SuccessGreen = new(0.3f, 1f, 0.5f, 1f);
        public static readonly Color WarningYellow = new(1f, 0.85f, 0.3f, 1f);

        // ---- Font Sizes ----
        public const int FontDisplay = 96;
        public const int FontH1 = 64;
        public const int FontH2 = 48;
        public const int FontH3 = 32;
        public const int FontBody = 24;
        public const int FontSmall = 18;
        public const int FontMicro = 12;

        // ---- Animation Durations (seconds) ----
        public const float AnimFast = 0.2f;
        public const float AnimNormal = 0.4f;
        public const float AnimSlow = 0.8f;
        public const float StaggerDelay = 0.08f;

        // ---- Panel ----
        public const int PanelBorder = 2;
        public const int PanelRadius = 8;
        public const int PanelRadiusLg = 16;

        // ---- Canvas Sort Orders ----
        public const int SortOrderBackground = 0;
        public const int SortOrderMenus = 10;
        public const int SortOrderOverlays = 50;
        public const int SortOrderNotifications = 100;
        public const int SortOrderTransitions = 200;
    }
}
