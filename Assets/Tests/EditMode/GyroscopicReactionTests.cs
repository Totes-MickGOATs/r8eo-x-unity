using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for GyroscopicMath.ComputeReactionTorque and ComputeWheelAngularVelocity.</summary>
    public class GyroscopicReactionTests
    {
        const float k_Epsilon = 0.0001f;
        const float k_WheelMoI = 0.120f;
        const float k_WheelRadius = 0.420f;
        const float k_DeltaTime = 0.02f;

        [Test]
        public void ComputeReactionTorque_SpinIncrease_ReturnsOpposingTorque()
        {
            Vector3 torque = GyroscopicMath.ComputeReactionTorque(
                Vector3.right, k_WheelMoI, 300f, 250f, k_DeltaTime);
            Assert.Less(torque.x, 0f, "Reaction should oppose spin increase (negative X)");
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeReactionTorque_SpinDecrease_ReturnsOpposingTorque()
        {
            Vector3 torque = GyroscopicMath.ComputeReactionTorque(
                Vector3.right, k_WheelMoI, 200f, 300f, k_DeltaTime);
            Assert.Greater(torque.x, 0f, "Reaction should oppose spin decrease (positive X)");
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeReactionTorque_NoSpinChange_ReturnsZero()
        {
            Vector3 torque = GyroscopicMath.ComputeReactionTorque(
                Vector3.right, k_WheelMoI, 283f, 283f, k_DeltaTime);
            Assert.AreEqual(0f, torque.x, k_Epsilon);
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeReactionTorque_BrakeIsStrongerThanThrottle()
        {
            Vector3 throttleTorque = GyroscopicMath.ComputeReactionTorque(
                Vector3.right, k_WheelMoI, 300f, 270f, k_DeltaTime);
            Vector3 brakeTorque = GyroscopicMath.ComputeReactionTorque(
                Vector3.right, k_WheelMoI, 200f, 270f, k_DeltaTime);
            Assert.Greater(brakeTorque.magnitude, throttleTorque.magnitude,
                "Brake reaction torque should be larger than throttle");
        }

        [Test]
        public void ComputeWheelAngularVelocity_KnownSpeed_ReturnsCorrectRadPerSec()
        {
            float speed = 15f;
            float omega = GyroscopicMath.ComputeWheelAngularVelocity(speed, k_WheelRadius);
            Assert.AreEqual(speed / k_WheelRadius, omega, 0.01f);
        }
    }
}
