using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug.Tuning
{
    /// <summary>
    /// Base class for a collapsible tuning section rendered in TuningPanel.
    /// Subclasses populate <see cref="Sliders"/> in <see cref="Initialize"/> and may
    /// override <see cref="Draw"/> for sections that need buttons as well as sliders.
    /// </summary>
    public abstract class TuningSection
    {
        // ---- Constants ----

        const float k_HeaderSpacingDefault = 4f;

        // ---- Properties ----

        public string Title { get; }
        public bool IsFolded { get; set; }

        protected SliderDefinition[] Sliders { get; set; }

        // ---- Constructor ----

        protected TuningSection(string title, bool defaultOpen = true)
        {
            Title = title;
            IsFolded = !defaultOpen;
            Sliders = System.Array.Empty<SliderDefinition>();
        }

        // ---- Public API ----

        /// <summary>Bind slider delegates to the given car instance.</summary>
        public abstract void Initialize(RCCar car);

        /// <summary>
        /// Renders the fold header and, if unfolded, all sliders.
        /// Returns the y position after the section (including section spacing).
        /// </summary>
        public virtual float Draw(
            float x, float y,
            float lineHeight, float sectionSpacing, float headerSpacing,
            float panelWidth, float scrollBarWidth,
            float labelWidth, float sliderWidth, float valueWidth,
            GUIStyle headerStyle, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            // Fold toggle header
            string prefix = IsFolded ? "[+]" : "[-]";
            if (GUI.Button(
                    new Rect(x, y, panelWidth - scrollBarWidth, lineHeight),
                    $"{prefix} {Title}", headerStyle))
            {
                IsFolded = !IsFolded;
            }
            y += lineHeight + headerSpacing;

            if (IsFolded)
                return y;

            // Slider rows
            y = SliderRenderer.DrawSliderGroup(
                x, y, Sliders, lineHeight,
                labelWidth, sliderWidth, valueWidth,
                labelStyle, valueStyle);

            y += sectionSpacing;
            return y;
        }
    }
}
