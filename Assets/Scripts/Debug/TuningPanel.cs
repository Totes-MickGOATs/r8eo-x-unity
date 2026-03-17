using UnityEngine;
using UnityEngine.InputSystem;
using R8EOX.Vehicle;

namespace R8EOX.Debug
{
    /// <summary>
    /// Runtime tuning panel for live adjustment of all RC buggy parameters.
    /// Toggle visibility with Tab key. Organised into collapsible sections.
    /// Uses OnGUI sliders to push values directly to vehicle components.
    /// </summary>
    public class TuningPanel : MonoBehaviour
    {
        // ---- Constants ----

        const int k_FontSize = 13;
        const int k_HeaderFontSize = 15;
        const float k_LineHeight = 22f;
        const float k_Margin = 10f;
        const float k_PanelWidth = 420f;
        const float k_LabelWidth = 180f;
        const float k_SliderWidth = 160f;
        const float k_ValueWidth = 60f;
        const float k_BackgroundAlpha = 0.85f;
        const float k_SectionSpacing = 6f;
        const float k_HeaderSpacing = 4f;
        const float k_ScrollBarWidth = 16f;
        const int k_MotorPresetCount = 7;


        // ---- Serialized Fields ----

        [Header("Target")]
        [Tooltip("The RC car to tune parameters on")]
        [SerializeField] private RCCar _car;

        [Header("Input")]
        [Tooltip("Action for toggling the tuning panel")]
        [SerializeField] private InputActionReference _toggleAction;

        [Header("Display")]
        [Tooltip("Whether the panel is visible on start")]
        [SerializeField] private bool _showPanel;


        // ---- Private Fields ----

        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private UnityEngine.Vector2 _scrollPosition;
        private float _panelHeight;
        private bool _isInitialised;

        // Section fold states
        private bool _showMotor = true;
        private bool _showThrottle = true;
        private bool _showSteering = true;
        private bool _showSuspension = true;
        private bool _showTraction = true;
        private bool _showCoM = true;
        private bool _showCrash = true;
        private bool _showAir = true;
        private bool _showDrivetrain = true;
        private bool _showScale = true;

        // Cached slider values (synced from car on show)
        private float _engineForce;
        private float _maxSpeed;
        private float _brakeForce;
        private float _reverseForce;
        private float _coastDrag;
        private float _throttleRampUp;
        private float _throttleRampDown;
        private float _steeringMax;
        private float _steeringSpeed;
        private float _steeringSpeedLimit;
        private float _steeringHighSpeedFactor;
        private float _springStrength;
        private float _springDamping;
        private float _gripCoeff;
        private float _comGroundY;
        private float _tumbleEngageDeg;
        private float _tumbleFullDeg;
        private float _tumbleBounce;
        private float _tumbleFriction;
        private float _mass;
        private float _rearPreload;
        private int _motorPresetIndex;
        private int _driveLayoutIndex;
        private int _rearDiffIndex;


        // ---- Unity Lifecycle ----

        void OnEnable()
        {
            if (_toggleAction != null && _toggleAction.action != null)
                _toggleAction.action.Enable();
        }

        void OnDisable()
        {
            if (_toggleAction != null && _toggleAction.action != null)
                _toggleAction.action.Disable();
        }

        void Update()
        {
            if (_toggleAction != null && _toggleAction.action.WasPressedThisFrame())
            {
                _showPanel = !_showPanel;
                if (_showPanel)
                    SyncFromCar();
            }
        }

