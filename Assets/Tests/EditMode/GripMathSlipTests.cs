using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for GripMath slip ratio and lateral force calculations.</summary>
    public class GripMathSlipTests
    {
        const float k_GripCoeff = 0.7f;

        [Test]
        public void ComputeSlipRatio_NoLateralVelocity_ReturnsZero()
        {
            Assert.AreEqual(0f, GripMath.ComputeSlipRatio(0f, 5f), 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_FullSideways_ReturnsOne()
        {
            Assert.AreEqual(1f, GripMath.ComputeSlipRatio(5f, 5f), 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_HalfSideways_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, GripMath.ComputeSlipRatio(2.5f, 5f), 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_ZeroSpeed_ReturnsZero()
        {
            Assert.AreEqual(0f, GripMath.ComputeSlipRatio(3f, 0f), 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_NegativeLateral_UsesAbsoluteValue()
        {
            Assert.AreEqual(0.5f, GripMath.ComputeSlipRatio(-3f, 6f), 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_ExceedsSpeed_ClampsToOne()
        {
            Assert.AreEqual(1f, GripMath.ComputeSlipRatio(10f, 5f), 0.0001f);
        }

        [Test]
        public void ComputeLateralForceMagnitude_PositiveLateral_ReturnsNegative()
        {
            float force = GripMath.ComputeLateralForceMagnitude(2f, 0.8f, k_GripCoeff, 10f);
            Assert.Less(force, 0f);
        }

        [Test]
        public void ComputeLateralForceMagnitude_NegativeLateral_ReturnsPositive()
        {
            float force = GripMath.ComputeLateralForceMagnitude(-2f, 0.8f, k_GripCoeff, 10f);
            Assert.Greater(force, 0f);
        }

        [Test]
        public void ComputeLateralForceMagnitude_ZeroGripLoad_ReturnsZero()
        {
            float force = GripMath.ComputeLateralForceMagnitude(2f, 0.8f, k_GripCoeff, 0f);
            Assert.AreEqual(0f, force, 0.0001f);
        }

        [Test]
        public void ComputeLateralForceMagnitude_ZeroGripFactor_ReturnsZero()
        {
            float force = GripMath.ComputeLateralForceMagnitude(2f, 0f, k_GripCoeff, 10f);
            Assert.AreEqual(0f, force, 0.0001f);
        }

        [Test]
        public void ComputeLateralForceMagnitude_KnownValues_MatchesExpected()
        {
            float force = GripMath.ComputeLateralForceMagnitude(2f, 0.8f, k_GripCoeff, 10f);
            Assert.AreEqual(-11.2f, force, 0.01f);
        }
    }
}
