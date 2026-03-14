using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Pure math functions for airborne physics calculations.
    /// Pitch/roll torques and gyroscopic stabilization.
    /// </summary>
    public static class AirPhysicsMath
    {
        /// <summary>
        /// Compute pitch torque magnitude from throttle/brake input.
        /// Positive throttle pitches nose up, positive brake pitches nose down.
        /// </summary>
        /// <param name="throttle">Throttle input (0-1)</param>
        /// <param name="brake">Brake input (0-1)</param>
        /// <param name="pitchTorque">Maximum pitch torque (N*m)</param>
        /// <param name="pitchSensitivity">Sensitivity multiplier</param>
        /// <returns>Pitch torque magnitude (N*m)</returns>
        [System.Obsolete("Use GyroscopicMath.ComputeReactionTorque instead")]
        public static float ComputePitchTorque(float throttle, float brake,
            float pitchTorque, float pitchSensitivity)
        {
            float pitchInput = throttle - brake;
            return pitchInput * pitchTorque * pitchSensitivity;
        }

        /// <summary>
        /// Compute roll torque magnitude from steering input.
        /// </summary>
        /// <param name="steer">Steering input (-1 to 1)</param>
        /// <param name="rollTorque">Maximum roll torque (N*m)</param>
        /// <param name="rollSensitivity">Sensitivity multiplier</param>
        /// <returns>Roll torque magnitude (N*m)</returns>
        [System.Obsolete("Use GyroscopicMath.ComputeGyroscopicTorque instead")]
        public static float ComputeRollTorque(float steer, float rollTorque, float rollSensitivity)
        {
            return steer * rollTorque * rollSensitivity;
        }

        /// <summary>
        /// Compute gyroscopic damping factor from average wheel RPM.
        /// Returns 0 if RPM is below threshold (10 RPM).
        /// </summary>
        /// <param name="avgRpm">Average absolute wheel RPM</param>
        /// <param name="gyroStrength">Maximum gyro strength multiplier</param>
        /// <param name="gyroFullRpm">RPM at which full gyro effect is reached</param>
        /// <returns>Gyro damping factor (0 to gyroStrength)</returns>
        [System.Obsolete("Use GyroscopicMath.ComputeGyroscopicTorque instead")]
        public static float ComputeGyroDampingFactor(float avgRpm, float gyroStrength, float gyroFullRpm)
        {
            const float k_MinRpmForGyro = 10f;
            if (avgRpm <= k_MinRpmForGyro) return 0f;
            if (gyroFullRpm <= 0f) return 0f;
            return Mathf.Min(avgRpm / gyroFullRpm, 1f) * gyroStrength;
        }

        /// <summary>
        /// Compute average absolute RPM across a set of wheel RPMs.
        /// </summary>
        public static float ComputeAverageAbsRpm(float[] wheelRpms)
        {
            if (wheelRpms == null || wheelRpms.Length == 0) return 0f;
            float total = 0f;
            foreach (float rpm in wheelRpms)
                total += Mathf.Abs(rpm);
            return total / wheelRpms.Length;
        }
    }
}
