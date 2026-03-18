using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug.Tuning
{
    /// <summary>
    /// Tuning section for motor parameters.
    /// Includes a preset cycle button followed by five sliders for manual override.
    /// </summary>
    public sealed class MotorTuningSection : TuningSection
    {
        // ---- Constants ----

        const int k_MotorPresetCount = 7;

        // ---- Fields ----

        static readonly string[] k_PresetNames =
            { "21.5T", "17.5T", "13.5T", "9.5T", "5.5T", "1.5T", "Custom" };

        private RCCar _car;
        private int _presetIndex;
        private GUIStyle _buttonStyle;

        // ---- Constructor ----

        public MotorTuningSection() : base("MOTOR") { }

        // ---- TuningSection ----

        public override void Initialize(RCCar car)
        {
            _car = car;
            _presetIndex = (int)car.ActiveMotorPreset;

            Sliders = new[]
            {
                new SliderDefinition("Engine Force (N)", 5f,  500f, () => _car.EngineForceMax, v => _car.SetMotorParams(v, _car.MaxSpeed, _car.BrakeForce, _car.ReverseForce, _car.CoastDrag)),
                new SliderDefinition("Max Speed (m/s)",  5f,   80f, () => _car.MaxSpeed,       v => _car.SetMotorParams(_car.EngineForceMax, v, _car.BrakeForce, _car.ReverseForce, _car.CoastDrag)),
                new SliderDefinition("Brake Force (N)",  5f,  500f, () => _car.BrakeForce,     v => _car.SetMotorParams(_car.EngineForceMax, _car.MaxSpeed, v, _car.ReverseForce, _car.CoastDrag)),
                new SliderDefinition("Reverse Force (N)", 2f, 300f, () => _car.ReverseForce,   v => _car.SetMotorParams(_car.EngineForceMax, _car.MaxSpeed, _car.BrakeForce, v, _car.CoastDrag)),
                new SliderDefinition("Coast Drag (N)",   0f,   60f, () => _car.CoastDrag,      v => _car.SetMotorParams(_car.EngineForceMax, _car.MaxSpeed, _car.BrakeForce, _car.ReverseForce, v)),
            };
        }

        public override float Draw(
            float x, float y,
            float lineHeight, float sectionSpacing, float headerSpacing,
            float panelWidth, float scrollBarWidth,
            float labelWidth, float sliderWidth, float valueWidth,
            GUIStyle headerStyle, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            // Fold header
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

            // Ensure button style exists
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = labelStyle.fontSize };
            }

            // Preset cycle button
            int oldPreset = _presetIndex;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "Preset", labelStyle);
            if (GUI.Button(
                    new Rect(x + labelWidth, y, sliderWidth + valueWidth + 4f, lineHeight),
                    k_PresetNames[_presetIndex], _buttonStyle))
            {
                _presetIndex = (_presetIndex + 1) % k_MotorPresetCount;
            }
            if (_presetIndex != oldPreset && _presetIndex < k_MotorPresetCount - 1)
            {
                _car.SelectMotorPreset((RCCar.MotorPreset)_presetIndex);
            }
            y += lineHeight;

            // Sliders
            y = SliderRenderer.DrawSliderGroup(
                x, y, Sliders, lineHeight,
                labelWidth, sliderWidth, valueWidth,
                labelStyle, valueStyle);

            y += sectionSpacing;
            return y;
        }
    }
}
