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

        /// <summary>Number of startup frames to skip for trigger detection (C5 fix).</summary>
        const int k_GraceFrames = 60;
        /// <summary>Consecutive strong-input frames required to lock trigger mode (C5 fix).</summary>
        const int k_ConfirmFrames = 5;


        // ---- Serialized Fields ----

        [Header("Steering")]
        [Tooltip("Exponent applied to steering input for non-linear response (1.0=linear, 1.5=default)")]
        [SerializeField] private float _steerCurveExponent = 1.5f;

        [Header("Gamepad Deadzones")]
        [Tooltip("Deadzone for trigger axes — values below this are treated as 0")]
        [SerializeField] private float _triggerDeadzone = 0.15f;
        [Tooltip("Deadzone for steering stick (M4 fix: increased from 0.1 to 0.2)")]
        [SerializeField] private float _steerDeadzone = 0.2f;


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

        private TriggerDetector _triggerDetector;

        /// <summary>Lazy accessor ensures detector is never null, even if accessed before Awake.</summary>
        private TriggerDetector Detector =>
            _triggerDetector ??= new TriggerDetector(k_GraceFrames, k_ConfirmFrames);


        // ---- Unity Lifecycle ----

        void Awake()
        {
            _triggerDetector ??= new TriggerDetector(k_GraceFrames, k_ConfirmFrames);
        }

        void Update()
        {
            // M3: Suppress all input during startup grace period
            if (InputGuard.ShouldSuppressInput(Time.frameCount))
            {
                Throttle = 0f;
                Brake = 0f;
                Steer = 0f;
                ResetPressed = false;
                DebugTogglePressed = false;
                CameraCyclePressed = false;
                return;
            }

            PollThrottle();
            PollBrake();
            PollSteering();
            PollButtons();

            if (Detector.CurrentMode == TriggerDetector.Mode.Detecting)
            {
                DetectTriggerMode();
            }
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

            // M4: Use symmetric deadzone that preserves sign
            // Phantom input fix: only read gamepad steering when a gamepad is actually detected.
            // When TriggerDetector.Mode is None (no gamepad) or Detecting (not yet confirmed),
            // gamepad steering is forced to 0 to prevent phantom Horizontal axis values.
            var mode = Detector.CurrentMode;
            bool gamepadDetected = mode == TriggerDetector.Mode.Separate ||
                                   mode == TriggerDetector.Mode.Combined;
            float gp = InputMath.FilterGamepadSteering(
                UnityEngine.Input.GetAxisRaw("Horizontal"), _steerDeadzone, gamepadDetected);

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
            float sepRT = Mathf.Abs(UnityEngine.Input.GetAxisRaw("RightTrigger"));
            float sepLT = Mathf.Abs(UnityEngine.Input.GetAxisRaw("LeftTrigger"));
            float combined = Mathf.Abs(UnityEngine.Input.GetAxisRaw("CombinedTriggers"));

            Detector.ProcessFrame(sepRT, sepLT, combined, Time.frameCount);

            if (Detector.CurrentMode == TriggerDetector.Mode.Separate)
                UnityEngine.Debug.Log("[RCInput] Gamepad triggers: separate axes (9/10)");
            else if (Detector.CurrentMode == TriggerDetector.Mode.Combined)
                UnityEngine.Debug.Log("[RCInput] Gamepad triggers: combined axis (3)");
            else if (Detector.CurrentMode == TriggerDetector.Mode.None)
                UnityEngine.Debug.Log("[RCInput] No gamepad triggers detected — keyboard only.");
        }

        private float GetGamepadThrottle()
        {
            var mode = Detector.CurrentMode;
            const float strongThreshold = 0.3f;

            switch (mode)
            {
                case TriggerDetector.Mode.Separate:
                    return InputMath.ApplyDeadzone(
                        UnityEngine.Input.GetAxisRaw("RightTrigger"), _triggerDeadzone);

                case TriggerDetector.Mode.Combined:
                    return InputMath.CombinedTriggerThrottle(
                        UnityEngine.Input.GetAxisRaw("CombinedTriggers"), _triggerDeadzone);

                case TriggerDetector.Mode.Detecting:
                    // Don't produce input during detection — phantom axis values
                    // on Windows cause false throttle/brake while detecting.
                    // Detection only observes axes to determine the mode.
                    return 0f;

                default:
                    return 0f;
            }
        }

        private float GetGamepadBrake()
        {
            var mode = Detector.CurrentMode;

            switch (mode)
            {
                case TriggerDetector.Mode.Separate:
                    return InputMath.ApplyDeadzone(
                        UnityEngine.Input.GetAxisRaw("LeftTrigger"), _triggerDeadzone);

                case TriggerDetector.Mode.Combined:
                    return InputMath.CombinedTriggerBrake(
                        UnityEngine.Input.GetAxisRaw("CombinedTriggers"), _triggerDeadzone);

                case TriggerDetector.Mode.Detecting:
                    // Don't produce input during detection — see GetGamepadThrottle comment.
                    return 0f;

                default:
                    return 0f;
            }
        }
    }
}