        void OnGUI()
        {
            if (!_showPanel || _car == null) return;

            InitStyles();

            float screenRight = Screen.width;
            float panelX = screenRight - k_PanelWidth - k_Margin;
            float panelY = k_Margin;
            float maxPanelHeight = Screen.height - 2f * k_Margin;

            // Background
            GUI.color = new UnityEngine.Color(0.05f, 0.05f, 0.1f, k_BackgroundAlpha);
            GUI.DrawTexture(
                new Rect(panelX - 5f, panelY - 5f, k_PanelWidth + 10f, maxPanelHeight + 10f),
                Texture2D.whiteTexture);
            GUI.color = UnityEngine.Color.white;

            // Scroll view
            Rect viewRect = new Rect(panelX, panelY, k_PanelWidth, maxPanelHeight);
            Rect contentRect = new Rect(0f, 0f, k_PanelWidth - k_ScrollBarWidth, _panelHeight);
            _scrollPosition = GUI.BeginScrollView(viewRect, _scrollPosition, contentRect);

            float x = 0f;
            float y = 0f;

            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                "=== TUNING PANEL (Tab to hide) ===", _headerStyle);
            y += k_LineHeight + k_HeaderSpacing;

            y = DrawScaleSection(x, y);
            y = DrawMotorSection(x, y);
            y = DrawThrottleSection(x, y);
            y = DrawSteeringSection(x, y);
            y = DrawSuspensionSection(x, y);
            y = DrawTractionSection(x, y);
            y = DrawCoMSection(x, y);
            y = DrawCrashSection(x, y);
            y = DrawAirPhysicsSection(x, y);
            y = DrawDrivetrainSection(x, y);

            _panelHeight = y + k_LineHeight;

            GUI.EndScrollView();
        }


        // ---- Public API ----

        /// <summary>Syncs all slider values from the car's current state.</summary>
        public void SyncFromCar()
        {
            if (_car == null) return;

            _engineForce = _car.EngineForceMax;
            _maxSpeed = _car.MaxSpeed;
            _brakeForce = _car.BrakeForce;
            _reverseForce = _car.ReverseForce;
            _coastDrag = _car.CoastDrag;
            _throttleRampUp = _car.ThrottleRampUp;
            _throttleRampDown = _car.ThrottleRampDown;
            _steeringMax = _car.SteeringMax;
            _steeringSpeed = _car.SteeringSpeed;
            _steeringSpeedLimit = _car.SteeringSpeedLimit;
            _steeringHighSpeedFactor = _car.SteeringHighSpeedFactor;
            _springStrength = _car.FrontSpringStrength;
            _springDamping = _car.FrontSpringDamping;
            _gripCoeff = _car.GripCoeff;
            _comGroundY = _car.ComGroundY;
            _tumbleEngageDeg = _car.TumbleEngageDeg;
            _tumbleFullDeg = _car.TumbleFullDeg;
            _tumbleBounce = _car.TumbleBounce;
            _tumbleFriction = _car.TumbleFriction;
            _mass = _car.Mass;
            _motorPresetIndex = (int)_car.ActiveMotorPreset;

            var air = _car.AirPhysics;
            if (air != null)
            {
            }

            var dt = _car.DrivetrainRef;
            if (dt != null)
            {
                _driveLayoutIndex = (int)dt.ActiveDriveLayout;
                _rearDiffIndex = (int)dt.RearDiff;
                _rearPreload = dt.RearPreload;
            }
        }


        // ---- Private Methods ----

        private void InitStyles()
        {
            if (_isInitialised) return;
            _isInitialised = true;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = k_FontSize
            };
            _labelStyle.normal.textColor = UnityEngine.Color.white;

            _headerStyle = new GUIStyle(_labelStyle)
            {
                fontSize = k_HeaderFontSize,
                fontStyle = FontStyle.Bold
            };
            _headerStyle.normal.textColor = UnityEngine.Color.yellow;

