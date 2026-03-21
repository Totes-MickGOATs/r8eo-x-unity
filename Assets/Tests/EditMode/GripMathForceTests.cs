using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for GripMath lateral force, effective traction, and longitudinal force.
    /// SlipRatio and WheelRpm tests live in GripMathSlipRatioTests.cs.
    /// </summary>
    public class GripMathForceTests
    {
        const float k_GripCoeff = 0.7f;
        const float k_ZTraction = 0.10f;
        const float k_ZBrakeTraction = 0.5f;
        const float k_StaticFrictionSpeed = 0.5f;
        const float k_StaticFrictionTraction = 5.0f;

        [Test]
        public void ComputeLateralForceMagnitude_PositiveLateral_ReturnsNegative()
        {
            float force = GripMath.ComputeLateralForceMagnitude(2f, 0.8f, k_GripCoeff, 10f);
            Assert.Less(force, 0f, "Lateral force should oppose positive lateral velocity");
        }

        [Test]
        public void ComputeLateralForceMagnitude_NegativeLateral_ReturnsPositive()
        {
            float force = GripMath.ComputeLateralForceMagnitude(-2f, 0.8f, k_GripCoeff, 10f);
            Assert.Greater(force, 0f, "Lateral force should oppose negative lateral velocity");
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

        [Test]
        public void ComputeEffectiveTraction_NormalDriving_ReturnsZTraction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                false, 5f, 10f, k_ZTraction, k_ZBrakeTraction,
                k_StaticFrictionSpeed, k_StaticFrictionTraction);
            Assert.AreEqual(k_ZTraction, traction, 0.0001f);
        }

        [Test]
        public void ComputeEffectiveTraction_Braking_ReturnsBrakeTraction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                true, 5f, 0f, k_ZTraction, k_ZBrakeTraction,
                k_StaticFrictionSpeed, k_StaticFrictionTraction);
            Assert.AreEqual(k_ZBrakeTraction, traction, 0.0001f);
        }

        [Test]
        public void ComputeEffectiveTraction_StoppedNoEngine_ReturnsStaticFriction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                false, 0.1f, 0f, k_ZTraction, k_ZBrakeTraction,
                k_StaticFrictionSpeed, k_StaticFrictionTraction);
            Assert.AreEqual(k_StaticFrictionTraction, traction, 0.0001f);
        }

        [Test]
        public void ComputeEffectiveTraction_StoppedWithEngine_ReturnsNormalTraction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                false, 0.1f, 10f, k_ZTraction, k_ZBrakeTraction,
                k_StaticFrictionSpeed, k_StaticFrictionTraction);
            Assert.AreEqual(k_ZTraction, traction, 0.0001f);
        }

        [Test]
        public void ComputeLongitudinalForceMagnitude_ForwardMotion_OpposesDirection()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(5f, k_ZTraction, k_GripCoeff, 10f);
            Assert.Less(force, 0f, "Longitudinal friction should oppose forward motion");
        }

        [Test]
        public void ComputeLongitudinalForceMagnitude_KnownValues_MatchesExpected()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(5f, k_ZTraction, k_GripCoeff, 10f);
            Assert.AreEqual(-3.5f, force, 0.01f);
        }
    }
}
