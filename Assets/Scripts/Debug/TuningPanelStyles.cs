using UnityEngine;

namespace R8EOX.Debug
{
    /// <summary>
    /// Immutable GUIStyle bundle for <see cref="TuningPanel"/>.
    /// Built once on first draw via <see cref="Build"/> and cached by the panel.
    /// </summary>
    public readonly struct TuningPanelStyles
    {
        // ---- Constants ----

        public const int k_FontSize       = 13;
        public const int k_HeaderFontSize = 15;

        // ---- Fields ----

        /// <summary>Standard label style — white, 13pt.</summary>
        public readonly GUIStyle Label;

        /// <summary>Section header style — yellow, 15pt, bold.</summary>
        public readonly GUIStyle Header;

        /// <summary>Right-aligned value display style.</summary>
        public readonly GUIStyle Value;

        // ---- Constructor ----

        private TuningPanelStyles(GUIStyle label, GUIStyle header, GUIStyle value)
        {
            Label  = label;
            Header = header;
            Value  = value;
        }

        // ---- Factory ----

        /// <summary>Creates a new <see cref="TuningPanelStyles"/> from the current GUI skin.</summary>
        public static TuningPanelStyles Build()
        {
            var label = new GUIStyle(GUI.skin.label) { fontSize = k_FontSize };
            label.normal.textColor = Color.white;

            var header = new GUIStyle(label) { fontSize = k_HeaderFontSize, fontStyle = FontStyle.Bold };
            header.normal.textColor = Color.yellow;

            var value = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };

            return new TuningPanelStyles(label, header, value);
        }
    }
}
