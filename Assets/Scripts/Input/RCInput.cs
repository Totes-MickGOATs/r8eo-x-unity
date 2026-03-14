using UnityEngine;

namespace R8EOX.Input
{
    /// <summary>
    /// Player input provider for the RC buggy using Unity's legacy Input Manager.
    /// Supports keyboard (WASD) and gamepad (separate triggers + left stick).
    /// Implements IVehicleInput for swappable input sources.
    ///
    /// Controller detection: defaults to separate triggers (modern Xbox/PS controllers).
    /// A 60-frame grace period suppresses gamepad axes on startup to avoid ghost inputs
    /// from uninitialized axis values. After the grace period, if no trigger input is
    /// detected within 5 seconds, falls back to keyboard-only mode.
    ///
    /// No "combined axis" mode — it was a source of ghost brake inputs (see audit C5).
    /// </summary>
    public class RCInput : MonoBehaviour, IVehicleInput
    {
        // ---- Constants ----

        /// <summary>Number of frames to suppress gamepad input on startup.</summary>
        const int k_GraceFrames = 60;

        /// <summary>Frames to wait for trigger input before falling back to keyboard-only.</summary>
        const int k_DetectWindow = 300;

        /// <summary>Minimum input magnitude to confirm a real trigger press.</summary>
        const float k_StrongInputThreshold = 0.3f;

        /// <summary>Default deadzone for gamepad stick axes.</summary>
        const float k_DefaultStickDeadzone = 0.2f;

        /// <summary>Default deadzone for gamepad trigger axes.</summary>
        const float k_DefaultTriggerDeadzone = 0.2f;


        // ---- Serialized Fields ----

        [Header("Steering")]
        [Tooltip("Exponent applied to steering input for non-linear response (1.0=linear, 1.5=default)")]
        [SerializeField] private float _steerCurveExponent = 1.5f;

        [Header("Gamepad Deadzones")]
        [Tooltip("Deadzone for trigger axes — values below this are treated as 0")]
        [SerializeField] private float _triggerDeadzone = k_DefaultTriggerDeadzone;

        [Tooltip("Deadzone for steering stick — values below this are treated as 0")]
        [SerializeField] private float _steerDeadzone = k_DefaultStickDeadzone;


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

        private enum TriggerMode { Grace, Detecting, Active, KeyboardOnly }
        private TriggerMode _triggerMode = TriggerMode.Grace;
        private int _frameCounter;


        // ---- Unity Lifecycle ----

        void Update()
        {
            _frameCounter++;
            UpdateTriggerMode();

            PollThrottle();
            PollBrake();
            PollSteering();
            PollButtons();
        }


        // ---- Private Methods ----

        private void UpdateTriggerMode()
        {
            switch (_triggerMode)
            {
                case TriggerMode.Grace:
                    if (_frameCounter >= k_GraceFrames)
                    {
                        _triggerMode = TriggerMode.Detecting;
                        _frameCounter = 0;
                        UnityEngine.Debug.Log("[RCInput] Grace period ended, detecting gamepad triggers...");
                    }
                    break;

                case TriggerMode.Detecting:
                    float rt = Mathf.Abs(UnityEngine.Input.GetAxisRaw("GamepadThrottle"));
                    float lt = Mathf.Abs(UnityEngine.Input.GetAxisRaw("GamepadBrake"));

                    if (rt > k_StrongInputThreshold || lt > k_StrongInputThreshold)
                    {
                        _triggerMode = TriggerMode.Active;
                        UnityEngine.Debug.Log("[RCInput] Gamepad triggers detected — controller active.");
                        return;
                    }

                    if (_frameCounter >= k_DetectWindow)
                    {
                        _triggerMode = TriggerMode.KeyboardOnly;
                        UnityEngine.Debug.Log("[RCInput] No gamepad triggers detected — keyboard only.");
                    }
                    break;
            }
        }

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

            float gp = GetGamepadSteering();

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

        private float GetGamepadThrottle()
        {
            if (_triggerMode == TriggerMode.Grace || _triggerMode == TriggerMode.KeyboardOnly)
                return 0f;

            float raw = UnityEngine.Input.GetAxisRaw("GamepadThrottle");
            return InputMath.ApplyDeadzone(raw, _triggerDeadzone);
        }

        private float GetGamepadBrake()
        {
            if (_triggerMode == TriggerMode.Grace || _triggerMode == TriggerMode.KeyboardOnly)
                return 0f;

            float raw = UnityEngine.Input.GetAxisRaw("GamepadBrake");
            return InputMath.ApplyDeadzone(raw, _triggerDeadzone);
        }

        private float GetGamepadSteering()
        {
            if (_triggerMode == TriggerMode.Grace || _triggerMode == TriggerMode.KeyboardOnly)
                return 0f;

            float raw = UnityEngine.Input.GetAxisRaw("GamepadSteerX");
            return InputMath.ApplySymmetricDeadzone(raw, _steerDeadzone);
        }
    }
}
