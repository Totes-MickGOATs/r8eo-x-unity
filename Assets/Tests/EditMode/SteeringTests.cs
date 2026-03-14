using NUnit.Framework;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for steering direction and speed-dependent angle reduction.
    /// Tests the pure math from RCCar.ApplySteering without MonoBehaviour.
    /// </summary>
    public class SteeringTests
    {
        // ---- Constants matching RCCar defaults ----
        const float k_SteeringMax = 0.50f;        // Max steering angle in radians (~29 deg)
        const float k_SteeringSpeed = 7f;          // Steering ramp speed in rad/s
        const float k_SteeringSpeedLimit = 8f;     // Speed in m/s at which steering reduces
        const float k_SteeringHighSpeedFactor = 0.4f; // Fraction of steeringMax kept at high speed
        const float k_ReverseSpeedThreshold = 0.25f;
        const float k_Dt = 0.008333f;              // 120 Hz timestep


        /// <summary>
        /// Computes the steering target angle, mirroring RCCar.ApplySteering logic.
        /// Returns the target steering angle in radians.
        /// </summary>
        private float ComputeSteeringTarget(float steerIn, float fwdSpeed)
        {
            float spd = Mathf.Abs(fwdSpeed);
            float t = Mathf.Clamp01(spd / k_SteeringSpeedLimit);
            float effectiveMax = Mathf.Lerp(k_SteeringMax, k_SteeringMax * k_SteeringHighSpeedFactor, t);
            float steerSign = fwdSpeed < -k_ReverseSpeedThreshold ? -1f : 1f;
            return steerIn * effectiveMax * steerSign;
        }

        /// <summary>
        /// Simulates ApplySteering with ramp-toward-target, mirroring RCCar exactly.
        /// </summary>
        private float SimulateSteering(float currentSteering, float steerIn, float fwdSpeed, float dt)
        {
            float target = ComputeSteeringTarget(steerIn, fwdSpeed);
            return Mathf.MoveTowards(currentSteering, target, k_SteeringSpeed * dt);
        }


        // ---- Steering Direction ----

        [Test]
        public void Steering_RightInput_PositiveAngle()
        {
            // steerIn=1.0 should produce positive steering angle
            // In Unity: positive Y rotation = turn right when viewed from above
            float target = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: 1.0f);

            Assert.Greater(target, 0f,
                "Right steering input should produce positive angle (right turn in Unity)");
        }

        [Test]
        public void Steering_LeftInput_NegativeAngle()
        {
            // steerIn=-1.0 should produce negative angle
            float target = ComputeSteeringTarget(steerIn: -1.0f, fwdSpeed: 1.0f);

            Assert.Less(target, 0f,
                "Left steering input should produce negative angle (left turn in Unity)");
        }

        [Test]
        public void Steering_FullRight_EqualsSteeringMax()
        {
            // At low speed, full right should give full steering angle
            float target = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: 0.5f);

            // At 0.5 m/s, t = 0.5/8.0 = 0.0625, effectiveMax ≈ 0.481
            // Close to steeringMax but slightly reduced
            Assert.Greater(target, k_SteeringMax * 0.9f,
                "At low speed, full right steer should be near steeringMax");
            Assert.LessOrEqual(target, k_SteeringMax + 0.001f,
                "Steering target should not exceed steeringMax");
        }


        // ---- Speed-Dependent Reduction ----

        [Test]
        public void Steering_HighSpeed_ReducedAngle()
        {
            // At high speed, effective max angle should be reduced by steeringHighSpeedFactor
            float lowSpeedTarget = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: 0f);
            float highSpeedTarget = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: 20f);

            Assert.Less(Mathf.Abs(highSpeedTarget), Mathf.Abs(lowSpeedTarget),
                "High speed steering angle should be smaller than low speed angle");

            // At speed >> steeringSpeedLimit, t ≈ 1.0, effectiveMax ≈ steeringMax * 0.4
            float expectedHighSpeedMax = k_SteeringMax * k_SteeringHighSpeedFactor;
            Assert.AreEqual(expectedHighSpeedMax, highSpeedTarget, 0.01f,
                "At very high speed, steering max should approach steeringMax * highSpeedFactor");
        }

        [Test]
        public void Steering_AtSpeedLimit_HalfwayReduction()
        {
            // At exactly the speed limit, t=1.0, should give full reduction
            float target = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: k_SteeringSpeedLimit);

            float expected = k_SteeringMax * k_SteeringHighSpeedFactor;
            Assert.AreEqual(expected, target, 0.01f,
                "At steeringSpeedLimit, steering should be at minimum (highSpeedFactor * max)");
        }


        // ---- Reverse Steering Flip ----

        [Test]
        public void Steering_Reverse_FlipsDirection()
        {
            // When fwdSpeed < -0.25, steering direction should flip
            // This is essential for intuitive reverse driving
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
            // When fwdSpeed is negative but above -0.25 threshold, steering should NOT flip
            float target = ComputeSteeringTarget(steerIn: 1.0f, fwdSpeed: -0.10f);

            Assert.Greater(target, 0f,
                "At slow reverse (above threshold), steering should NOT flip direction");
        }


        // ---- Steering Ramp ----

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
            float currentSteering = 0.3f; // Currently turned
            float result = SimulateSteering(currentSteering, steerIn: 0f, fwdSpeed: 1.0f, k_Dt);

            Assert.Less(result, currentSteering,
                "Steering should move toward zero when input is zero");
        }
    }
}
