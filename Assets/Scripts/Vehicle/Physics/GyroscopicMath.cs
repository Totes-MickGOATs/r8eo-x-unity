using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Pure math functions for gyroscopic precession and reaction torque.
    /// Computes effects of spinning wheels on chassis stability.
    /// </summary>
    /// <remarks>
    /// STUB: Methods return zero. Implement with real physics to make tests GREEN.
    /// </remarks>
    public static class GyroscopicMath
    {
        /// <summary>
        /// Compute gyroscopic precession torque from body rotation and wheel spin.
        /// τ = ω_body × (I_wheel * ω_spin * spinAxis)
        /// </summary>
        /// <param name="bodyAngularVelocity">Body angular velocity (rad/s)</param>
        /// <param name="wheelSpinAxis">Normalized wheel spin axis (typically right)</param>
        /// <param name="wheelMoI">Wheel moment of inertia (kg*m²)</param>
        /// <param name="wheelSpinRate">Wheel spin rate (rad/s)</param>
        /// <returns>Gyroscopic precession torque (N*m)</returns>
        public static Vector3 ComputeGyroscopicTorque(
            Vector3 bodyAngularVelocity, Vector3 wheelSpinAxis,
            float wheelMoI, float wheelSpinRate)
        {
            // TODO: implement — τ = ω_body × (I * ω_spin * axis)
            return Vector3.zero;
        }

        /// <summary>
        /// Compute reaction torque from change in wheel spin rate.
        /// τ = -spinAxis * I * (currentSpin - previousSpin) / dt
        /// </summary>
        /// <param name="wheelSpinAxis">Normalized wheel spin axis</param>
        /// <param name="wheelMoI">Wheel moment of inertia (kg*m²)</param>
        /// <param name="currentSpinRate">Current wheel spin rate (rad/s)</param>
        /// <param name="previousSpinRate">Previous frame spin rate (rad/s)</param>
        /// <param name="deltaTime">Time step (s)</param>
        /// <returns>Reaction torque opposing spin change (N*m)</returns>
        public static Vector3 ComputeReactionTorque(
            Vector3 wheelSpinAxis, float wheelMoI,
            float currentSpinRate, float previousSpinRate, float deltaTime)
        {
            // TODO: implement — τ = -axis * I * Δω/Δt
            return Vector3.zero;
        }

        /// <summary>
        /// Compute wheel angular velocity from linear speed and radius.
        /// ω = v / r
        /// </summary>
        /// <param name="linearSpeed">Linear speed at tire contact (m/s)</param>
        /// <param name="wheelRadius">Wheel radius (m)</param>
        /// <returns>Angular velocity (rad/s)</returns>
        public static float ComputeWheelAngularVelocity(float linearSpeed, float wheelRadius)
        {
            // TODO: implement — ω = v / r
            return 0f;
        }
    }
}
