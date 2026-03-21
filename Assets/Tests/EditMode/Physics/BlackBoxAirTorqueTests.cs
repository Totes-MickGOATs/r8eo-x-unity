#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Black-box unit tests for AirPhysicsMath pitch and roll torque.</summary>
    [Category("Fast")]
    public class BlackBoxAirTorqueTests
    {
        // =====================================================================
        // AirPhysicsMath — ComputePitchTorque
        // =====================================================================

        [Test]
        public void PitchTorque_ThrottleOnly_PositivePitch()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(1.0f, 0f, 5f, 1f);
            Assert.Greater(torque, 0f, "Throttle should produce positive pitch torque (nose up)");
        }

        [Test]
        public void PitchTorque_BrakeOnly_NegativePitch()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(0f, 1.0f, 5f, 1f);
            Assert.Less(torque, 0f, "Brake should produce negative pitch torque (nose down)");
        }

        [Test]
        public void PitchTorque_EqualThrottleAndBrake_Zero()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(0.5f, 0.5f, 5f, 1f);
            Assert.AreEqual(0f, torque, k_Epsilon, "Equal throttle and brake should cancel out");
        }

        [Test]
        public void PitchTorque_ZeroInput_ZeroTorque()
        {
            Assert.AreEqual(0f, AirPhysicsMath.ComputePitchTorque(0f, 0f, 5f, 1f), k_Epsilon);
        }

        [Test]
        public void PitchTorque_ScalesLinearlyWithSensitivity()
        {
            float t1 = AirPhysicsMath.ComputePitchTorque(1f, 0f, 5f, 1f);
            float t2 = AirPhysicsMath.ComputePitchTorque(1f, 0f, 5f, 2f);
            Assert.AreEqual(t2, t1 * 2f, k_Epsilon, "Pitch torque should scale linearly with sensitivity");
        }

        [Test]
        public void PitchTorque_ScalesLinearlyWithMaxTorque()
        {
            float t1 = AirPhysicsMath.ComputePitchTorque(1f, 0f, 5f, 1f);
            float t2 = AirPhysicsMath.ComputePitchTorque(1f, 0f, 10f, 1f);
            Assert.AreEqual(t2, t1 * 2f, k_Epsilon, "Pitch torque should scale linearly with max torque");
        }

        // =====================================================================
        // AirPhysicsMath — ComputeRollTorque
        // =====================================================================

        [Test]
        public void RollTorque_RightSteer_PositiveRoll()
        {
            Assert.Greater(AirPhysicsMath.ComputeRollTorque(1.0f, 5f, 1f), 0f,
                "Right steer (+1) should produce positive roll torque");
        }

        [Test]
        public void RollTorque_LeftSteer_NegativeRoll()
        {
            Assert.Less(AirPhysicsMath.ComputeRollTorque(-1.0f, 5f, 1f), 0f,
                "Left steer (-1) should produce negative roll torque");
        }

        [Test]
        public void RollTorque_ZeroSteer_ZeroTorque()
        {
            Assert.AreEqual(0f, AirPhysicsMath.ComputeRollTorque(0f, 5f, 1f), k_Epsilon);
        }

        [Test]
        public void RollTorque_ScalesWithSensitivity()
        {
            float t1 = AirPhysicsMath.ComputeRollTorque(1f, 5f, 1f);
            float t2 = AirPhysicsMath.ComputeRollTorque(1f, 5f, 3f);
            Assert.AreEqual(t2, t1 * 3f, k_Epsilon);
        }
    }
}

#pragma warning restore CS0618
