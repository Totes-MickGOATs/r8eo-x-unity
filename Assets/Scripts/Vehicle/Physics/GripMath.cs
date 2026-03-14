using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Pure math functions for tire grip calculations.
    /// Curve-sampled slip ratio model — NOT Pacejka.
    /// </summary>
    public static class GripMath
    {
        /// <summary>
        /// Compute lateral slip ratio from sideways velocity and total speed.
        /// </summary>
        /// <param name="lateralVelocity">Sideways velocity component (m/s)</param>
        /// <param name="speed">Total tire contact speed (m/s)</param>
        /// <returns>Slip ratio clamped to [0, 1]</returns>
        public static float ComputeSlipRatio(float lateralVelocity, float speed)
        {
            if (speed < 0.0001f) return 0f;
            return Mathf.Clamp01(Mathf.Abs(lateralVelocity) / speed);
        }

        /// <summary>
        /// Compute lateral grip force magnitude.
        /// F = lateralVelocity * gripFactor * gripCoeff * gripLoad
        /// </summary>
        /// <param name="lateralVelocity">Sideways velocity component (m/s)</param>
        /// <param name="gripFactor">Grip factor from curve sample (0-1)</param>
        /// <param name="gripCoeff">Surface friction coefficient (0-1)</param>
        /// <param name="gripLoad">Normal force from suspension (N)</param>
        /// <returns>Lateral force magnitude in Newtons (negative = opposing lateral motion)</returns>
        public static float ComputeLateralForceMagnitude(
            float lateralVelocity, float gripFactor, float gripCoeff, float gripLoad)
        {
            return -lateralVelocity * gripFactor * gripCoeff * gripLoad;
        }

        /// <summary>
        /// Compute effective longitudinal traction based on braking state and speed.
        /// When stopped with no engine force, static friction is applied.
        /// </summary>
        /// <param name="isBraking">Whether the wheel is actively braking</param>
        /// <param name="forwardSpeed">Forward speed at contact point (m/s)</param>
        /// <param name="engineForce">Current engine force (N)</param>
        /// <param name="zTraction">Normal longitudinal traction factor</param>
        /// <param name="zBrakeTraction">Braking traction factor</param>
        /// <param name="staticFrictionSpeed">Speed threshold for static friction (m/s)</param>
        /// <param name="staticFrictionTraction">Static friction traction value</param>
        /// <returns>Effective traction coefficient</returns>
        public static float ComputeEffectiveTraction(
            bool isBraking, float forwardSpeed, float engineForce,
            float zTraction, float zBrakeTraction,
            float staticFrictionSpeed, float staticFrictionTraction)
        {
            float effectiveTraction = isBraking ? zBrakeTraction : zTraction;

            if (Mathf.Abs(forwardSpeed) < staticFrictionSpeed && engineForce == 0f)
                effectiveTraction = staticFrictionTraction;

            return effectiveTraction;
        }

        /// <summary>
        /// Compute longitudinal friction force magnitude.
        /// Opposes forward motion along the car's axis.
        /// </summary>
        /// <param name="forwardSpeed">Forward velocity at contact point (m/s)</param>
        /// <param name="effectiveTraction">Traction coefficient</param>
        /// <param name="gripCoeff">Surface friction coefficient</param>
        /// <param name="gripLoad">Normal force from suspension (N)</param>
        /// <returns>Longitudinal force magnitude in Newtons</returns>
        public static float ComputeLongitudinalForceMagnitude(
            float forwardSpeed, float effectiveTraction, float gripCoeff, float gripLoad)
        {
            return -forwardSpeed * effectiveTraction * gripCoeff * gripLoad;
        }

        /// <summary>
        /// Compute wheel RPM from forward speed and wheel radius.
        /// </summary>
        public static float ComputeWheelRpm(float forwardSpeed, float wheelRadius)
        {
            if (wheelRadius <= 0f) return 0f;
            return (forwardSpeed / wheelRadius) * 60f / (2f * Mathf.PI);
        }
    }
}
