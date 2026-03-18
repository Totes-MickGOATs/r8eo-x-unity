using System;

namespace R8EOX.Debug.Tuning
{
    /// <summary>
    /// Data descriptor for a single runtime tuning slider.
    /// Binds a label, range, format, and get/set delegates to a vehicle parameter.
    /// </summary>
    public struct SliderDefinition
    {
        public string Label;
        public float Min;
        public float Max;
        public string Format;
        public Func<float> Getter;
        public Action<float> Setter;

        public SliderDefinition(
            string label,
            float min,
            float max,
            Func<float> getter,
            Action<float> setter,
            string format = "F2")
        {
            Label = label;
            Min = min;
            Max = max;
            Getter = getter;
            Setter = setter;
            Format = format;
        }
    }
}
