#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for GripMath public functions.
    /// Tests verify physically correct behavior from inputs/outputs only.
    /// Uses realistic 1/10th scale RC car values throughout.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxGripTests
    {
        // =====================================================================
        // GripMath — ComputeSlipRatio
        // =====================================================================

        [Test]
        public void SlipRatio_StraightLine_ZeroSlip()
        {
            // Moving forward with no lateral component
            float slip = GripMath.ComputeSlipRatio(0f, 5.0f);
            Assert.AreEqual(0f, slip, k_Epsilon,
                "Zero lateral velocity should give zero slip ratio");
        }

        [Test]
        public void SlipRatio_PurelySideways_FullSlip()
        {
            // All velocity is lateral — complete slide
            float lateralSpeed = 5.0f;
            float totalSpeed = 5.0f;
            float slip = GripMath.ComputeSlipRatio(lateralSpeed, totalSpeed);
            Assert.AreEqual(1.0f, slip, k_Epsilon,
                "Purely sideways motion should give slip ratio of 1.0");
        }

        [Test]
        public void SlipRatio_CarAtRest_NoSlip()
        {
            float slip = GripMath.ComputeSlipRatio(0f, 0f);
            Assert.AreEqual(0f, slip, k_Epsilon,
                "Stationary car should have zero slip (no division by zero)");
        }

        [Test]
        public void SlipRatio_NegativeLateral_UsesAbsoluteValue()
        {
            float slipPos = GripMath.ComputeSlipRatio(3f, 5f);
            float slipNeg = GripMath.ComputeSlipRatio(-3f, 5f);
            Assert.AreEqual(slipPos, slipNeg, k_Epsilon,
                "Slip ratio should use absolute lateral velocity");
        }

        [Test]
        public void SlipRatio_AlwaysBetweenZeroAndOne()
        {
            // Even with lateral > speed (physically unusual), should clamp
            float slip = GripMath.ComputeSlipRatio(10f, 5f);
            Assert.GreaterOrEqual(slip, 0f);
            Assert.LessOrEqual(slip, 1f,
                "Slip ratio must always be clamped to [0, 1]");
        }

        [Test]
        public void SlipRatio_VerySmallSpeed_ReturnsZero()
        {
            // Near-zero speed should avoid division issues
            float slip = GripMath.ComputeSlipRatio(0.00001f, 0.00001f);
            Assert.AreEqual(0f, slip, k_Epsilon,
                "Near-zero speed should return zero slip to avoid instability");
        }


        // =====================================================================
        // GripMath — ComputeLateralForceMagnitude
        // =====================================================================

        [Test]
        public void LateralForce_OpposesLateralVelocity()
        {
            // Positive lateral velocity should produce negative force (opposing)
            float force = GripMath.ComputeLateralForceMagnitude(2.0f, 0.8f, 1.0f, 10f);
            Assert.Less(force, 0f,
                "Lateral force must oppose positive lateral velocity");

            // Negative lateral velocity should produce positive force
            float forceNeg = GripMath.ComputeLateralForceMagnitude(-2.0f, 0.8f, 1.0f, 10f);
            Assert.Greater(forceNeg, 0f,
                "Lateral force must oppose negative lateral velocity");
        }

        [Test]
        public void LateralForce_DoublingGripLoad_DoublesForce()
        {
            float f1 = GripMath.ComputeLateralForceMagnitude(2.0f, 0.8f, 1.0f, 10f);
            float f2 = GripMath.ComputeLateralForceMagnitude(2.0f, 0.8f, 1.0f, 20f);
            Assert.AreEqual(f2, f1 * 2f, 0.01f,
                "Doubling grip load should double lateral force (linearity)");
        }

        [Test]
        public void LateralForce_ZeroGripLoad_ZeroForce()
        {
            float force = GripMath.ComputeLateralForceMagnitude(2.0f, 0.8f, 1.0f, 0f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero grip load should produce zero lateral force");
        }

        [Test]
        public void LateralForce_ZeroGripFactor_ZeroForce()
        {
            float force = GripMath.ComputeLateralForceMagnitude(2.0f, 0f, 1.0f, 10f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero grip factor should produce zero lateral force");
        }

        [Test]
        public void LateralForce_ZeroLateralVelocity_ZeroForce()
        {
            float force = GripMath.ComputeLateralForceMagnitude(0f, 0.8f, 1.0f, 10f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero lateral velocity should produce zero lateral force");
        }


        // =====================================================================
        // GripMath — ComputeEffectiveTraction
        // =====================================================================

        [Test]
        public void EffectiveTraction_NormalDriving_ReturnsBaseTraction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: false, forwardSpeed: 5f, engineForce: 10f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            Assert.AreEqual(0.5f, traction, k_Epsilon,
                "Normal driving should use base traction");
        }

        [Test]
        public void EffectiveTraction_Braking_ReturnsBrakeTraction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: true, forwardSpeed: 5f, engineForce: 0f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            Assert.AreEqual(0.8f, traction, k_Epsilon,
                "Braking should use brake traction");
        }

        [Test]
        public void EffectiveTraction_StoppedNoEngine_StaticFriction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: false, forwardSpeed: 0.01f, engineForce: 0f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            Assert.AreEqual(1.2f, traction, k_Epsilon,
                "Stopped with no engine should use static friction (highest)");
        }

        [Test]
        public void EffectiveTraction_StoppedWithEngine_BaseTraction()
        {
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: false, forwardSpeed: 0.01f, engineForce: 5f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            Assert.AreEqual(0.5f, traction, k_Epsilon,
                "Stopped but with engine force should use base traction, not static");
        }

        [Test]
        public void EffectiveTraction_BrakingAndStopped_UsesBrakeTraction()
        {
            // Braking flag takes priority, then static friction check
            // Since isBraking=true, effectiveTraction starts as brake, then
            // static friction override only applies when engineForce == 0
            float traction = GripMath.ComputeEffectiveTraction(
                isBraking: true, forwardSpeed: 0.01f, engineForce: 0f,
                zTraction: 0.5f, zBrakeTraction: 0.8f,
                staticFrictionSpeed: 0.1f, staticFrictionTraction: 1.2f);
            // Static friction override applies because speed < threshold and engine == 0
            Assert.AreEqual(1.2f, traction, k_Epsilon,
                "Stopped and braking with no engine should use static friction");
        }


        // =====================================================================
        // GripMath — ComputeLongitudinalForceMagnitude
        // =====================================================================

        [Test]
        public void LongitudinalForce_OpposesForwardMotion()
        {
            // Positive forward speed should produce negative force (opposing)
            float force = GripMath.ComputeLongitudinalForceMagnitude(3f, 0.5f, 1.0f, 10f);
            Assert.Less(force, 0f,
                "Longitudinal friction must oppose forward motion");
        }

        [Test]
        public void LongitudinalForce_ZeroLoad_ZeroForce()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(3f, 0.5f, 1.0f, 0f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero grip load should produce zero longitudinal force");
        }

        [Test]
        public void LongitudinalForce_NegativeSpeed_PositiveForce()
        {
            // Reversing should produce force that opposes reverse direction
            float force = GripMath.ComputeLongitudinalForceMagnitude(-3f, 0.5f, 1.0f, 10f);
            Assert.Greater(force, 0f,
                "Negative speed (reversing) should produce positive force (opposing)");
        }

        [Test]
        public void LongitudinalForce_ZeroTraction_ZeroForce()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(3f, 0f, 1.0f, 10f);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Zero traction should produce zero longitudinal force");
        }


        // =====================================================================
        // GripMath — ComputeWheelRpm
        // =====================================================================

        [Test]
        public void WheelRpm_KnownSpeedAndRadius_CorrectRpm()
        {
            // v = omega * r, RPM = omega * 60 / (2 * pi)
            // RPM = (v / r) * 60 / (2 * pi)
            float speed = 5f;
            float expectedRpm = (speed / k_WheelRadiusRear) * 60f / (2f * Mathf.PI);
            float rpm = GripMath.ComputeWheelRpm(speed, k_WheelRadiusRear);
            Assert.AreEqual(expectedRpm, rpm, 0.01f,
                "RPM should follow omega = v / r converted to rev/min");
        }

        [Test]
        public void WheelRpm_ZeroSpeed_ZeroRpm()
        {
            float rpm = GripMath.ComputeWheelRpm(0f, k_WheelRadiusRear);
            Assert.AreEqual(0f, rpm, k_Epsilon,
                "Stationary wheel should have zero RPM");
        }

        [Test]
        public void WheelRpm_NegativeSpeed_NegativeRpm()
        {
            float rpm = GripMath.ComputeWheelRpm(-3f, k_WheelRadiusRear);
            Assert.Less(rpm, 0f,
                "Reverse speed should produce negative RPM (direction matters)");
        }

        [Test]
        public void WheelRpm_ZeroRadius_ZeroRpm()
        {
            float rpm = GripMath.ComputeWheelRpm(5f, 0f);
            Assert.AreEqual(0f, rpm, k_Epsilon,
                "Zero radius should return zero RPM (avoid division by zero)");
        }

        [Test]
        public void WheelRpm_NegativeRadius_ZeroRpm()
        {
            float rpm = GripMath.ComputeWheelRpm(5f, -0.1f);
            Assert.AreEqual(0f, rpm, k_Epsilon,
                "Negative radius is physically invalid — should return zero RPM");
        }
    }
}

#pragma warning restore CS0618
