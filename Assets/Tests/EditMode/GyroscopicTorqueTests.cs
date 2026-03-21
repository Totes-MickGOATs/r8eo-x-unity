using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for GyroscopicMath.ComputeGyroscopicTorque and
    /// GyroscopicMath.ComputeWheelAngularVelocity.
    /// Reaction-torque tests live in GyroscopicReactionTests.cs.
    /// </summary>
    public class GyroscopicTorqueTests
    {
        const float k_Epsilon = 0.0001f;
        const float k_WheelMoI = 0.120f;
        const float k_WheelRadius = 0.420f;
        const float k_SpinRate = 9.04f;

        [Test]
        public void ComputeGyroscopicTorque_YawWithSpinningWheels_ProducesPitchTorque()
        {
            Vector3 bodyOmega = new Vector3(0f, 2f, 0f);
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, k_SpinRate);

            Assert.AreEqual(0f, torque.x, k_Epsilon, "X should be zero");
            Assert.AreEqual(0f, torque.y, k_Epsilon, "Y should be zero");
            Assert.AreNotEqual(0f, torque.z, "Z (pitch) should be non-zero");
        }

        [Test]
        public void ComputeGyroscopicTorque_PitchWithSpinningWheels_ProducesYawTorque()
        {
            Vector3 bodyOmega = new Vector3(0f, 0f, 2f);
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, k_SpinRate);

            Assert.AreEqual(0f, torque.x, k_Epsilon, "X should be zero");
            Assert.AreNotEqual(0f, torque.y, "Y (yaw) should be non-zero");
            Assert.AreEqual(0f, torque.z, k_Epsilon, "Z should be zero");
        }

        [Test]
        public void ComputeGyroscopicTorque_RollWithSpinningWheels_ProducesZeroTorque()
        {
            Vector3 bodyOmega = new Vector3(2f, 0f, 0f);
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, k_SpinRate);

            Assert.AreEqual(0f, torque.x, k_Epsilon);
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeGyroscopicTorque_ZeroSpin_ReturnsZero()
        {
            Vector3 bodyOmega = new Vector3(0f, 2f, 0f);
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, 0f);

            Assert.AreEqual(Vector3.zero, torque);
        }

        [Test]
        public void ComputeGyroscopicTorque_ZeroBodyRotation_ReturnsZero()
        {
            Vector3 bodyOmega = Vector3.zero;
            Vector3 spinAxis = Vector3.right;

            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(
                bodyOmega, spinAxis, k_WheelMoI, k_SpinRate);

            Assert.AreEqual(Vector3.zero, torque);
        }

        [Test]
        public void ComputeWheelAngularVelocity_KnownSpeed_ReturnsCorrectRadPerSec()
        {
            float speed = 15f;
            float omega = GyroscopicMath.ComputeWheelAngularVelocity(speed, k_WheelRadius);

            float expected = speed / k_WheelRadius;
            Assert.AreEqual(expected, omega, 0.01f);
        }
    }
}
