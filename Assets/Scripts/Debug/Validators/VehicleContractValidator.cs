using UnityEngine;
using R8EOX.Input;
using R8EOX.Vehicle;

namespace R8EOX.Debug.Validators
{
#if UNITY_EDITOR || DEBUG
    /// <summary>
    /// Validates vehicle force and state contracts (FixedUpdate cadence).
    /// Extracted from ContractDebugger — stateless, all methods static.
    /// </summary>
    public static class VehicleContractValidator
    {
        // ---- Constants ----

        /// <summary>Tolerance for floating-point comparisons.</summary>
        const float k_Epsilon = 1e-5f;


        // ---- Public API ----

        /// <summary>
        /// Validates all vehicle contracts for a given frame.
        /// </summary>
        /// <param name="car">The RC car to inspect.</param>
        /// <param name="input">The vehicle input (may be null).</param>
        /// <param name="logAllValues">When true, logs passing values every frame (verbose).</param>
        /// <returns>Number of violations found.</returns>
        public static int ValidateVehicleContracts(RCCar car, IVehicleInput input, bool logAllValues)
        {
            if (car == null) return 0;

            int violations = 0;
            int frame = Time.frameCount;

            // CurrentBrakeForce >= 0
            if (car.CurrentBrakeForce < -k_Epsilon)
            {
                violations++;
                LogViolation("CurrentBrakeForce is negative",
                    car.CurrentBrakeForce, 0f, float.MaxValue, frame);
            }

            // Engine and Brake mutually exclusive (both > 0 simultaneously is a violation)
            if (car.CurrentEngineForce > k_Epsilon && car.CurrentBrakeForce > k_Epsilon)
            {
                violations++;
                LogViolation(
                    $"Engine ({car.CurrentEngineForce:F2}) and Brake ({car.CurrentBrakeForce:F2}) both active",
                    car.CurrentEngineForce, 0f, 0f, frame);
            }

            // If airborne: engine and brake must be 0
            if (car.IsAirborne)
            {
                if (Mathf.Abs(car.CurrentEngineForce) > k_Epsilon)
                {
                    violations++;
                    LogViolation("Engine force non-zero while airborne",
                        car.CurrentEngineForce, 0f, 0f, frame);
                }
                if (Mathf.Abs(car.CurrentBrakeForce) > k_Epsilon)
                {
                    violations++;
                    LogViolation("Brake force non-zero while airborne",
                        car.CurrentBrakeForce, 0f, 0f, frame);
                }
            }

            // Speed cutoff: if at max speed with throttle, engine should be 0
            if (input != null && input.Throttle > k_Epsilon &&
                Mathf.Abs(car.ForwardSpeed) >= car.MaxSpeed - k_Epsilon &&
                !car.IsAirborne)
            {
                if (Mathf.Abs(car.CurrentEngineForce) > k_Epsilon)
                {
                    violations++;
                    LogViolation(
                        $"Engine force ({car.CurrentEngineForce:F2}) active at max speed ({car.ForwardSpeed:F2}/{car.MaxSpeed:F2})",
                        car.CurrentEngineForce, 0f, 0f, frame);
                }
            }

            // SmoothThrottle in [0, 1]
            if (car.SmoothThrottle < -k_Epsilon || car.SmoothThrottle > 1f + k_Epsilon)
            {
                violations++;
                LogViolation("SmoothThrottle out of range [0,1]",
                    car.SmoothThrottle, 0f, 1f, frame);
            }

            // CurrentSteering magnitude <= SteeringMax
            if (Mathf.Abs(car.CurrentSteering) > car.SteeringMax + k_Epsilon)
            {
                violations++;
                LogViolation(
                    $"CurrentSteering magnitude ({Mathf.Abs(car.CurrentSteering):F4}) exceeds SteeringMax ({car.SteeringMax:F4})",
                    Mathf.Abs(car.CurrentSteering), 0f, car.SteeringMax, frame);
            }

            if (logAllValues)
            {
                UnityEngine.Debug.Log($"[VehicleContractValidator] Vehicle OK frame={frame} " +
                    $"engine={car.CurrentEngineForce:F2} brake={car.CurrentBrakeForce:F2} " +
                    $"steering={car.CurrentSteering:F4} smoothThrottle={car.SmoothThrottle:F4} " +
                    $"airborne={car.IsAirborne}");
            }

            return violations;
        }


        // ---- Private Helpers ----

        static void LogViolation(string contract, float actual, float expectedMin, float expectedMax, int frame)
        {
            UnityEngine.Debug.LogError(
                $"[ContractDebugger] VEHICLE VIOLATION: {contract} | " +
                $"actual={actual:F6} expected=[{expectedMin:F2}, {expectedMax:F2}] | frame={frame}");
        }
    }
#endif // UNITY_EDITOR || DEBUG
}
