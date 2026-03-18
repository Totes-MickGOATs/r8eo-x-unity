using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug.Tuning
{
    /// <summary>
    /// Tuning section for drivetrain layout, differential type, and rear preload.
    /// Renders two cycle buttons followed by a preload slider.
    /// Falls back to a status label when no Drivetrain component is present.
    /// </summary>
    public sealed class DrivetrainTuningSection : TuningSection
    {
        // ---- Constants ----

        static readonly string[] k_LayoutNames = { "RWD", "AWD" };
        static readonly string[] k_DiffNames   = { "Open", "BallDiff", "Spool" };

        // ---- Fields ----

        private RCCar _car;
        private int _layoutIndex;
        private int _diffIndex;
        private GUIStyle _buttonStyle;

        // ---- Constructor ----

        public DrivetrainTuningSection() : base("DRIVETRAIN") { }

        // ---- TuningSection ----

        public override void Initialize(RCCar car)
        {
            _car = car;
            var dt = car.DrivetrainRef;
            if (dt != null)
            {
                _layoutIndex = (int)dt.ActiveDriveLayout;
                _diffIndex   = (int)dt.RearDiff;
            }

            Sliders = new[]
            {
                new SliderDefinition(
                    "Rear Preload (N)", 0f, 20f,
                    () => _car.DrivetrainRef != null ? _car.DrivetrainRef.RearPreload : 0f,
                    v => { if (_car.DrivetrainRef != null) _car.DrivetrainRef.RearPreload = v; },
                    "F1"),
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

            var dt = _car.DrivetrainRef;
            if (dt == null)
            {
                GUI.Label(new Rect(x, y, panelWidth, lineHeight),
                    "  (no Drivetrain component)", labelStyle);
                y += lineHeight + sectionSpacing;
                return y;
            }

            if (_buttonStyle == null)
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = labelStyle.fontSize };

            // Drive layout cycle
            int oldLayout = _layoutIndex;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "Drive Layout", labelStyle);
            if (GUI.Button(
                    new Rect(x + labelWidth, y, sliderWidth + valueWidth + 4f, lineHeight),
                    k_LayoutNames[_layoutIndex], _buttonStyle))
            {
                _layoutIndex = (_layoutIndex + 1) % k_LayoutNames.Length;
            }
            if (_layoutIndex != oldLayout)
                dt.ActiveDriveLayout = (Drivetrain.DriveLayout)_layoutIndex;
            y += lineHeight;

            // Rear diff cycle
            int oldDiff = _diffIndex;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "Rear Diff Type", labelStyle);
            if (GUI.Button(
                    new Rect(x + labelWidth, y, sliderWidth + valueWidth + 4f, lineHeight),
                    k_DiffNames[_diffIndex], _buttonStyle))
            {
                _diffIndex = (_diffIndex + 1) % k_DiffNames.Length;
            }
            if (_diffIndex != oldDiff)
                dt.RearDiff = (Drivetrain.DiffType)_diffIndex;
            y += lineHeight;

            // Rear preload slider
            y = SliderRenderer.DrawSliderGroup(
                x, y, Sliders, lineHeight,
                labelWidth, sliderWidth, valueWidth,
                labelStyle, valueStyle);

            y += sectionSpacing;
            return y;
        }
    }
}
