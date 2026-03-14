using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Pure math functions for gyroscopic precession and reaction torque.
    /// Two equations produce emergent aerial behavior:
    ///   1. Gyroscopic torque: tau = omega_body x (I * omega_spin * axis)
    ///   2. Reaction torque: tau = -axis * I * d(omega)/dt
    /// </summary>
    public static class GyroscopicMath
    {
        /// <summary>
        /// Compute gyroscopic precession torque from body rotation and wheel spin.
        /// tau_gyro = omega_body x (I_wheel * omega_spin * spin_axis)
        /// This couples yaw to pitch and pitch to yaw — NOT damping.
        /// </summary>
        /// <param name="bodyAngularVelocity">Body angular velocity in world space (rad/s)</param>
        /// <param name="wheelSpinAxis">Wheel spin axis in world space (unit vector)</param>
        /// <param name="wheelMoI">Wheel moment of inertia (kg*m^2)</param>
        /// <param name="wheelAngularVelocity">Wheel spin rate (rad/s)</param>
        /// <returns>Gyroscopic torque vector in world space (N*m)</returns>
        public static Vector3 ComputeGyroscopicTorque(
            Vector3 bodyAngularVelocity, Vector3 wheelSpinAxis,
            float wheelMoI, float wheelAngularVelocity)
        {
            Debug.Assert(wheelMoI >= 0f, "Wheel MoI must be non-negative");

            Vector3 angularMomentum = wheelSpinAxis * (wheelMoI * wheelAngularVelocity);
            return Vector3.Cross(bodyAngularVelocity, angularMomentum);
        }

        /// <summary>
        /// Compute reaction torque from wheel spin rate change (Newton's 3rd law).
        /// tau_reaction = -spin_axis * I_wheel * delta_omega / delta_time
        /// Throttle accelerates wheels -> nose pitches up.
        /// Brake decelerates wheels -> nose pitches down.
        /// </summary>
        /// <param name="wheelSpinAxis">Wheel spin axis in world space (unit vector)</param>
        /// <param name="wheelMoI">Wheel moment of inertia (kg*m^2)</param>
        /// <param name="currentSpinRate">Current wheel spin rate (rad/s)</param>
        /// <param name="previousSpinRate">Previous frame wheel spin rate (rad/s)</param>
        /// <param name="deltaTime">Time step (seconds)</param>
        /// <returns>Reaction torque vector in world space (N*m)</returns>
        public static Vector3 ComputeReactionTorque(
            Vector3 wheelSpinAxis, float wheelMoI,
            float currentSpinRate, float previousSpinRate, float deltaTime)
        {
            Debug.Assert(wheelMoI >= 0f, "Wheel MoI must be non-negative");
            Debug.Assert(deltaTime > 0f, "Delta time must be positive");

            float deltaOmega = currentSpinRate - previousSpinRate;
            float angularAcceleration = deltaOmega / deltaTime;
            return -wheelSpinAxis * (wheelMoI * angularAcceleration);
        }

        /// <summary>
        /// Convert forward speed to wheel angular velocity.
        /// omega = v / r (no-slip condition)
        /// </summary>
        /// <param name="forwardSpeed">Forward speed in m/s</param>
        /// <param name="wheelRadius">Wheel radius in metres</param>
        /// <returns>Angular velocity in rad/s</returns>
        public static float ComputeWheelAngularVelocity(float forwardSpeed, float wheelRadius)
        {
            Debug.Assert(wheelRadius > 0f, "Wheel radius must be positive");

            return forwardSpeed / wheelRadius;
        }
    }
}
