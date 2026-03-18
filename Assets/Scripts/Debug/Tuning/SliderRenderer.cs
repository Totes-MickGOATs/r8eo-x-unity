using UnityEngine;

namespace R8EOX.Debug.Tuning
{
    /// <summary>
    /// Stateless utility for rendering slider rows and groups via UnityEngine.GUI.
    /// All layout parameters are passed explicitly — no global state.
    /// </summary>
    public static class SliderRenderer
    {
        /// <summary>
        /// Draws a single labelled slider row and returns the (possibly new) value.
        /// </summary>
        public static float DrawSlider(
            float x, float y,
            string label, float value, float min, float max,
            float labelWidth, float sliderWidth, float valueWidth, float lineHeight,
            GUIStyle labelStyle, GUIStyle valueStyle,
            string format = "F2")
        {
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), label, labelStyle);
            float newVal = GUI.HorizontalSlider(
                new Rect(x + labelWidth, y + 4f, sliderWidth, lineHeight),
                value, min, max);
            GUI.Label(
                new Rect(x + labelWidth + sliderWidth + 4f, y, valueWidth, lineHeight),
                newVal.ToString(format), valueStyle);
            return newVal;
        }

        /// <summary>
        /// Draws a group of sliders defined by <paramref name="sliders"/>.
        /// Calls each definition's Setter when the value changes.
        /// Returns the y position after the last row.
        /// </summary>
        public static float DrawSliderGroup(
            float x, float y,
            SliderDefinition[] sliders,
            float lineHeight,
            float labelWidth, float sliderWidth, float valueWidth,
            GUIStyle labelStyle, GUIStyle valueStyle)
        {
            for (int i = 0; i < sliders.Length; i++)
            {
                var s = sliders[i];
                float current = s.Getter();
                float newVal = DrawSlider(
                    x, y, s.Label, current, s.Min, s.Max,
                    labelWidth, sliderWidth, valueWidth, lineHeight,
                    labelStyle, valueStyle, s.Format);

                if (!Mathf.Approximately(newVal, current))
                    s.Setter(newVal);

                y += lineHeight;
            }
            return y;
        }
    }
}
