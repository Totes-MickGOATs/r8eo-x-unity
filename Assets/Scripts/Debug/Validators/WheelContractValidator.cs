using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug.Validators
{
#if UNITY_EDITOR || DEBUG
    /// <summary>
    /// Validates per-wheel physics contracts (FixedUpdate cadence).
    /// Extracted from ContractDebugger — stateless, all methods static.
    /// </summary>
    public static class WheelContractValidator
    {
        // ---- Constants ----

        /// <summary>Tolerance for floating-point comparisons.</summary>
        const float k_Epsilon = 1e-5f;


        // ---- Public API ----

        /// <summary>
        /// Validates all wheel contracts for the given wheel array.
        /// </summary>
        /// <param name="wheels">Array of raycast wheels to inspect.</param>
        /// <param name="logAllValues">When true, logs passing values every frame (verbose).</param>
        /// <returns>Number of violations found.</returns>
        public static int ValidateWheelContracts(RaycastWheel[] wheels, bool logAllValues)
        {
            if (wheels == null) return 0;

            int violations = 0;
            int frame = Time.frameCount;

            for (int i = 0; i < wheels.Length; i++)
            {
                RaycastWheel w = wheels[i];
                string wheelName = w != null ? w.name : $"Wheel[{i}]";

                // GripLoad >= 0 when grounded (no tension in suspension)
                if (w.IsOnGround && w.LastGripLoad < -k_Epsilon)
                {
                    violations++;
                    LogWheelViolation(wheelName, "GripLoad negative while grounded",
                        w.LastGripLoad, 0f, float.MaxValue, frame);
                }

                // SlipRatio in [0, 1] (our implementation uses 0-1, not -1 to 1)
                if (w.SlipRatio < -k_Epsilon || w.SlipRatio > 1f + k_Epsilon)
                {
                    violations++;
                    LogWheelViolation(wheelName, "SlipRatio out of range [0,1]",
                        w.SlipRatio, 0f, 1f, frame);
                }

                // GripFactor in [0, 1]
                if (w.GripFactor < -k_Epsilon || w.GripFactor > 1f + k_Epsilon)
                {
                    violations++;
                    LogWheelViolation(wheelName, "GripFactor out of range [0,1]",
                        w.GripFactor, 0f, 1f, frame);
                }

                // Motor force only if IsMotor AND IsOnGround
                if (!w.IsMotor && Mathf.Abs(w.MotorForceShare) > k_Epsilon)
                {
                    violations++;
                    LogWheelViolation(wheelName, "MotorForceShare non-zero on non-motor wheel",
                        w.MotorForceShare, 0f, 0f, frame);
                }
                if (!w.IsOnGround && Mathf.Abs(w.MotorForceShare) > k_Epsilon)
                {
                    violations++;
                    LogWheelViolation(wheelName, "MotorForceShare non-zero while airborne",
                        w.MotorForceShare, 0f, 0f, frame);
                }
            }

            if (logAllValues && wheels.Length > 0)
            {
                var w = wheels[0];
                UnityEngine.Debug.Log($"[WheelContractValidator] Wheels OK frame={frame} " +
                    $"sample: {w.name} ground={w.IsOnGround} grip={w.GripFactor:F3} " +
                    $"slip={w.SlipRatio:F3} motor={w.MotorForceShare:F2}");
            }

            return violations;
        }


        // ---- Private Helpers ----

        static void LogWheelViolation(string wheelName, string contract, float actual,
            float expectedMin, float expectedMax, int frame)
        {
            UnityEngine.Debug.LogError(
                $"[ContractDebugger] WHEEL VIOLATION [{wheelName}]: {contract} | " +
                $"actual={actual:F6} expected=[{expectedMin:F2}, {expectedMax:F2}] | frame={frame}");
        }
    }
#endif // UNITY_EDITOR || DEBUG
}
