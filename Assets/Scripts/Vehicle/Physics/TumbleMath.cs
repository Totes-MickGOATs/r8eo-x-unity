using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Pure math functions for tumble detection and blending.
    /// </summary>
    public static class TumbleMath
    {
        /// <summary>
        /// Compute tumble factor using smoothstep blending between engage and full angles.
        /// Returns 0 when airborne (tumble is a ground-contact concept).
        /// </summary>
        /// <param name="tiltAngle">Current tilt from upright (degrees)</param>
        /// <param name="isAirborne">Whether all wheels are off ground</param>
        /// <param name="wasTumbling">Whether tumbling was active on previous frame</param>
        /// <param name="engageDeg">Angle where tumble blending begins (degrees)</param>
        /// <param name="fullDeg">Angle for full tumble effect (degrees)</param>
        /// <param name="hysteresisDeg">Hysteresis band to prevent oscillation (degrees)</param>
        /// <returns>Tumble factor 0.0-1.0</returns>
        public static float ComputeTumbleFactor(
            float tiltAngle, bool isAirborne, bool wasTumbling,
            float engageDeg, float fullDeg, float hysteresisDeg)
        {
            if (isAirborne) return 0f;

            float effectiveEngage = wasTumbling ? engageDeg - hysteresisDeg : engageDeg;

            if (tiltAngle <= effectiveEngage)
                return 0f;
            if (tiltAngle >= fullDeg)
                return 1f;

            float t = (tiltAngle - effectiveEngage) / (fullDeg - effectiveEngage);
            return Smoothstep(t);
        }

        /// <summary>
        /// Hermite smoothstep: 3t^2 - 2t^3
        /// Provides smooth transition with zero derivatives at 0 and 1.
        /// </summary>
        public static float Smoothstep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Compute tilt angle from the car's up vector relative to world up.
        /// </summary>
        /// <param name="carUp">Car's local up direction (normalized)</param>
        /// <returns>Tilt angle in degrees (0 = upright, 180 = inverted)</returns>
        public static float ComputeTiltAngle(Vector3 carUp)
        {
            float dot = Mathf.Clamp(Vector3.Dot(carUp, Vector3.up), -1f, 1f);
            return Mathf.Acos(dot) * Mathf.Rad2Deg;
        }
    }
}
