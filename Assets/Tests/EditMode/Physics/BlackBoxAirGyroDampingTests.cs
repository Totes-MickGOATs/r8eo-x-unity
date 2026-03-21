#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for AirPhysicsMath gyro damping and average RPM functions.
    /// Pitch and roll torque tests live in BlackBoxAirPitchRollTests.cs.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxAirGyroDampingTests
    {
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
            Assert.AreEqual(0f, AirPhysicsMath.ComputeGyroDampingFactor(10f, 2f, 1000f), k_Epsilon,
                "At exactly 10 RPM threshold should return zero (threshold is exclusive)");
        }

        [Test]
        public void GyroDamping_AtFullRpm_FullStrength()
        {
            Assert.AreEqual(2.5f, AirPhysicsMath.ComputeGyroDampingFactor(1000f, 2.5f, 1000f), k_Epsilon,
                "At full RPM should return full gyro strength");
        }

        [Test]
        public void GyroDamping_HalfRpm_HalfStrength()
        {
            Assert.AreEqual(1.0f, AirPhysicsMath.ComputeGyroDampingFactor(500f, 2.0f, 1000f), 0.01f,
                "Half RPM should give half strength (linear scaling)");
        }

        [Test]
        public void GyroDamping_AboveFullRpm_ClampedToFull()
        {
            Assert.AreEqual(2.0f, AirPhysicsMath.ComputeGyroDampingFactor(2000f, 2.0f, 1000f), k_Epsilon,
                "Above full RPM should clamp to full strength");
        }

        [Test]
        public void GyroDamping_ZeroGyroFullRpm_Zero()
        {
            Assert.AreEqual(0f, AirPhysicsMath.ComputeGyroDampingFactor(500f, 2f, 0f), k_Epsilon,
                "Zero gyroFullRpm should return zero (no crash)");
        }

        [Test]
        public void GyroDamping_NegativeFullRpm_Zero()
        {
            Assert.AreEqual(0f, AirPhysicsMath.ComputeGyroDampingFactor(500f, 2f, -100f), k_Epsilon,
                "Negative gyroFullRpm should return zero");
        }

        [Test]
        public void AverageAbsRpm_MixedPositiveNegative_UsesAbsolute()
        {
            float[] rpms = { 100f, -200f, 300f, -400f };
            float expected = (100f + 200f + 300f + 400f) / 4f;
            Assert.AreEqual(expected, AirPhysicsMath.ComputeAverageAbsRpm(rpms), k_Epsilon,
                "Should average absolute values of all RPMs");
        }

        [Test]
        public void AverageAbsRpm_EmptyArray_Zero()
        {
            Assert.AreEqual(0f, AirPhysicsMath.ComputeAverageAbsRpm(new float[0]), k_Epsilon,
                "Empty array should return zero");
        }

        [Test]
        public void AverageAbsRpm_Null_Zero()
        {
            Assert.AreEqual(0f, AirPhysicsMath.ComputeAverageAbsRpm(null), k_Epsilon,
                "Null should return zero");
        }

        [Test]
        public void AverageAbsRpm_SingleElement()
        {
            Assert.AreEqual(500f, AirPhysicsMath.ComputeAverageAbsRpm(new[] { -500f }), k_Epsilon,
                "Single element should return its absolute value");
        }

        [Test]
        public void AverageAbsRpm_AllZeros_Zero()
        {
            Assert.AreEqual(0f, AirPhysicsMath.ComputeAverageAbsRpm(new[] { 0f, 0f, 0f, 0f }), k_Epsilon);
        }
    }
}

#pragma warning restore CS0618
