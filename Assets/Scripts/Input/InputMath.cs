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
        /// Apply symmetric deadzone with remapping for bipolar axes (-1 to +1).
        /// Returns 0 when |raw| is below the deadzone threshold, then remaps
        /// the remaining range to [-1, +1] with no discontinuous jump at the edge.
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
        /// Merge two input sources, taking whichever has the larger absolute value.
        /// Used to combine keyboard and gamepad seamlessly.
        /// </summary>
        public static float MergeInputs(float a, float b)
        {
            return Mathf.Abs(a) > Mathf.Abs(b) ? a : b;
        }
    }
}
