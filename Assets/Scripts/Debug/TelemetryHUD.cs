using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug
{
    /// <summary>
    /// On-screen telemetry overlay showing vehicle physics state.
    /// Toggle with F2.
    /// </summary>
    public class TelemetryHUD : MonoBehaviour
    {
        // ---- Constants ----

        const int k_FontSize = 14;
        const int k_HeaderFontSize = 16;
        const float k_LineHeight = 20f;
        const float k_Margin = 10f;
        const float k_PanelWidth = 400f;
        const float k_PanelHeight = 500f;
        const float k_BackgroundAlpha = 0.7f;
        const float k_SectionSpacing = 10f;
        const float k_HeaderSpacing = 5f;


        // ---- Serialized Fields ----

        [Header("Target")]
        [Tooltip("The RC car to display telemetry for")]
        [SerializeField] private RCCar _car;

        [Header("Display")]
        [Tooltip("Whether the HUD is visible")]
        [SerializeField] private bool _showHUD = true;


        // ---- Private Fields ----

        private GUIStyle _style;
        private GUIStyle _headerStyle;


        // ---- Unity Lifecycle ----

        void Start()
        {
            _style = new GUIStyle
            {
                fontSize = k_FontSize,
                font = Font.CreateDynamicFontFromOSFont("Consolas", k_FontSize)
            };
            _style.normal.textColor = Color.white;

            _headerStyle = new GUIStyle(_style)
            {
                fontSize = k_HeaderFontSize
            };
            _headerStyle.normal.textColor = Color.yellow;
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
                _showHUD = !_showHUD;
        }

        void OnGUI()
        {
            if (!_showHUD || _car == null) return;

            float x = k_Margin;
            float y = k_Margin;

            DrawBackground(x, y);

            var rb = _car.GetComponent<Rigidbody>();

            y = DrawVehicleState(x, y, rb);
            y = DrawWheelState(x, y);
            DrawControls(x, y);
        }


        // ---- Private Methods ----

        private void DrawBackground(float x, float y)
        {
            GUI.color = new Color(0f, 0f, 0f, k_BackgroundAlpha);
            GUI.DrawTexture(new Rect(x - 5, y - 5, k_PanelWidth, k_PanelHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private float DrawVehicleState(float x, float y, Rigidbody rb)
        {
            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight), "=== RC BUGGY TELEMETRY ===", _headerStyle);
            y += k_LineHeight + k_HeaderSpacing;

            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                $"Speed: {_car.GetSpeedKmh():F1} km/h  ({rb.velocity.magnitude:F1} m/s)", _style);
            y += k_LineHeight;

            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                $"Fwd Speed: {_car.GetForwardSpeedKmh():F1} km/h", _style);
            y += k_LineHeight;

            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                $"Throttle: {_car.SmoothThrottle:F2}  Engine: {_car.CurrentEngineForce:F1} N", _style);
            y += k_LineHeight;

            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                $"Brake: {_car.CurrentBrakeForce:F1} N  Reverse: {(_car.ReverseEngaged ? "YES" : "no")}", _style);
            y += k_LineHeight;

            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                $"Steering: {_car.CurrentSteering * Mathf.Rad2Deg:F1} deg", _style);
            y += k_LineHeight;

            string airState = _car.IsAirborne ? "AIRBORNE" : "GROUNDED";
            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                $"State: {airState}  Tumble: {_car.TumbleFactor:F2}  Tilt: {_car.TiltAngle:F1} deg", _style);
            y += k_LineHeight + k_SectionSpacing;

            return y;
        }

        private float DrawWheelState(float x, float y)
        {
            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight), "=== WHEELS ===", _headerStyle);
            y += k_LineHeight + k_HeaderSpacing;

            var wheels = _car.GetAllWheels();
            if (wheels == null) return y;

            foreach (var w in wheels)
            {
                string ground = w.IsOnGround ? "GND" : "AIR";
                string motor = w.IsMotor ? "M" : " ";
                string steer = w.IsSteer ? "S" : " ";

                GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                    $"{w.name} [{motor}{steer}] {ground}  " +
                    $"spring={w.LastSpringLen:F3}  slip={w.SlipRatio:F2}  " +
                    $"grip={w.GripFactor:F2}  rpm={w.WheelRpm:F0}", _style);
                y += k_LineHeight;
            }

            y += k_SectionSpacing;
            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                $"Avg Slip: {_car.GetSlip():F3}", _style);
            y += k_LineHeight;

            return y;
        }

        private void DrawControls(float x, float y)
        {
            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                "F2: Toggle HUD  |  R: Flip car  |  WASD: Drive", _style);
        }
    }
}