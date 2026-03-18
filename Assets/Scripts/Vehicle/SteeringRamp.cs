using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>Computes speed-sensitive steering target and ramps toward it.</summary>
    public static class SteeringRamp
    {
        const float k_ReverseSpeedThreshold = 0.25f;

        public static float Step(float current, float dt,
            float steerIn, float fwdSpeed,
            float steeringMax, float steeringSpeed,
            float steeringSpeedLimit, float highSpeedFactor)
        {
            float spd = Mathf.Abs(fwdSpeed);
            float t = Mathf.Clamp01(spd / steeringSpeedLimit);
            float effectiveMax = Mathf.Lerp(steeringMax, steeringMax * highSpeedFactor, t);
            float steerSign = fwdSpeed < -k_ReverseSpeedThreshold ? -1f : 1f;
            float target = steerIn * effectiveMax * steerSign;
            return Mathf.MoveTowards(current, target, steeringSpeed * dt);
        }
    }
}
