using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for GyroscopicMath.ComputeGyroscopicTorque.</summary>
    public class GyroscopicTorqueTests
    {
        const float k_Epsilon = 0.0001f;
        const float k_WheelMoI = 0.120f;
        const float k_SpinRate = 9.04f;

        [Test]
        public void ComputeGyroscopicTorque_YawWithSpinningWheels_ProducesPitchTorque()
        {
            Vector3 bodyOmega = new Vector3(0f, 2f, 0f);
            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(bodyOmega, Vector3.right, k_WheelMoI, k_SpinRate);
            Assert.AreEqual(0f, torque.x, k_Epsilon);
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreNotEqual(0f, torque.z, "Z (pitch) should be non-zero");
        }

        [Test]
        public void ComputeGyroscopicTorque_PitchWithSpinningWheels_ProducesYawTorque()
        {
            Vector3 bodyOmega = new Vector3(0f, 0f, 2f);
            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(bodyOmega, Vector3.right, k_WheelMoI, k_SpinRate);
            Assert.AreEqual(0f, torque.x, k_Epsilon);
            Assert.AreNotEqual(0f, torque.y, "Y (yaw) should be non-zero");
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeGyroscopicTorque_RollWithSpinningWheels_ProducesZeroTorque()
        {
            Vector3 bodyOmega = new Vector3(2f, 0f, 0f);
            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(bodyOmega, Vector3.right, k_WheelMoI, k_SpinRate);
            Assert.AreEqual(0f, torque.x, k_Epsilon);
            Assert.AreEqual(0f, torque.y, k_Epsilon);
            Assert.AreEqual(0f, torque.z, k_Epsilon);
        }

        [Test]
        public void ComputeGyroscopicTorque_ZeroSpin_ReturnsZero()
        {
            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(new Vector3(0f, 2f, 0f), Vector3.right, k_WheelMoI, 0f);
            Assert.AreEqual(Vector3.zero, torque);
        }

        [Test]
        public void ComputeGyroscopicTorque_ZeroBodyRotation_ReturnsZero()
        {
            Vector3 torque = GyroscopicMath.ComputeGyroscopicTorque(Vector3.zero, Vector3.right, k_WheelMoI, k_SpinRate);
            Assert.AreEqual(Vector3.zero, torque);
        }
    }
}
