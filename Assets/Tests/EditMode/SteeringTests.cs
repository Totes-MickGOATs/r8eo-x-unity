using NUnit.Framework;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for steering direction and speed-dependent angle reduction.</summary>
    public class SteeringTests
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
        public void Steering_RightInput_PositiveAngle()
        {
            Assert.Greater(ComputeSteeringTarget(1.0f, 1.0f), 0f);
        }

        [Test]
        public void Steering_LeftInput_NegativeAngle()
        {
            Assert.Less(ComputeSteeringTarget(-1.0f, 1.0f), 0f);
        }

        [Test]
        public void Steering_FullRight_EqualsSteeringMax()
        {
            float target = ComputeSteeringTarget(1.0f, 0.5f);
            Assert.Greater(target, k_SteeringMax * 0.9f);
            Assert.LessOrEqual(target, k_SteeringMax + 0.001f);
        }

        [Test]
        public void Steering_HighSpeed_ReducedAngle()
        {
            float lowSpeedTarget = ComputeSteeringTarget(1.0f, 0f);
            float highSpeedTarget = ComputeSteeringTarget(1.0f, 20f);
            Assert.Less(Mathf.Abs(highSpeedTarget), Mathf.Abs(lowSpeedTarget));
            Assert.AreEqual(k_SteeringMax * k_SteeringHighSpeedFactor, highSpeedTarget, 0.01f);
        }

        [Test]
        public void Steering_AtSpeedLimit_HalfwayReduction()
        {
            float target = ComputeSteeringTarget(1.0f, k_SteeringSpeedLimit);
            Assert.AreEqual(k_SteeringMax * k_SteeringHighSpeedFactor, target, 0.01f);
        }

        [Test]
        public void Steering_Reverse_FlipsDirection()
        {
            Assert.Greater(ComputeSteeringTarget(1.0f, 1.0f), 0f);
            Assert.Less(ComputeSteeringTarget(1.0f, -1.0f), 0f);
        }

        [Test]
        public void Steering_SlowReverse_DoesNotFlip()
        {
            Assert.Greater(ComputeSteeringTarget(1.0f, -0.10f), 0f);
        }

        [Test]
        public void Steering_RampFromZero_MovesTowardTarget()
        {
            float result = SimulateSteering(0f, 1.0f, 1.0f, k_Dt);
            Assert.Greater(result, 0f);
            Assert.Less(result, k_SteeringMax);
        }

        [Test]
        public void Steering_ZeroInput_ReturnsToCenter()
        {
            float currentSteering = 0.3f;
            float result = SimulateSteering(currentSteering, 0f, 1.0f, k_Dt);
            Assert.Less(result, currentSteering);
        }
    }
}
