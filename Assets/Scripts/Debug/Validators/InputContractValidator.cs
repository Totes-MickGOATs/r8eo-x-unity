using UnityEngine;
using R8EOX.Input;

namespace R8EOX.Debug.Validators
{
#if UNITY_EDITOR || DEBUG
    /// <summary>
    /// Validates input range and phantom-input contracts.
    /// Extracted from ContractDebugger — stateless, all methods static.
    /// </summary>
    public static class InputContractValidator
    {
        // ---- Constants ----

        /// <summary>Tolerance for floating-point comparisons.</summary>
        const float k_Epsilon = 1e-5f;


        // ---- Public API ----

        /// <summary>
        /// Validates all input contracts for a given frame.
        /// </summary>
        /// <param name="input">The vehicle input to inspect.</param>
        /// <param name="logAllValues">When true, logs passing values every frame (verbose).</param>
        /// <returns>Number of violations found.</returns>
        public static int ValidateInputContracts(IVehicleInput input, bool logAllValues)
        {
            if (input == null) return 0;

            int violations = 0;
            int frame = Time.frameCount;

            // Throttle in [0, 1]
            if (input.Throttle < -k_Epsilon || input.Throttle > 1f + k_Epsilon)
            {
                violations++;
                LogViolation("Throttle out of range [0,1]",
                    input.Throttle, 0f, 1f, frame);
            }

            // Brake in [0, 1]
            if (input.Brake < -k_Epsilon || input.Brake > 1f + k_Epsilon)
            {
                violations++;
                LogViolation("Brake out of range [0,1]",
                    input.Brake, 0f, 1f, frame);
            }

            // Steer in [-1, 1]
            if (input.Steer < -1f - k_Epsilon || input.Steer > 1f + k_Epsilon)
            {
                violations++;
                LogViolation("Steer out of range [-1,1]",
                    input.Steer, -1f, 1f, frame);
            }

            if (logAllValues)
            {
                UnityEngine.Debug.Log($"[InputContractValidator] Input OK frame={frame} " +
                    $"throttle={input.Throttle:F4} brake={input.Brake:F4} steer={input.Steer:F4}");
            }

            return violations;
        }


        // ---- Private Helpers ----

        static void LogViolation(string contract, float actual, float expectedMin, float expectedMax, int frame)
        {
            UnityEngine.Debug.LogError(
                $"[ContractDebugger] INPUT VIOLATION: {contract} | " +
                $"actual={actual:F6} expected=[{expectedMin:F2}, {expectedMax:F2}] | frame={frame}");
        }
    }
#endif // UNITY_EDITOR || DEBUG
}
