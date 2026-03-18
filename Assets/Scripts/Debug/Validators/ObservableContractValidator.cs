using UnityEngine;
using R8EOX.Input;
using R8EOX.Vehicle;

namespace R8EOX.Debug.Validators
{
#if UNITY_EDITOR || DEBUG
    /// <summary>
    /// Validates observable consistency contracts over multiple frames (LateUpdate cadence).
    /// Stateful — maintains consecutive-frame counters and previous velocity.
    /// Extracted from ContractDebugger.
    /// </summary>
    public class ObservableContractValidator
    {
        // ---- Constants ----

        /// <summary>Consecutive frames required before observable contracts trigger.</summary>
        const int k_ObservableFrameThreshold = 10;

        /// <summary>Minimum velocity increase per frame to consider "accelerating" (m/s).</summary>
        const float k_MinAccelerationDelta = 0.0001f;

        /// <summary>Minimum velocity decrease per frame to consider "decelerating" (m/s).</summary>
        const float k_MinDecelerationDelta = 0.0001f;

        /// <summary>Speed threshold below which we consider the car "stopped" (m/s).</summary>
        const float k_StoppedSpeedThreshold = 0.05f;

        /// <summary>Tolerance for floating-point comparisons.</summary>
        const float k_Epsilon = 1e-5f;


        // ---- State ----

        private int _consecutiveEngineFrames;
        private int _consecutiveZeroInputFrames;
        private float _prevVelocityMagnitude;


        // ---- Public API ----

        /// <summary>
        /// Validates observable consistency contracts for the current frame.
        /// </summary>
        /// <param name="car">The RC car to inspect.</param>
        /// <param name="rb">The car's Rigidbody.</param>
        /// <param name="input">The vehicle input (may be null).</param>
        /// <param name="logAllValues">When true, logs passing values every frame (verbose).</param>
        /// <returns>Number of violations found.</returns>
        public int Validate(RCCar car, Rigidbody rb, IVehicleInput input, bool logAllValues)
        {
            if (car == null || rb == null) return 0;

            int violations = 0;
            int frame = Time.frameCount;
            float currentSpeed = rb.velocity.magnitude;

            // Track consecutive engine-force frames
            if (car.CurrentEngineForce > k_Epsilon && !car.IsAirborne)
                _consecutiveEngineFrames++;
            else
                _consecutiveEngineFrames = 0;

            // Track consecutive zero-input frames
            if (input != null &&
                Mathf.Abs(input.Throttle) < k_Epsilon &&
                Mathf.Abs(input.Brake) < k_Epsilon &&
                Mathf.Abs(input.Steer) < k_Epsilon &&
                !car.IsAirborne)
            {
                _consecutiveZeroInputFrames++;
            }
            else
            {
                _consecutiveZeroInputFrames = 0;
            }

            // If engine active for N frames AND grounded: velocity should increase (or be at max)
            if (_consecutiveEngineFrames >= k_ObservableFrameThreshold)
            {
                bool atMaxSpeed = Mathf.Abs(car.ForwardSpeed) >= car.MaxSpeed - k_Epsilon;
                bool velocityIncreasing = currentSpeed > _prevVelocityMagnitude - k_MinAccelerationDelta;

                if (!atMaxSpeed && !velocityIncreasing)
                {
                    violations++;
                    LogObservableViolation(
                        $"Engine active for {_consecutiveEngineFrames} frames but velocity not increasing " +
                        $"(prev={_prevVelocityMagnitude:F4} current={currentSpeed:F4})",
                        frame);
                }
            }

            // If all inputs zero for N frames AND grounded: velocity should decrease
            if (_consecutiveZeroInputFrames >= k_ObservableFrameThreshold)
            {
                bool alreadyStopped = currentSpeed < k_StoppedSpeedThreshold;
                bool velocityDecreasing = currentSpeed < _prevVelocityMagnitude + k_MinDecelerationDelta;

                if (!alreadyStopped && !velocityDecreasing)
                {
                    violations++;
                    LogObservableViolation(
                        $"All inputs zero for {_consecutiveZeroInputFrames} frames but velocity not decreasing " +
                        $"(prev={_prevVelocityMagnitude:F4} current={currentSpeed:F4})",
                        frame);
                }
            }

            _prevVelocityMagnitude = currentSpeed;

            if (logAllValues)
            {
                UnityEngine.Debug.Log($"[ObservableContractValidator] Observable OK frame={frame} " +
                    $"speed={currentSpeed:F4} engineFrames={_consecutiveEngineFrames} " +
                    $"zeroFrames={_consecutiveZeroInputFrames}");
            }

            return violations;
        }

        /// <summary>
        /// Resets all frame counters and previous velocity tracking.
        /// Call this when the session is restarted or counters need clearing.
        /// </summary>
        public void Reset()
        {
            _consecutiveEngineFrames = 0;
            _consecutiveZeroInputFrames = 0;
            _prevVelocityMagnitude = 0f;
        }


        // ---- Private Helpers ----

        static void LogObservableViolation(string description, int frame)
        {
            UnityEngine.Debug.LogWarning(
                $"[ContractDebugger] OBSERVABLE: {description} | frame={frame}");
        }
    }
#endif // UNITY_EDITOR || DEBUG
}
