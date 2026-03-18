using UnityEngine;
using R8EOX.Vehicle;
using R8EOX.Debug.Diagnostics;

namespace R8EOX.Debug
{
    /// <summary>
    /// Runtime diagnostics for detecting terrain snags, contact anomalies, and suspension spikes.
    /// Logs tagged messages ([physics], [suspension]) that DebugLogSink captures to SQLite.
    /// Attach to the same GameObject as <see cref="RCCar"/> or any child with access to wheels.
    /// All detection logic lives in <see cref="TerrainDiagnosticChecks"/>; this class owns
    /// serialised thresholds and the Unity lifecycle.
    /// </summary>
    public class WheelTerrainDiagnostics : MonoBehaviour
    {
        // ---- Serialized Fields ----

        [Header("Target")]
        [Tooltip("The RC car to monitor. Auto-detected from this or parent if unset.")]
        [SerializeField] private RCCar _car;

        [Header("Normal Deviation")]
        [Tooltip("Contact normal Y below this triggers a steep-normal warning (1.0 = flat, 0.0 = vertical)")]
        [SerializeField] private float _normalDeviationThreshold = 0.85f;

        [Header("Suspension Force")]
        [Tooltip("Frame-over-frame suspension force delta (in Newtons) that triggers a spike warning")]
        [SerializeField] private float _forceSpikeThreshold = 30f;

        [Header("Velocity")]
        [Tooltip("Tire velocity magnitude change (m/s per frame) that triggers a discontinuity warning")]
        [SerializeField] private float _velocityDiscontinuityThreshold = 2f;

        [Header("Contact Point")]
        [Tooltip("Contact point jump distance (metres per frame) that triggers a warning while grounded")]
        [SerializeField] private float _contactJumpThreshold = 0.05f;

        [Header("Ground Flicker")]
        [Tooltip("Number of physics frames in the sliding window for flicker detection")]
        [SerializeField] private int _flickerWindowFrames = 10;

        [Tooltip("Number of ground-state toggles within the window that triggers a flicker warning")]
        [SerializeField] private int _flickerCountThreshold = 3;

        [Header("Log Throttle")]
        [Tooltip("Minimum seconds between logs of the same type per wheel to prevent spam")]
        [SerializeField] private float _logCooldownSeconds = 0.5f;

        [Header("Scene Debug Drawing")]
        [Tooltip("Draw contact normals in Scene view (green=normal, yellow=steep, red=very steep)")]
        [SerializeField] private bool _drawContactNormals = true;


        // ---- Private Fields ----

        private RaycastWheel[] _wheels;
        private TerrainDiagnosticChecks.WheelState[] _states;


        // ---- Unity Lifecycle ----

        void Awake()
        {
            if (_car == null)
                _car = GetComponentInParent<RCCar>();

            if (_car == null)
            {
                UnityEngine.Debug.LogError(
                    $"[physics] {nameof(WheelTerrainDiagnostics)} could not find RCCar — disabling.");
                enabled = false;
                return;
            }
        }

        void Start()
        {
            _wheels = _car.GetAllWheels();
            if (_wheels == null || _wheels.Length == 0)
            {
                UnityEngine.Debug.LogError(
                    $"[physics] {nameof(WheelTerrainDiagnostics)} found no wheels on {_car.name} — disabling.");
                enabled = false;
                return;
            }

            InitStates();
            TerrainDiagnosticChecks.CheckCollisionDetectionMode(
                _car.GetComponent<Rigidbody>(), _car.name);
            TerrainDiagnosticChecks.CheckTerrainCollider(highResThreshold: 513);
        }

        void FixedUpdate()
        {
            if (_wheels == null) return;

            for (int i = 0; i < _wheels.Length; i++)
            {
                var wheel = _wheels[i];
                if (wheel == null) continue;

                ref TerrainDiagnosticChecks.WheelState state = ref _states[i];

                if (wheel.IsOnGround)
                {
                    TerrainDiagnosticChecks.CheckNormalDeviation(
                        wheel, ref state, _normalDeviationThreshold, _logCooldownSeconds);
                    TerrainDiagnosticChecks.CheckForceSpikeDetection(
                        wheel, ref state, _forceSpikeThreshold, _logCooldownSeconds);
                    TerrainDiagnosticChecks.CheckVelocityDiscontinuity(
                        wheel, ref state, _velocityDiscontinuityThreshold, _logCooldownSeconds);
                    TerrainDiagnosticChecks.CheckContactPointJump(
                        wheel, ref state, _contactJumpThreshold, _logCooldownSeconds);

                    if (_drawContactNormals)
                        TerrainDiagnosticChecks.DrawContactNormal(
                            wheel, _normalDeviationThreshold, steepThreshold: 0.7f);
                }

                TerrainDiagnosticChecks.CheckGroundFlicker(
                    wheel, ref state, _flickerWindowFrames, _flickerCountThreshold, _logCooldownSeconds);

                // Store current frame as previous for next frame
                state.PrevSuspensionForce = wheel.SuspensionForce;
                state.PrevTireVelocity = wheel.TireVelocity;
                state.PrevContactPoint = wheel.ContactPoint;
                state.PrevIsOnGround = wheel.IsOnGround;
            }
        }


        // ---- Private Helpers ----

        private void InitStates()
        {
            _states = new TerrainDiagnosticChecks.WheelState[_wheels.Length];
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i] = new TerrainDiagnosticChecks.WheelState
                {
                    GroundHistory = new bool[_flickerWindowFrames],
                    GroundHistoryIndex = 0
                };
            }
        }
    }
}
