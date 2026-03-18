#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for AirPhysicsMath public functions.
    /// Tests verify physically correct behavior from inputs/outputs only.
    /// Uses realistic 1/10th scale RC car values throughout.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxAirPhysicsTests
    {
        // =====================================================================
        // AirPhysicsMath — ComputePitchTorque
        // =====================================================================

        [Test]
        public void PitchTorque_ThrottleOnly_PositivePitch()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(1.0f, 0f, 5f, 1f);
            Assert.Greater(torque, 0f,
                "Throttle should produce positive pitch torque (nose up)");
        }

        [Test]
        public void PitchTorque_BrakeOnly_NegativePitch()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(0f, 1.0f, 5f, 1f);
            Assert.Less(torque, 0f,
                "Brake should produce negative pitch torque (nose down)");
        }

        [Test]
        public void PitchTorque_EqualThrottleAndBrake_Zero()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(0.5f, 0.5f, 5f, 1f);
            Assert.AreEqual(0f, torque, k_Epsilon,
                "Equal throttle and brake should cancel out");
        }

        [Test]
        public void PitchTorque_ZeroInput_ZeroTorque()
        {
            float torque = AirPhysicsMath.ComputePitchTorque(0f, 0f, 5f, 1f);
            Assert.AreEqual(0f, torque, k_Epsilon);
        }

        [Test]
        public void PitchTorque_ScalesLinearlyWithSensitivity()
        {
            float t1 = AirPhysicsMath.ComputePitchTorque(1f, 0f, 5f, 1f);
            float t2 = AirPhysicsMath.ComputePitchTorque(1f, 0f, 5f, 2f);
            Assert.AreEqual(t2, t1 * 2f, k_Epsilon,
                "Pitch torque should scale linearly with sensitivity");
        }

        [Test]
        public void PitchTorque_ScalesLinearlyWithMaxTorque()
        {
            float t1 = AirPhysicsMath.ComputePitchTorque(1f, 0f, 5f, 1f);
            float t2 = AirPhysicsMath.ComputePitchTorque(1f, 0f, 10f, 1f);
            Assert.AreEqual(t2, t1 * 2f, k_Epsilon,
                "Pitch torque should scale linearly with max torque");
        }


        // =====================================================================
        // AirPhysicsMath — ComputeRollTorque
        // =====================================================================

        [Test]
        public void RollTorque_RightSteer_PositiveRoll()
        {
            float torque = AirPhysicsMath.ComputeRollTorque(1.0f, 5f, 1f);
            Assert.Greater(torque, 0f,
                "Right steer (+1) should produce positive roll torque");
        }

        [Test]
        public void RollTorque_LeftSteer_NegativeRoll()
        {
            float torque = AirPhysicsMath.ComputeRollTorque(-1.0f, 5f, 1f);
            Assert.Less(torque, 0f,
                "Left steer (-1) should produce negative roll torque");
        }

        [Test]
        public void RollTorque_ZeroSteer_ZeroTorque()
        {
            float torque = AirPhysicsMath.ComputeRollTorque(0f, 5f, 1f);
            Assert.AreEqual(0f, torque, k_Epsilon);
        }

        [Test]
        public void RollTorque_ScalesWithSensitivity()
        {
            float t1 = AirPhysicsMath.ComputeRollTorque(1f, 5f, 1f);
            float t2 = AirPhysicsMath.ComputeRollTorque(1f, 5f, 3f);
            Assert.AreEqual(t2, t1 * 3f, k_Epsilon);
        }


        // =====================================================================
        // AirPhysicsMath — ComputeGyroDampingFactor
        // =====================================================================

        [Test]
        public void GyroDamping_Below10Rpm_Zero()
        {
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(5f, 2f, 1000f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Below 10 RPM threshold should return zero gyro damping");
        }

        [Test]
        public void GyroDamping_At10Rpm_StillZero()
        {
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(10f, 2f, 1000f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "At exactly 10 RPM threshold should return zero (threshold is exclusive)");
        }

        [Test]
        public void GyroDamping_AtFullRpm_FullStrength()
        {
            float strength = 2.5f;
            float fullRpm = 1000f;
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(fullRpm, strength, fullRpm);
            Assert.AreEqual(strength, factor, k_Epsilon,
                "At full RPM should return full gyro strength");
        }

        [Test]
        public void GyroDamping_HalfRpm_HalfStrength()
        {
            float strength = 2.0f;
            float fullRpm = 1000f;
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(500f, strength, fullRpm);
            Assert.AreEqual(strength * 0.5f, factor, 0.01f,
                "Half RPM should give half strength (linear scaling)");
        }

        [Test]
        public void GyroDamping_AboveFullRpm_ClampedToFull()
        {
            float strength = 2.0f;
            float fullRpm = 1000f;
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(2000f, strength, fullRpm);
            Assert.AreEqual(strength, factor, k_Epsilon,
                "Above full RPM should clamp to full strength");
        }

        [Test]
        public void GyroDamping_ZeroGyroFullRpm_Zero()
        {
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(500f, 2f, 0f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Zero gyroFullRpm should return zero (no crash)");
        }

        [Test]
        public void GyroDamping_NegativeFullRpm_Zero()
        {
            float factor = AirPhysicsMath.ComputeGyroDampingFactor(500f, 2f, -100f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Negative gyroFullRpm should return zero");
        }


        // =====================================================================
        // AirPhysicsMath — ComputeAverageAbsRpm
        // =====================================================================

        [Test]
        public void AverageAbsRpm_MixedPositiveNegative_UsesAbsolute()
        {
            float[] rpms = { 100f, -200f, 300f, -400f };
            float expected = (100f + 200f + 300f + 400f) / 4f;
            float result = AirPhysicsMath.ComputeAverageAbsRpm(rpms);
            Assert.AreEqual(expected, result, k_Epsilon,
                "Should average absolute values of all RPMs");
        }

        [Test]
        public void AverageAbsRpm_EmptyArray_Zero()
        {
            float result = AirPhysicsMath.ComputeAverageAbsRpm(new float[0]);
            Assert.AreEqual(0f, result, k_Epsilon,
                "Empty array should return zero");
        }

        [Test]
        public void AverageAbsRpm_Null_Zero()
        {
            float result = AirPhysicsMath.ComputeAverageAbsRpm(null);
            Assert.AreEqual(0f, result, k_Epsilon,
                "Null should return zero");
        }

        [Test]
        public void AverageAbsRpm_SingleElement()
        {
            float result = AirPhysicsMath.ComputeAverageAbsRpm(new[] { -500f });
            Assert.AreEqual(500f, result, k_Epsilon,
                "Single element should return its absolute value");
        }

        [Test]
        public void AverageAbsRpm_AllZeros_Zero()
        {
            float result = AirPhysicsMath.ComputeAverageAbsRpm(new[] { 0f, 0f, 0f, 0f });
            Assert.AreEqual(0f, result, k_Epsilon);
        }
    }
}

#pragma warning restore CS0618
