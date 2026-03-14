using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Pure math functions for Hooke's law suspension calculations.
    /// No MonoBehaviour dependency — fully unit-testable.
    /// </summary>
    public static class SuspensionMath
    {
        /// <summary>
        /// Compute spring length from wheel anchor to contact point, minus wheel radius.
        /// Clamps to minSpringLen (bump stop) to prevent chassis clip-through.
        /// </summary>
        /// <param name="anchorToContact">Distance from wheel anchor to ground contact point (m)</param>
        /// <param name="wheelRadius">Tire radius (m)</param>
        /// <param name="minSpringLen">Bump stop minimum length (m)</param>
        /// <returns>Clamped spring length (m)</returns>
        public static float ComputeSpringLength(float anchorToContact, float wheelRadius, float minSpringLen)
        {
            float raw = anchorToContact - wheelRadius;
            return Mathf.Max(raw, minSpringLen);
        }

        /// <summary>
        /// Compute Hooke's law suspension force: F = k * compression + c * velocity.
        /// Returns 0 if force would be negative (suspension never pulls — no tension).
        /// </summary>
        /// <param name="springStrength">Spring stiffness k (N/m)</param>
        /// <param name="restDistance">Rest length of suspension (m)</param>
        /// <param name="springLen">Current spring length (m)</param>
        /// <param name="prevSpringLen">Previous frame spring length (m)</param>
        /// <param name="deltaTime">Physics timestep (s)</param>
        /// <returns>Suspension force in Newtons, never negative</returns>
        public static float ComputeSuspensionForce(
            float springStrength, float restDistance,
            float springLen, float prevSpringLen, float deltaTime)
        {
            float compression = restDistance - springLen;
            float springForce = springStrength * compression;
            float dampingForce = springStrength > 0f && deltaTime > 0f
                ? (prevSpringLen - springLen) / deltaTime
                : 0f;
            // Note: damping uses the damping coefficient, not spring strength.
            // This method returns the raw spring force component.
            // Use ComputeSuspensionForceWithDamping for the full calculation.
            float rawForce = springForce;
            return Mathf.Max(rawForce, 0f);
        }

        /// <summary>
        /// Compute full suspension force with spring and damping components.
        /// F = stiffness * (restDistance - springLen) + damping * (prevLen - curLen) / dt
        /// Clamped to >= 0 (no tension).
        /// </summary>
        /// <param name="springStrength">Spring stiffness k (N/m)</param>
        /// <param name="springDamping">Damping coefficient c</param>
        /// <param name="restDistance">Rest length (m)</param>
        /// <param name="springLen">Current spring length (m)</param>
        /// <param name="prevSpringLen">Previous frame spring length (m)</param>
        /// <param name="deltaTime">Physics timestep (s)</param>
        /// <returns>Total suspension force in Newtons, clamped >= 0</returns>
        public static float ComputeSuspensionForceWithDamping(
            float springStrength, float springDamping,
            float restDistance, float springLen, float prevSpringLen, float deltaTime)
        {
            float compression = restDistance - springLen;
            float springForce = springStrength * compression;
            float dampingForce = deltaTime > 0f
                ? springDamping * (prevSpringLen - springLen) / deltaTime
                : 0f;
            return Mathf.Max(springForce + dampingForce, 0f);
        }

        /// <summary>
        /// Compute grip load: spring force clamped to [0, maxSpringForce].
        /// Used as the normal force input for tire grip calculations.
        /// </summary>
        /// <param name="springStrength">Spring stiffness (N/m)</param>
        /// <param name="restDistance">Rest length (m)</param>
        /// <param name="springLen">Current spring length (m)</param>
        /// <param name="maxSpringForce">Maximum force clamp (N)</param>
        /// <returns>Grip load in Newtons</returns>
        public static float ComputeGripLoad(
            float springStrength, float restDistance,
            float springLen, float maxSpringForce)
        {
            float springForce = springStrength * (restDistance - springLen);
            return Mathf.Clamp(springForce, 0f, maxSpringForce);
        }

        /// <summary>
        /// Compute grip load from the actual (damped) suspension force.
        /// Clamps to [0, maxSpringForce]. Use this instead of ComputeGripLoad
        /// to ensure damping is included in the normal force for tire grip.
        /// </summary>
        /// <param name="suspensionForce">Total suspension force including damping (N)</param>
        /// <param name="maxSpringForce">Maximum force clamp (N)</param>
        /// <returns>Grip load in Newtons</returns>
        public static float ComputeGripLoadFromSuspensionForce(
            float suspensionForce, float maxSpringForce)
        {
            return Mathf.Clamp(suspensionForce, 0f, maxSpringForce);
        }

        /// <summary>
        /// Compute total raycast length needed for ground detection.
        /// </summary>
        public static float ComputeRayLength(float restDistance, float overExtend, float wheelRadius)
        {
            return restDistance + overExtend + wheelRadius;
        }
    }
}
