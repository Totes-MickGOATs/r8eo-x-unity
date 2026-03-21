using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for GripMath traction, longitudinal force, and RPM calculations.</summary>
    public class GripMathTractionTests
    {
        const float k_GripCoeff = 0.7f;
        const float k_ZTraction = 0.10f;
        const float k_ZBrakeTraction = 0.5f;
        const float k_StaticFrictionSpeed = 0.5f;
        const float k_StaticFrictionTraction = 5.0f;
        const float k_WheelRadius = 0.166f;

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
            Assert.Less(force, 0f);
        }

        [Test]
        public void ComputeLongitudinalForceMagnitude_KnownValues_MatchesExpected()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(5f, k_ZTraction, k_GripCoeff, 10f);
            Assert.AreEqual(-3.5f, force, 0.01f);
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
            Assert.AreEqual(0f, GripMath.ComputeWheelRpm(0f, k_WheelRadius), 0.0001f);
        }

        [Test]
        public void ComputeWheelRpm_ZeroRadius_ReturnsZero()
        {
            Assert.AreEqual(0f, GripMath.ComputeWheelRpm(5f, 0f), 0.0001f);
        }
    }
}
