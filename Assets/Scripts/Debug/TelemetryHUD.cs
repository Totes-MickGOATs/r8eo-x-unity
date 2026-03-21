using UnityEngine;
using UnityEngine.InputSystem;
using R8EOX.Vehicle;

namespace R8EOX.Debug
{
    /// <summary>
    /// On-screen telemetry overlay showing vehicle physics state.
    /// Toggle with F2. String formatting is handled by <see cref="TelemetryHudRenderer"/>.
    /// </summary>
    public class TelemetryHUD : MonoBehaviour
    {
        // ---- Constants ----
        const int   k_FontSize       = 14;
        const int   k_HeaderFontSize = 16;
        const float k_LineHeight     = 20f;
        const float k_Margin         = 10f;
        const float k_PanelWidth     = 400f;
        const float k_PanelHeight    = 500f;
        const float k_BackgroundAlpha = 0.7f;
        const float k_SectionSpacing = 10f;
        const float k_HeaderSpacing  = 5f;


        // ---- Serialized Fields ----

        [Header("Target")]
        [Tooltip("The RC car to display telemetry for")]
        [SerializeField] private RCCar _car;

        [Header("Input")]
        [Tooltip("Action for toggling the HUD")]
        [SerializeField] private InputActionReference _toggleAction;

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

            _headerStyle = new GUIStyle(_style) { fontSize = k_HeaderFontSize };
            _headerStyle.normal.textColor = Color.yellow;
        }
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
                _showHUD = !_showHUD;
        }

        void OnGUI()
        {
            if (!_showHUD || _car == null) return;

            float x = k_Margin;
            float y = k_Margin;

            GUI.color = new Color(0f, 0f, 0f, k_BackgroundAlpha);
            GUI.DrawTexture(new Rect(x - 5, y - 5, k_PanelWidth, k_PanelHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var rb = _car.GetComponent<Rigidbody>();
            y = DrawVehicleState(x, y, rb);
            y = DrawWheelState(x, y);
            DrawControls(x, y);
        }


        // ---- Private Methods ----

        private float DrawVehicleState(float x, float y, Rigidbody rb)
        {
            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight), "=== RC BUGGY TELEMETRY ===", _headerStyle);
            y += k_LineHeight + k_HeaderSpacing;

            foreach (var line in TelemetryHudRenderer.GetVehicleLines(_car, rb))
            {
                GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight), line, _style);
                y += k_LineHeight;
            }

            return y + k_SectionSpacing;
        }

        private float DrawWheelState(float x, float y)
        {
            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight), "=== WHEELS ===", _headerStyle);
            y += k_LineHeight + k_HeaderSpacing;

            foreach (var line in TelemetryHudRenderer.GetWheelLines(_car.GetAllWheels()))
            {
                GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight), line, _style);
                y += k_LineHeight;
            }

            y += k_SectionSpacing;
            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                $"Avg Slip: {_car.GetSlip():F3}", _style);
            return y + k_LineHeight;
        }

        private void DrawControls(float x, float y)
        {
            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                "F2: Toggle HUD  |  R: Flip car  |  WASD: Drive", _style);
        }
    }
}
