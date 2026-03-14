using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for tire grip calculations.
    /// Covers slip ratio, lateral force, longitudinal friction, and RPM conversion.
    /// </summary>
    public class GripMathTests
    {
        const float k_GripCoeff = 0.7f;
        const float k_ZTraction = 0.10f;
        const float k_ZBrakeTraction = 0.5f;
        const float k_StaticFrictionSpeed = 0.5f;
        const float k_StaticFrictionTraction = 5.0f;
        const float k_WheelRadius = 0.166f;


        // ---- ComputeSlipRatio ----

        [Test]
        public void ComputeSlipRatio_NoLateralVelocity_ReturnsZero()
        {
            float result = GripMath.ComputeSlipRatio(0f, 5f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void ComputeSlipRatio_FullSideways_ReturnsOne()
        {
            // All velocity is lateral
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
            // Lateral velocity exceeds total speed (shouldn't happen physically, but test clamp)
            float result = GripMath.ComputeSlipRatio(10f, 5f);
            Assert.AreEqual(1f, result, 0.0001f);
        }


        // ---- ComputeLateralForceMagnitude ----

        [Test]
        public void ComputeLateralForceMagnitude_PositiveLateral_ReturnsNegative()
        {
            // Force opposes lateral motion
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
            // F = -2 * 0.8 * 0.7 * 10 = -11.2 N
            float force = GripMath.ComputeLateralForceMagnitude(2f, 0.8f, k_GripCoeff, 10f);
            Assert.AreEqual(-11.2f, force, 0.01f);
        }


        // ---- ComputeEffectiveTraction ----

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


        // ---- ComputeLongitudinalForceMagnitude ----

        [Test]
        public void ComputeLongitudinalForceMagnitude_ForwardMotion_OpposesDirection()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(5f, k_ZTraction, k_GripCoeff, 10f);
            Assert.Less(force, 0f, "Longitudinal friction should oppose forward motion");
        }

        [Test]
        public void ComputeLongitudinalForceMagnitude_KnownValues_MatchesExpected()
        {
            // F = -5 * 0.1 * 0.7 * 10 = -3.5 N
            float force = GripMath.ComputeLongitudinalForceMagnitude(5f, k_ZTraction, k_GripCoeff, 10f);
            Assert.AreEqual(-3.5f, force, 0.01f);
        }


        // ---- ComputeWheelRpm ----

        [Test]
        public void ComputeWheelRpm_KnownSpeed_MatchesExpected()
        {
            // At 5 m/s with radius 0.166m:
            // angular_vel = 5 / 0.166 = 30.12 rad/s
            // RPM = 30.12 * 60 / (2*PI) = 287.6
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
