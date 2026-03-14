using UnityEngine;

namespace R8EOX.Input
{
    /// <summary>
    /// Input abstraction for the RC buggy. Uses legacy Input Manager.
    /// Keyboard works immediately. Gamepad triggers use three axis definitions:
    ///   - "RightTrigger" (axis 10) and "LeftTrigger" (axis 9) for separate triggers
    ///   - "CombinedTriggers" (axis 3) as fallback for combined LT/RT axis
    /// </summary>
    public class RCInput : MonoBehaviour
    {
        // ---- Constants ----

        const int k_DetectWindow = 300; // ~5 sec at 60fps
        const float k_StrongInputThreshold = 0.3f;


        // ---- Serialized Fields ----

        [Header("Steering")]
        [Tooltip("Exponent applied to steering input for non-linear response")]
        [SerializeField] private float _steerCurveExponent = 1.5f;

        [Header("Gamepad")]
        [Tooltip("Deadzone for trigger axes — values below this are treated as 0")]
        [SerializeField] private float _triggerDeadzone = 0.15f;
        [Tooltip("Deadzone for steering stick")]
        [SerializeField] private float _steerDeadzone = 0.1f;


        // ---- Public Properties ----

        /// <summary>Throttle input value (0-1).</summary>
        public float Throttle { get; private set; }
        /// <summary>Brake input value (0-1).</summary>
        public float Brake { get; private set; }
        /// <summary>Steering input value (-1 to +1) with curve applied.</summary>
        public float Steer { get; private set; }
        /// <summary>True on the frame the reset button was pressed.</summary>
        public bool ResetPressed { get; private set; }
        /// <summary>True on the frame the debug toggle was pressed.</summary>
        public bool DebugTogglePressed { get; private set; }


        // ---- Private Fields ----

        private enum TriggerMode { Detecting, Separate, Combined, None }
        private TriggerMode _triggerMode = TriggerMode.Detecting;
        private int _detectFrames;


        // ---- Unity Lifecycle ----

        void Update()
        {
            float kbThrottle = UnityEngine.Input.GetKey(KeyCode.W) ? 1f : 0f;
            float gpThrottle = GetGamepadThrottle();
            Throttle = Mathf.Max(kbThrottle, gpThrottle);

            float kbBrake = UnityEngine.Input.GetKey(KeyCode.S) ? 1f : 0f;
            float gpBrake = GetGamepadBrake();
            Brake = Mathf.Max(kbBrake, gpBrake);

            float kbSteer = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) kbSteer += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) kbSteer -= 1f;

            float gpSteer = UnityEngine.Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(gpSteer) < _steerDeadzone) gpSteer = 0f;

            float rawSteer = Mathf.Abs(gpSteer) > Mathf.Abs(kbSteer) ? gpSteer : kbSteer;
            Steer = Mathf.Sign(rawSteer) * Mathf.Pow(Mathf.Abs(rawSteer), _steerCurveExponent);

            ResetPressed = UnityEngine.Input.GetKeyDown(KeyCode.R) ||
                           UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton3);
            DebugTogglePressed = UnityEngine.Input.GetKeyDown(KeyCode.F2);

            if (_triggerMode == TriggerMode.Detecting)
                DetectTriggerMode();
        }


        // ---- Private Methods ----

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
                Debug.Log("[RCInput] No gamepad triggers detected — using keyboard only. " +
                          "Press a trigger firmly to enable gamepad.");
            }
        }

        private float GetGamepadThrottle()
        {
            switch (_triggerMode)
            {
                case TriggerMode.Separate:
                    return ApplyDeadzone(UnityEngine.Input.GetAxisRaw("RightTrigger"));

                case TriggerMode.Combined:
                    return ApplyDeadzone(UnityEngine.Input.GetAxisRaw("CombinedTriggers"));

                case TriggerMode.Detecting:
                    float rt = UnityEngine.Input.GetAxisRaw("RightTrigger");
                    if (rt > k_StrongInputThreshold) return ApplyDeadzone(rt);
                    float com = UnityEngine.Input.GetAxisRaw("CombinedTriggers");
                    if (com > k_StrongInputThreshold) return ApplyDeadzone(com);
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
                    return ApplyDeadzone(UnityEngine.Input.GetAxisRaw("LeftTrigger"));

                case TriggerMode.Combined:
                    return ApplyDeadzone(-UnityEngine.Input.GetAxisRaw("CombinedTriggers"));

                case TriggerMode.Detecting:
                    float lt = UnityEngine.Input.GetAxisRaw("LeftTrigger");
                    if (lt > k_StrongInputThreshold) return ApplyDeadzone(lt);
                    float com = -UnityEngine.Input.GetAxisRaw("CombinedTriggers");
                    if (com > k_StrongInputThreshold) return ApplyDeadzone(com);
                    return 0f;

                default:
                    return 0f;
            }
        }

        private float ApplyDeadzone(float raw)
        {
            if (Mathf.Abs(raw) < _triggerDeadzone) return 0f;
            float sign = Mathf.Sign(raw);
            float remapped = (Mathf.Abs(raw) - _triggerDeadzone) / (1f - _triggerDeadzone);
            return Mathf.Clamp01(sign * remapped);
        }
    }
}