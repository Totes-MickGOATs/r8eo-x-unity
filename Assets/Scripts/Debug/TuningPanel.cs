using UnityEngine;
using UnityEngine.InputSystem;
using R8EOX.Debug.Tuning;
using R8EOX.Vehicle;

namespace R8EOX.Debug
{
    /// <summary>
    /// Runtime tuning panel — Tab to toggle. Organised into collapsible sections.
    /// Pushes values live to vehicle components via OnGUI sliders.
    /// Styles: <see cref="TuningPanelStyles"/>.
    /// </summary>
    public class TuningPanel : MonoBehaviour
    {
        // ---- Constants ----
        const float k_LineHeight = 22f;   const float k_Margin = 10f;
        const float k_PanelWidth = 420f;  const float k_LabelWidth = 180f;
        const float k_SliderWidth = 160f; const float k_ValueWidth = 60f;
        const float k_BackgroundAlpha = 0.85f;
        const float k_SectionSpacing = 6f;
        const float k_HeaderSpacing  = 4f;
        const float k_ScrollBarWidth = 16f;


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

        private TuningPanelStyles _styles;
        private bool _stylesBuilt;
        private UnityEngine.Vector2 _scrollPosition;
        private float _panelHeight;

        private TuningSection[] _sections;


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

            if (!_stylesBuilt) { _styles = TuningPanelStyles.Build(); _stylesBuilt = true; }

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
            Rect viewRect    = new Rect(panelX, panelY, k_PanelWidth, maxPanelHeight);
            Rect contentRect = new Rect(0f, 0f, k_PanelWidth - k_ScrollBarWidth, _panelHeight);
            _scrollPosition  = GUI.BeginScrollView(viewRect, _scrollPosition, contentRect);

            float x = 0f;
            float y = 0f;

            GUI.Label(new Rect(x, y, k_PanelWidth, k_LineHeight),
                "=== TUNING PANEL (Tab to hide) ===", _styles.Header);
            y += k_LineHeight + k_HeaderSpacing;

            if (_sections != null)
            {
                foreach (var section in _sections)
                {
                    y = section.Draw(
                        x, y,
                        k_LineHeight, k_SectionSpacing, k_HeaderSpacing,
                        k_PanelWidth, k_ScrollBarWidth,
                        k_LabelWidth, k_SliderWidth, k_ValueWidth,
                        _styles.Header, _styles.Label, _styles.Value);
                }
            }

            _panelHeight = y + k_LineHeight;
            GUI.EndScrollView();
        }


        // ---- Public API ----

        /// <summary>Initialises or re-initialises all sections from the car's current state.</summary>
        public void SyncFromCar()
        {
            if (_car == null) return;

            if (_sections == null)
                BuildSections();

            foreach (var section in _sections)
                section.Initialize(_car);
        }


        // ---- Private Methods ----

        private void BuildSections()
        {
            _sections = new TuningSection[]
            {
                new VehicleTuningSection(),
                new MotorTuningSection(),
                new SuspensionTuningSection(),
                new SteeringTuningSection(),
                new CrashTuningSection(),
                new DrivetrainTuningSection(),
            };
        }
    }
}
