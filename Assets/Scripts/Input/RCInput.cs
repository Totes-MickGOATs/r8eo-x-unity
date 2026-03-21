using UnityEngine;

namespace R8EOX.Input
{
    /// <summary>
    /// Player input provider for the RC buggy using Unity's Input System package.
    /// Supports keyboard (WASD) and gamepad (triggers + stick) via the R8EOXInputActions asset.
    /// Implements IVehicleInput for swappable input sources.
    /// </summary>
    public class RCInput : MonoBehaviour, IVehicleInput
    {
        // ---- Serialized Fields ----

        [Header("Steering")]
        [Tooltip("Exponent applied to steering input for non-linear response (1.0=linear, 1.5=default)")]
        [SerializeField] private float _steerCurveExponent = 1.5f;


        // ---- IVehicleInput Properties ----

        /// <inheritdoc/>
        public float Throttle { get; private set; }
        /// <inheritdoc/>
        public float Brake { get; private set; }
        /// <inheritdoc/>
        public float Steer { get; private set; }
        /// <inheritdoc/>
        public bool ResetPressed { get; private set; }
        /// <inheritdoc/>
        public bool DebugTogglePressed { get; private set; }
        /// <inheritdoc/>
        public bool CameraCyclePressed { get; private set; }
        /// <inheritdoc/>
        public bool PausePressed { get; private set; }


        // ---- Private Fields ----

        private InputBridge _input;


        // ---- Unity Lifecycle ----

        void Awake()
        {
            _input = new InputBridge(new R8EOXInputActions());
        }

        void OnEnable()
        {
            _input.Enable();
        }

        void OnDisable()
        {
            _input.Disable();
        }

        void OnDestroy()
        {
            _input?.Dispose();
        }

        void Update()
        {
            Throttle = _input.Throttle;
            Brake    = _input.Brake;

            float rawSteer = _input.Steer;
            Steer = InputMath.ApplySteeringCurve(rawSteer, _steerCurveExponent);

            ResetPressed       = _input.WasResetPressedThisFrame;
            DebugTogglePressed = _input.WasDebugTogglePressedThisFrame;
            CameraCyclePressed = _input.WasCameraCyclePressedThisFrame;
            PausePressed       = _input.WasPausePressedThisFrame;
        }
    }
}
