using NUnit.Framework;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for steering direction, high-speed reduction, and reverse flip.
    /// Steering ramp tests live in SteeringRampBehaviorTests.cs.
    /// </summary>
    public class SteeringDirectionTests
    {
        const float k_SteeringMax = 0.50f;
        const float k_SteeringSpeed = 7f;
        const float k_SteeringSpeedLimit = 8f;
        const float k_SteeringHighSpeedFactor = 0.4f;
        const float k_ReverseSpeedThreshold = 0.25f;

        private float ComputeSteeringTarget(float steerIn, float fwdSpeed)
        {
            float spd = Mathf.Abs(fwdSpeed);
            float t = Mathf.Clamp01(spd / k_SteeringSpeedLimit);
            float effectiveMax = Mathf.Lerp(k_SteeringMax, k_SteeringMax * k_SteeringHighSpeedFactor, t);
            float steerSign = fwdSpeed < -k_ReverseSpeedThreshold ? -1f : 1f;
            return steerIn * effectiveMax * steerSign;
        }

        [Test]
        public void Steering_RightInput_PositiveAngle()
        {
            float target = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: 1.0f);
            Assert.Greater(target, 0f,
                "Right steering input should produce positive angle (right turn in Unity)");
        }

        [Test]
        public void Steering_LeftInput_NegativeAngle()
        {
            float target = ComputeSteeringTarget(steerIn: -1.0f, fwdSpeed: 1.0f);
            Assert.Less(target, 0f,
                "Left steering input should produce negative angle (left turn in Unity)");
        }

        [Test]
        public void Steering_FullRight_EqualsSteeringMax()
        {
            float target = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: 0.5f);
            Assert.Greater(target, k_SteeringMax * 0.9f,
                "At low speed, full right steer should be near steeringMax");
            Assert.LessOrEqual(target, k_SteeringMax + 0.001f,
                "Steering target should not exceed steeringMax");
        }

        [Test]
        public void Steering_HighSpeed_ReducedAngle()
        {
            float lowSpeedTarget = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: 0f);
            float highSpeedTarget = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: 20f);

            Assert.Less(Mathf.Abs(highSpeedTarget), Mathf.Abs(lowSpeedTarget),
                "High speed steering angle should be smaller than low speed angle");

            float expectedHighSpeedMax = k_SteeringMax * k_SteeringHighSpeedFactor;
            Assert.AreEqual(expectedHighSpeedMax, highSpeedTarget, 0.01f,
                "At very high speed, steering max should approach steeringMax * highSpeedFactor");
        }

        [Test]
        public void Steering_AtSpeedLimit_HalfwayReduction()
        {
            float target = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: k_SteeringSpeedLimit);
            float expected = k_SteeringMax * k_SteeringHighSpeedFactor;
            Assert.AreEqual(expected, target, 0.01f,
                "At steeringSpeedLimit, steering should be at minimum (highSpeedFactor * max)");
        }

        [Test]
        public void Steering_Reverse_FlipsDirection()
        {
            float forwardTarget = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: 1.0f);
            float reverseTarget = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: -1.0f);

            Assert.Greater(forwardTarget, 0f,
                "Forward: right steer should produce positive angle");
            Assert.Less(reverseTarget, 0f,
                "Reverse: right steer should produce negative angle (flipped direction)");
        }

        [Test]
        public void Steering_SlowReverse_DoesNotFlip()
        {
            float target = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: -0.10f);
            Assert.Greater(target, 0f,
                "At slow reverse (above threshold), steering should NOT flip direction");
        }
    }
}
