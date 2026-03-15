using UnityEngine;
using UnityEngine.InputSystem;

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

        private R8EOXInputActions _actions;


        // ---- Unity Lifecycle ----

        void Awake()
        {
            _actions = new R8EOXInputActions();
        }

        void OnEnable()
        {
            _actions.Gameplay.Enable();
        }

        void OnDisable()
        {
            _actions.Gameplay.Disable();
        }

        void OnDestroy()
        {
            _actions?.Dispose();
        }

        void Update()
        {
            Throttle = _actions.Gameplay.Throttle.ReadValue<float>();
            Brake = _actions.Gameplay.Brake.ReadValue<float>();

            float rawSteer = _actions.Gameplay.Steer.ReadValue<float>();
            Steer = InputMath.ApplySteeringCurve(rawSteer, _steerCurveExponent);

            ResetPressed = _actions.Gameplay.Reset.WasPressedThisFrame();
            DebugTogglePressed = _actions.Gameplay.DebugToggle.WasPressedThisFrame();
            CameraCyclePressed = _actions.Gameplay.CameraCycle.WasPressedThisFrame();
            PausePressed = _actions.Gameplay.Pause.WasPressedThisFrame();
        }
    }
}
