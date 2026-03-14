using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Pure math for ESC (Electronic Speed Controller) ground drive logic.
    /// Extracted from RCCar.ApplyGroundDrive for testability.
    /// Fixes: C6 (coast drag / IsBraking), M5 (engine cutoff), M6 (reverse threshold).
    /// </summary>
    public static class ESCMath
    {
        /// <summary>Result of ground drive computation.</summary>
        public struct DriveResult
        {
            public float EngineForce;
            public float BrakeForce;
            public float CoastDragForce;
            public bool ReverseEngaged;
        }

        /// <summary>
        /// Compute engine force, brake force, coast drag, and reverse state.
        /// </summary>
        /// <param name="throttleIn">Smoothed throttle input (0-1)</param>
        /// <param name="brakeIn">Brake input (0-1)</param>
        /// <param name="forwardSpeed">Signed forward speed (m/s, positive=forward)</param>
        /// <param name="reverseEngaged">Whether reverse was engaged last frame</param>
        /// <param name="engineForceMax">Peak engine force (N)</param>
        /// <param name="brakeForce">Max brake force (N)</param>
        /// <param name="reverseForce">Max reverse force (N)</param>
        /// <param name="coastDrag">Coast drag force (N)</param>
        /// <param name="maxSpeed">Max speed (m/s)</param>
        /// <param name="velocityMagnitude">Total velocity magnitude (unused after M5 fix, kept for API compat)</param>
        /// <param name="reverseSpeedThreshold">Speed below which reverse can engage</param>
        /// <param name="forwardSpeedClearThreshold">Speed above which reverse disengages</param>
        /// <param name="reverseBrakeMinThreshold">Minimum brake input to engage reverse (M6 fix)</param>
        /// <returns>Drive result with engine, brake, coast, and reverse state</returns>
        public static DriveResult ComputeGroundDrive(
            float throttleIn, float brakeIn, float forwardSpeed,
            bool reverseEngaged,
            float engineForceMax, float brakeForce, float reverseForce,
            float coastDrag, float maxSpeed, float velocityMagnitude,
            float reverseSpeedThreshold, float forwardSpeedClearThreshold,
            float reverseBrakeMinThreshold)
        {
            var result = new DriveResult();

            // Reverse state machine
            if (throttleIn > 0f || forwardSpeed > forwardSpeedClearThreshold)
                reverseEngaged = false;
            else if (brakeIn > reverseBrakeMinThreshold && forwardSpeed < reverseSpeedThreshold) // M6: require brake > threshold
                reverseEngaged = true;

            result.ReverseEngaged = reverseEngaged;

            if (throttleIn > 0f)
            {
                // M5: Use Mathf.Abs(forwardSpeed) instead of velocity magnitude
                result.EngineForce = Mathf.Abs(forwardSpeed) >= maxSpeed ? 0f : throttleIn * engineForceMax;
                result.BrakeForce = 0f;
                result.CoastDragForce = 0f;
            }
            else if (brakeIn > 0f)
            {
                if (reverseEngaged)
                {
                    // M5: Also check reverse speed limit
                    result.EngineForce = Mathf.Abs(forwardSpeed) >= maxSpeed ? 0f : -brakeIn * reverseForce;
                    result.BrakeForce = 0f;
                }
                else
                {
                    result.EngineForce = 0f;
                    result.BrakeForce = brakeIn * brakeForce;
                }
                result.CoastDragForce = 0f;
            }
            else
            {
                // C6: Coast — set BrakeForce to 0, report coast drag separately
                result.ReverseEngaged = false;
                result.EngineForce = 0f;
                result.BrakeForce = 0f;
                result.CoastDragForce = coastDrag;
            }

            return result;
        }
    }
}
