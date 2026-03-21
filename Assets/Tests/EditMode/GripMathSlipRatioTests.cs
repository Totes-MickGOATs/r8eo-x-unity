using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for GripMath.ComputeSlipRatio and GripMath.ComputeWheelRpm.
    /// Force and traction tests live in GripMathForceTests.cs.
    /// </summary>
    public class GripMathSlipRatioTests
    {
        const float k_WheelRadius = 0.166f;

        [Test]
        public void ComputeSlipRatio_NoLateralVelocity_ReturnsZero()
        {
            float result = GripMath.ComputeSlipRatio(0f, 5f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_FullSideways_ReturnsOne()
        {
            float result = GripMath.ComputeSlipRatio(5f, 5f);
            Assert.AreEqual(1f, result, 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_HalfSideways_ReturnsHalf()
        {
            float result = GripMath.ComputeSlipRatio(2.5f, 5f);
            Assert.AreEqual(0.5f, result, 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_ZeroSpeed_ReturnsZero()
        {
            float result = GripMath.ComputeSlipRatio(3f, 0f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_NegativeLateral_UsesAbsoluteValue()
        {
            float result = GripMath.ComputeSlipRatio(-3f, 6f);
            Assert.AreEqual(0.5f, result, 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_ExceedsSpeed_ClampsToOne()
        {
            float result = GripMath.ComputeSlipRatio(10f, 5f);
            Assert.AreEqual(1f, result, 0.0001f);
        }

        [Test]
        public void ComputeWheelRpm_KnownSpeed_MatchesExpected()
        {
            float rpm = GripMath.ComputeWheelRpm(5f, k_WheelRadius);
            Assert.AreEqual(287.6f, rpm, 1f);
        }

        [Test]
        public void ComputeWheelRpm_ZeroSpeed_ReturnsZero()
        {
            float rpm = GripMath.ComputeWheelRpm(0f, k_WheelRadius);
            Assert.AreEqual(0f, rpm, 0.0001f);
        }

        [Test]
        public void ComputeWheelRpm_ZeroRadius_ReturnsZero()
        {
            float rpm = GripMath.ComputeWheelRpm(5f, 0f);
            Assert.AreEqual(0f, rpm, 0.0001f);
        }
    }
}
