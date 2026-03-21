using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for GyroscopicMath.ComputeReactionTorque.
    /// Gyroscopic-torque and angular-velocity tests live in GyroscopicTorqueTests.cs.
    /// </summary>
    public class GyroscopicReactionTests
    {
        const float k_Epsilon = 0.0001f;
        const float k_WheelMoI = 0.120f;
        const float k_DeltaTime = 0.02f;

        [Test]
        public void ComputeReactionTorque_SpinIncrease_ReturnsOpposingTorque()
        {
            Vector3 spinAxis = Vector3.right;
            float currentSpin = 300f;
            float previousSpin = 250f;

            Vector3 torque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, currentSpin, previousSpin, k_DeltaTime);

            Assert.Less(torque.x, 0f, "Reaction should oppose spin increase (negative X)");
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeReactionTorque_SpinDecrease_ReturnsOpposingTorque()
        {
            Vector3 spinAxis = Vector3.right;
            float currentSpin = 200f;
            float previousSpin = 300f;

            Vector3 torque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, currentSpin, previousSpin, k_DeltaTime);

            Assert.Greater(torque.x, 0f, "Reaction should oppose spin decrease (positive X)");
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeReactionTorque_NoSpinChange_ReturnsZero()
        {
            Vector3 spinAxis = Vector3.right;
            float spinRate = 283f;

            Vector3 torque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, spinRate, spinRate, k_DeltaTime);

            Assert.AreEqual(0f, torque.x, k_Epsilon);
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeReactionTorque_BrakeIsStrongerThanThrottle()
        {
            Vector3 spinAxis = Vector3.right;

            float throttleCurrent = 300f;
            float throttlePrevious = 270f;
            Vector3 throttleTorque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, throttleCurrent, throttlePrevious, k_DeltaTime);

            float brakeCurrent = 200f;
            float brakePrevious = 270f;
            Vector3 brakeTorque = GyroscopicMath.ComputeReactionTorque(
                spinAxis, k_WheelMoI, brakeCurrent, brakePrevious, k_DeltaTime);

            Assert.Greater(brakeTorque.magnitude, throttleTorque.magnitude,
                "Brake reaction torque should be larger than throttle (larger delta-omega)");
        }
    }
}