            _valueStyle = new GUIStyle(_labelStyle)
            {
                alignment = TextAnchor.MiddleRight
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = k_FontSize
            };
        }

        private bool DrawSectionHeader(float x, ref float y, string title, bool isOpen)
        {
            string prefix = isOpen ? "[-]" : "[+]";
            if (GUI.Button(new Rect(x, y, k_PanelWidth - k_ScrollBarWidth, k_LineHeight),
                $"{prefix} {title}", _headerStyle))
            {
                isOpen = !isOpen;
            }
            y += k_LineHeight + k_HeaderSpacing;
            return isOpen;
        }

        private float DrawSlider(float x, float y, string label, float value,
            float min, float max, string format = "F2")
        {
            GUI.Label(new Rect(x, y, k_LabelWidth, k_LineHeight), label, _labelStyle);
            float newVal = GUI.HorizontalSlider(
                new Rect(x + k_LabelWidth, y + 4f, k_SliderWidth, k_LineHeight),
                value, min, max);
            GUI.Label(new Rect(x + k_LabelWidth + k_SliderWidth + 4f, y, k_ValueWidth, k_LineHeight),
                newVal.ToString(format), _valueStyle);
            return newVal;
        }

        private string CycleButton(float x, float y, string label, string[] options, ref int index)
        {
            GUI.Label(new Rect(x, y, k_LabelWidth, k_LineHeight), label, _labelStyle);
            if (GUI.Button(
                new Rect(x + k_LabelWidth, y, k_SliderWidth + k_ValueWidth + 4f, k_LineHeight),
                options[index], _buttonStyle))
            {
                index = (index + 1) % options.Length;
            }
            return options[index];
        }


        // ---- Section Drawers ----

        private float DrawScaleSection(float x, float y)
        {
            _showScale = DrawSectionHeader(x, ref y, "VEHICLE", _showScale);
            if (!_showScale) return y;

            float newMass = DrawSlider(x, y, "Mass (kg)", _mass, 0.5f, 10f);
            if (!Mathf.Approximately(newMass, _mass))
            {
                _mass = newMass;
                _car.SetMass(_mass);
            }
            y += k_LineHeight;

            y += k_SectionSpacing;
            return y;
        }

        private float DrawMotorSection(float x, float y)
        {
            _showMotor = DrawSectionHeader(x, ref y, "MOTOR", _showMotor);
            if (!_showMotor) return y;

            // Motor preset cycle button
            string[] presetNames =
            {
                "21.5T", "17.5T", "13.5T", "9.5T", "5.5T", "1.5T", "Custom"
            };
            int oldPreset = _motorPresetIndex;
            CycleButton(x, y, "Preset", presetNames, ref _motorPresetIndex);
            if (_motorPresetIndex != oldPreset && _motorPresetIndex < k_MotorPresetCount - 1)
            {
                _car.SelectMotorPreset((RCCar.MotorPreset)_motorPresetIndex);
                SyncFromCar();
            }
            y += k_LineHeight;

            float newEngine = DrawSlider(x, y, "Engine Force (N)", _engineForce, 5f, 100f);
            y += k_LineHeight;
            float newMaxSpd = DrawSlider(x, y, "Max Speed (m/s)", _maxSpeed, 5f, 80f);
            y += k_LineHeight;
            float newBrake = DrawSlider(x, y, "Brake Force (N)", _brakeForce, 5f, 80f);
            y += k_LineHeight;
            float newReverse = DrawSlider(x, y, "Reverse Force (N)", _reverseForce, 2f, 50f);
            y += k_LineHeight;
            float newCoast = DrawSlider(x, y, "Coast Drag (N)", _coastDrag, 0f, 10f);
            y += k_LineHeight;

            if (!Mathf.Approximately(newEngine, _engineForce) ||
                !Mathf.Approximately(newMaxSpd, _maxSpeed) ||
                !Mathf.Approximately(newBrake, _brakeForce) ||
                !Mathf.Approximately(newReverse, _reverseForce) ||
                !Mathf.Approximately(newCoast, _coastDrag))
            {
                _engineForce = newEngine;
                _maxSpeed = newMaxSpd;
                _brakeForce = newBrake;
                _reverseForce = newReverse;
                _coastDrag = newCoast;
                _car.SetMotorParams(_engineForce, _maxSpeed, _brakeForce, _reverseForce, _coastDrag);
                _motorPresetIndex = (int)RCCar.MotorPreset.Custom;
            }

            y += k_SectionSpacing;
            return y;
        }

        private float DrawThrottleSection(float x, float y)
        {
            _showThrottle = DrawSectionHeader(x, ref y, "THROTTLE RESPONSE", _showThrottle);
            if (!_showThrottle) return y;

            float newUp = DrawSlider(x, y, "Ramp Up (u/s)", _throttleRampUp, 1f, 20f);
            y += k_LineHeight;
            float newDown = DrawSlider(x, y, "Ramp Down (u/s)", _throttleRampDown, 1f, 30f);
            y += k_LineHeight;

            if (!Mathf.Approximately(newUp, _throttleRampUp) ||
                !Mathf.Approximately(newDown, _throttleRampDown))
            {
                _throttleRampUp = newUp;
                _throttleRampDown = newDown;
                _car.SetThrottleResponse(_throttleRampUp, _throttleRampDown);
            }

            y += k_SectionSpacing;
            return y;
        }

        private float DrawSteeringSection(float x, float y)
        {
            _showSteering = DrawSectionHeader(x, ref y, "STEERING", _showSteering);
            if (!_showSteering) return y;

            float newMax = DrawSlider(x, y, "Max Angle (rad)", _steeringMax, 0.1f, 1.2f);
            y += k_LineHeight;
            float newSpd = DrawSlider(x, y, "Speed (rad/s)", _steeringSpeed, 1f, 15f);
            y += k_LineHeight;
            float newLim = DrawSlider(x, y, "Speed Limit (m/s)", _steeringSpeedLimit, 1f, 30f);
            y += k_LineHeight;
            float newHi = DrawSlider(x, y, "Hi-Speed Factor", _steeringHighSpeedFactor, 0f, 1f);
            y += k_LineHeight;

            if (!Mathf.Approximately(newMax, _steeringMax) ||
                !Mathf.Approximately(newSpd, _steeringSpeed) ||
                !Mathf.Approximately(newLim, _steeringSpeedLimit) ||
                !Mathf.Approximately(newHi, _steeringHighSpeedFactor))
            {
                _steeringMax = newMax;
                _steeringSpeed = newSpd;
                _steeringSpeedLimit = newLim;
                _steeringHighSpeedFactor = newHi;
                _car.SetSteeringParams(_steeringMax, _steeringSpeed,
                    _steeringSpeedLimit, _steeringHighSpeedFactor);
            }

            y += k_SectionSpacing;
            return y;
        }

        private float DrawSuspensionSection(float x, float y)
        {
            _showSuspension = DrawSectionHeader(x, ref y, "SUSPENSION", _showSuspension);
            if (!_showSuspension) return y;

            float newSpring = DrawSlider(x, y, "Spring (N/m)", _springStrength, 10f, 300f);
            y += k_LineHeight;
            float newDamp = DrawSlider(x, y, "Damping", _springDamping, 0.5f, 20f);
            y += k_LineHeight;

            if (!Mathf.Approximately(newSpring, _springStrength) ||
                !Mathf.Approximately(newDamp, _springDamping))
            {
                _springStrength = newSpring;
                _springDamping = newDamp;
                _car.SetSuspension(_springStrength, _springDamping);
            }

            y += k_SectionSpacing;
            return y;
        }

        private float DrawTractionSection(float x, float y)
        {
            _showTraction = DrawSectionHeader(x, ref y, "TRACTION", _showTraction);
            if (!_showTraction) return y;

            float newGrip = DrawSlider(x, y, "Grip Coeff (0-1)", _gripCoeff, 0f, 1f);
            y += k_LineHeight;

            if (!Mathf.Approximately(newGrip, _gripCoeff))
            {
                _gripCoeff = newGrip;
                _car.SetTraction(_gripCoeff);
            }

            y += k_SectionSpacing;
            return y;
        }

        private float DrawCoMSection(float x, float y)
        {
            _showCoM = DrawSectionHeader(x, ref y, "CENTRE OF MASS", _showCoM);
            if (!_showCoM) return y;

            float newGroundY = DrawSlider(x, y, "Ground Y (m)", _comGroundY, -1f, 0.5f);
            y += k_LineHeight;

            if (!Mathf.Approximately(newGroundY, _comGroundY))
            {
                _comGroundY = newGroundY;
                _car.SetCentreOfMass(_comGroundY);
            }

            y += k_SectionSpacing;
            return y;
        }

        private float DrawCrashSection(float x, float y)
        {
            _showCrash = DrawSectionHeader(x, ref y, "CRASH PHYSICS", _showCrash);
            if (!_showCrash) return y;

            float newEngage = DrawSlider(x, y, "Tumble Engage (deg)", _tumbleEngageDeg, 10f, 89f, "F1");
            y += k_LineHeight;
            float newFull = DrawSlider(x, y, "Tumble Full (deg)", _tumbleFullDeg, 20f, 89f, "F1");
            y += k_LineHeight;
            float newBounce = DrawSlider(x, y, "Tumble Bounce", _tumbleBounce, 0f, 1f);
            y += k_LineHeight;
            float newFrict = DrawSlider(x, y, "Tumble Friction", _tumbleFriction, 0f, 1f);
            y += k_LineHeight;

            if (!Mathf.Approximately(newEngage, _tumbleEngageDeg) ||
                !Mathf.Approximately(newFull, _tumbleFullDeg) ||
                !Mathf.Approximately(newBounce, _tumbleBounce) ||
                !Mathf.Approximately(newFrict, _tumbleFriction))
            {
                _tumbleEngageDeg = newEngage;
                _tumbleFullDeg = newFull;
                _tumbleBounce = newBounce;
                _tumbleFriction = newFrict;
                _car.SetCrashParams(_tumbleEngageDeg, _tumbleFullDeg,
                    _tumbleBounce, _tumbleFriction);
            }

            y += k_SectionSpacing;
            return y;
        }

        private float DrawAirPhysicsSection(float x, float y)
        {
            _showAir = DrawSectionHeader(x, ref y, "AIR PHYSICS", _showAir);
            if (!_showAir) return y;

            var air = _car.AirPhysics;
            if (air == null)
            {
                GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                    "  (no RCAirPhysics component)", _labelStyle);
                y += k_LineHeight + k_SectionSpacing;
                return y;
            }

            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                "  Gyroscopic model (tune via WheelInertiaConfig asset)", _labelStyle);
            y += k_LineHeight;

            y += k_SectionSpacing;
            return y;
        }

        private float DrawDrivetrainSection(float x, float y)
        {
            _showDrivetrain = DrawSectionHeader(x, ref y, "DRIVETRAIN", _showDrivetrain);
            if (!_showDrivetrain) return y;

            var dt = _car.DrivetrainRef;
            if (dt == null)
            {
                GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                    "  (no Drivetrain component)", _labelStyle);
                y += k_LineHeight + k_SectionSpacing;
                return y;
            }

            string[] layoutNames = { "RWD", "AWD" };
            int oldLayout = _driveLayoutIndex;
            CycleButton(x, y, "Drive Layout", layoutNames, ref _driveLayoutIndex);
            if (_driveLayoutIndex != oldLayout)
                dt.ActiveDriveLayout = (Drivetrain.DriveLayout)_driveLayoutIndex;
            y += k_LineHeight;

            string[] diffNames = { "Open", "BallDiff", "Spool" };
            int oldDiff = _rearDiffIndex;
            CycleButton(x, y, "Rear Diff Type", diffNames, ref _rearDiffIndex);
            if (_rearDiffIndex != oldDiff)
                dt.RearDiff = (Drivetrain.DiffType)_rearDiffIndex;
            y += k_LineHeight;

            float newPreload = DrawSlider(x, y, "Rear Preload (N)", _rearPreload, 0f, 20f, "F1");
            if (!Mathf.Approximately(newPreload, _rearPreload))
            {
                _rearPreload = newPreload;
                dt.RearPreload = _rearPreload;
            }
            y += k_LineHeight;

            y += k_SectionSpacing;
            return y;
        }
    }
}
