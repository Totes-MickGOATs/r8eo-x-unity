using UnityEngine;

namespace R8EOX.Input
{
    /// <summary>
    /// Player input provider for the RC buggy using Unity's legacy Input Manager.
    /// Supports keyboard (WASD) and gamepad (triggers + stick) with auto-detection.
    /// Implements IVehicleInput for swappable input sources.
    /// </summary>
    public class RCInput : MonoBehaviour, IVehicleInput
    {
        // ---- Constants ----

        const int k_DetectWindow = 300;
        const float k_StrongInputThreshold = 0.3f;


        // ---- Serialized Fields ----

        [Header("Steering")]
        [Tooltip("Exponent applied to steering input for non-linear response (1.0=linear, 1.5=default)")]
        [SerializeField] private float _steerCurveExponent = 1.5f;

        [Header("Gamepad Deadzones")]
        [Tooltip("Deadzone for trigger axes — values below this are treated as 0")]
        [SerializeField] private float _triggerDeadzone = 0.15f;
        [Tooltip("Deadzone for steering stick")]
        [SerializeField] private float _steerDeadzone = 0.1f;


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


        // ---- Private Fields ----

        private enum TriggerMode { Detecting, Separate, Combined, None }
        private TriggerMode _triggerMode = TriggerMode.Detecting;
        private int _detectFrames;


        // ---- Unity Lifecycle ----

        void Update()
        {
            PollThrottle();
            PollBrake();
            PollSteering();
            PollButtons();

            if (_triggerMode == TriggerMode.Detecting)
                DetectTriggerMode();
        }


        // ---- Private Methods ----

        private void PollThrottle()
        {
            float kb = UnityEngine.Input.GetKey(KeyCode.W) ? 1f : 0f;
            float gp = GetGamepadThrottle();
            Throttle = Mathf.Max(kb, gp);
        }

        private void PollBrake()
        {
            float kb = UnityEngine.Input.GetKey(KeyCode.S) ? 1f : 0f;
            float gp = GetGamepadBrake();
            Brake = Mathf.Max(kb, gp);
        }

        private void PollSteering()
        {
            float kb = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) kb += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) kb -= 1f;

            float gp = UnityEngine.Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(gp) < _steerDeadzone) gp = 0f;

            float raw = InputMath.MergeInputs(gp, kb);
            Steer = InputMath.ApplySteeringCurve(raw, _steerCurveExponent);
        }

        private void PollButtons()
        {
            ResetPressed = UnityEngine.Input.GetKeyDown(KeyCode.R) ||
                           UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton3);
            DebugTogglePressed = UnityEngine.Input.GetKeyDown(KeyCode.F2);
            CameraCyclePressed = UnityEngine.Input.GetKeyDown(KeyCode.C) ||
                                 UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton4);
        }

        private void DetectTriggerMode()
        {
            _detectFrames++;

            float sepRT = Mathf.Abs(UnityEngine.Input.GetAxisRaw("RightTrigger"));
            float sepLT = Mathf.Abs(UnityEngine.Input.GetAxisRaw("LeftTrigger"));
            float combined = Mathf.Abs(UnityEngine.Input.GetAxisRaw("CombinedTriggers"));

            if (sepRT > k_StrongInputThreshold || sepLT > k_StrongInputThreshold)
            {
                _triggerMode = TriggerMode.Separate;
                Debug.Log("[RCInput] Gamepad triggers: separate axes (9/10)");
                return;
            }

            if (combined > k_StrongInputThreshold)
            {
                _triggerMode = TriggerMode.Combined;
                Debug.Log("[RCInput] Gamepad triggers: combined axis (3)");
                return;
            }

            if (_detectFrames >= k_DetectWindow)
            {
                _triggerMode = TriggerMode.None;
                Debug.Log("[RCInput] No gamepad triggers detected — keyboard only.");
            }
        }

        private float GetGamepadThrottle()
        {
            switch (_triggerMode)
            {
                case TriggerMode.Separate:
                    return InputMath.ApplyDeadzone(
                        UnityEngine.Input.GetAxisRaw("RightTrigger"), _triggerDeadzone);

                case TriggerMode.Combined:
                    return InputMath.ApplyDeadzone(
                        UnityEngine.Input.GetAxisRaw("CombinedTriggers"), _triggerDeadzone);

                case TriggerMode.Detecting:
                    float rt = UnityEngine.Input.GetAxisRaw("RightTrigger");
                    if (rt > k_StrongInputThreshold)
                        return InputMath.ApplyDeadzone(rt, _triggerDeadzone);
                    float com = UnityEngine.Input.GetAxisRaw("CombinedTriggers");
                    if (com > k_StrongInputThreshold)
                        return InputMath.ApplyDeadzone(com, _triggerDeadzone);
                    return 0f;

                default:
                    return 0f;
            }
        }

        private float GetGamepadBrake()
        {
            switch (_triggerMode)
            {
                case TriggerMode.Separate:
                    return InputMath.ApplyDeadzone(
                        UnityEngine.Input.GetAxisRaw("LeftTrigger"), _triggerDeadzone);

                case TriggerMode.Combined:
                    return InputMath.ApplyDeadzone(
                        -UnityEngine.Input.GetAxisRaw("CombinedTriggers"), _triggerDeadzone);

                case TriggerMode.Detecting:
                    float lt = UnityEngine.Input.GetAxisRaw("LeftTrigger");
                    if (lt > k_StrongInputThreshold)
                        return InputMath.ApplyDeadzone(lt, _triggerDeadzone);
                    float com = -UnityEngine.Input.GetAxisRaw("CombinedTriggers");
                    if (com > k_StrongInputThreshold)
                        return InputMath.ApplyDeadzone(com, _triggerDeadzone);
                    return 0f;

                default:
                    return 0f;
            }
        }
    }
}
