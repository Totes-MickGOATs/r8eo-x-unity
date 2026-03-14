using UnityEngine;

namespace R8EOX.Input
{
    /// <summary>
    /// Pure math functions for input processing.
    /// Deadzone remapping and steering curve application.
    /// </summary>
    public static class InputMath
    {
        /// <summary>
        /// Apply deadzone with remapping so output is 0 at the deadzone edge
        /// and 1 at full deflection, with no discontinuous jump.
        /// </summary>
        /// <param name="raw">Raw input value</param>
        /// <param name="deadzone">Deadzone threshold (0-1)</param>
        /// <returns>Remapped value clamped to [0, 1]</returns>
        public static float ApplyDeadzone(float raw, float deadzone)
        {
            if (Mathf.Abs(raw) < deadzone) return 0f;
            float sign = Mathf.Sign(raw);
            float remapped = (Mathf.Abs(raw) - deadzone) / (1f - deadzone);
            return Mathf.Clamp01(sign * remapped);
        }

        /// <summary>
        /// Apply a power curve to steering for non-linear response.
        /// Preserves sign. Exponent > 1 gives more precision near center.
        /// </summary>
        /// <param name="rawSteer">Raw steering value (-1 to +1)</param>
        /// <param name="exponent">Curve exponent (1.0 = linear, 1.5 = default)</param>
        /// <returns>Curved steering value (-1 to +1)</returns>
        public static float ApplySteeringCurve(float rawSteer, float exponent)
        {
            return Mathf.Sign(rawSteer) * Mathf.Pow(Mathf.Abs(rawSteer), exponent);
        }

        /// <summary>
        /// Apply deadzone with symmetric remapping for signed axes like steering.
        /// Values within [-deadzone, +deadzone] return 0.
        /// Values outside are remapped to [-1, +1] preserving sign.
        /// </summary>
        /// <param name="raw">Raw input value (-1 to +1)</param>
        /// <param name="deadzone">Deadzone threshold (0-1)</param>
        /// <returns>Remapped value clamped to [-1, +1]</returns>
        public static float ApplySymmetricDeadzone(float raw, float deadzone)
        {
            float abs = Mathf.Abs(raw);
            if (abs < deadzone) return 0f;
            float sign = Mathf.Sign(raw);
            float remapped = (abs - deadzone) / (1f - deadzone);
            return sign * Mathf.Clamp01(remapped);
        }

        /// <summary>
        /// Extract throttle from a combined trigger axis.
        /// Only positive values produce throttle (RT pressed).
        /// Negative values (LT pressed or resting at -1.0) return 0.
        /// Fixes phantom trigger bug where resting value of -1.0 caused
        /// phantom brake via negation in the old code path.
        /// </summary>
        /// <param name="raw">Raw combined trigger axis value</param>
        /// <param name="deadzone">Deadzone threshold (0-1)</param>
        /// <returns>Throttle value clamped to [0, 1]</returns>
        public static float CombinedTriggerThrottle(float raw, float deadzone)
        {
            if (raw <= 0f) return 0f;
            return ApplyDeadzone(raw, deadzone);
        }

        /// <summary>
        /// Extract brake from a combined trigger axis.
        /// Only negative values produce brake (LT pressed).
        /// Positive values (RT pressed) and zero return 0.
        /// The raw value is negated internally so the output is always non-negative.
        /// Fixes phantom trigger bug where resting value of -1.0 caused
        /// phantom brake when negated to +1.0 and passed through deadzone.
        /// </summary>
        /// <param name="raw">Raw combined trigger axis value</param>
        /// <param name="deadzone">Deadzone threshold (0-1)</param>
        /// <returns>Brake value clamped to [0, 1]</returns>
        public static float CombinedTriggerBrake(float raw, float deadzone)
        {
            if (raw >= 0f) return 0f;
            return ApplyDeadzone(-raw, deadzone);
        }

        /// <summary>
        /// Filter gamepad steering axis based on gamepad detection state.
        /// When no gamepad is detected (TriggerDetector.Mode == None or Detecting
        /// with no strong input), returns 0 to prevent phantom steering from
        /// Unity's Input Manager reporting non-zero Horizontal axis values
        /// with no controller connected.
        /// When a gamepad is detected, applies the normal symmetric deadzone.
        /// </summary>
        /// <param name="rawHorizontal">Raw Horizontal axis value from Input.GetAxisRaw</param>
        /// <param name="deadzone">Symmetric deadzone threshold</param>
        /// <param name="gamepadDetected">True if TriggerDetector has confirmed a gamepad</param>
        /// <returns>Filtered steering value, 0 if no gamepad detected</returns>
        public static float FilterGamepadSteering(float rawHorizontal, float deadzone, bool gamepadDetected)
        {
            if (!gamepadDetected) return 0f;
            return ApplySymmetricDeadzone(rawHorizontal, deadzone);
        }

        /// <summary>
        /// Merge two input sources, taking whichever has the larger absolute value.
        /// Used to combine keyboard and gamepad seamlessly.
        /// </summary>
        public static float MergeInputs(float a, float b)
        {
            return Mathf.Abs(a) > Mathf.Abs(b) ? a : b;
        }
    }
}
