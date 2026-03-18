using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Computes the ramped steering angle for a ground-drive frame.
    /// Applies speed-dependent angle reduction and reverses steering direction
    /// when the vehicle is travelling backwards.
    /// </summary>
    internal sealed class SteeringRamp
    {
        // ---- Constants ----

        const float k_ReverseSpeedThreshold = 0.25f;


        // ---- State ----

        private float _currentSteering;


        // ---- Property ----

        /// <summary>Current ramped steering angle in radians.</summary>
        internal float CurrentSteering => _currentSteering;


        // ---- Update ----

        /// <summary>
        /// Advances the steering ramp by <paramref name="dt"/> seconds toward the desired
        /// target angle. Call once per FixedUpdate when the vehicle is on the ground.
        /// </summary>
        /// <param name="dt">Fixed time delta in seconds.</param>
        /// <param name="steerIn">Raw steer input in [-1, 1].</param>
        /// <param name="fwdSpeed">Signed forward speed in m/s.</param>
        /// <param name="steeringMax">Maximum steering angle in radians.</param>
        /// <param name="steeringSpeed">Ramp rate in rad/s.</param>
        /// <param name="speedLimit">Speed at which steering reduction begins, in m/s.</param>
        /// <param name="highSpeedFactor">Fraction of steeringMax kept at high speed (0-1).</param>
        internal void Update(
            float dt,
            float steerIn,
            float fwdSpeed,
            float steeringMax,
            float steeringSpeed,
            float speedLimit,
            float highSpeedFactor)
        {
            float spd         = Mathf.Abs(fwdSpeed);
            float t           = Mathf.Clamp01(spd / speedLimit);
            float effectiveMax = Mathf.Lerp(steeringMax, steeringMax * highSpeedFactor, t);
            float steerSign   = fwdSpeed < -k_ReverseSpeedThreshold ? -1f : 1f;
            float target      = steerIn * effectiveMax * steerSign;

            _currentSteering = Mathf.MoveTowards(_currentSteering, target, steeringSpeed * dt);
        }
    }
}
