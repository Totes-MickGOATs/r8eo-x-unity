using NUnit.Framework;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for steering ramp-toward-target behavior.
    /// Direction and speed-reduction tests live in SteeringDirectionTests.cs.
    /// </summary>
    public class SteeringRampBehaviorTests
    {
        const float k_SteeringMax = 0.50f;
        const float k_SteeringSpeed = 7f;
        const float k_SteeringSpeedLimit = 8f;
        const float k_SteeringHighSpeedFactor = 0.4f;
        const float k_ReverseSpeedThreshold = 0.25f;
        const float k_Dt = 0.008333f;

        private float ComputeSteeringTarget(float steerIn, float fwdSpeed)
        {
            float spd = Mathf.Abs(fwdSpeed);
            float t = Mathf.Clamp01(spd / k_SteeringSpeedLimit);
            float effectiveMax = Mathf.Lerp(k_SteeringMax, k_SteeringMax * k_SteeringHighSpeedFactor, t);
            float steerSign = fwdSpeed < -k_ReverseSpeedThreshold ? -1f : 1f;
            return steerIn * effectiveMax * steerSign;
        }

        private float SimulateSteering(float currentSteering, float steerIn, float fwdSpeed, float dt)
        {
            float target = ComputeSteeringTarget(steerIn, fwdSpeed);
            return Mathf.MoveTowards(currentSteering, target, k_SteeringSpeed * dt);
        }

        [Test]
        public void Steering_RampFromZero_MovesTowardTarget()
        {
            float currentSteering = 0f;
            float result = SimulateSteering(currentSteering, steerIn: 1.0f, fwdSpeed: 1.0f, k_Dt);

            Assert.Greater(result, 0f,
                "Steering should move toward target from zero");
            Assert.Less(result, k_SteeringMax,
                "Steering should not reach max in a single frame");
        }

        [Test]
        public void Steering_ZeroInput_ReturnsToCenter()
        {
            float currentSteering = 0.3f;
            float result = SimulateSteering(currentSteering, steerIn: 0f, fwdSpeed: 1.0f, k_Dt);

            Assert.Less(result, currentSteering,
                "Steering should move toward zero when input is zero");
        }
    }
}
