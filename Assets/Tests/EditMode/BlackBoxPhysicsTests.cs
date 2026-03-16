#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using R8EOX.Input;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for all public physics functions.
    /// Tests verify physically correct behavior from inputs/outputs only —
    /// no knowledge of internal implementation.
    /// Uses realistic 1/10th scale RC car values throughout.
    /// </summary>
    public class BlackBoxPhysicsTests
    {
        // ---- Realistic RC car constants (1/10th scale) ----
        const float k_Mass = 1.5f;           // kg
        const float k_WheelRadiusRearFront = 0.0425f;  // m  (Proline Electron front, 1:10 scale)
        const float k_WheelRadiusRearRear  = 0.0420f;  // m  (Proline Electron rear, 1:10 scale)
        const float k_RestDistance = 0.20f;   // m
        const float k_MinSpringLen = 0.032f;  // m (bump stop)
        const float k_SpringK = 75f;         // N/m
        const float k_Damping = 4.25f;       // damping coefficient
        const float k_MaxSpringForce = 50f;   // N
        const float k_OverExtend = 0.08f;    // m
        const float k_Dt = 0.008333f;        // 120 Hz physics step
        const float k_Epsilon = 0.0001f;     // float comparison tolerance


        // =====================================================================
        // SuspensionMath — ComputeSpringLength
        // =====================================================================

        [Test]
        public void ComputeSpringLength_WheelInsideGround_ClampsToMinimum()
        {
            // Scenario: contact point is above the anchor (wheel pushed through ground)
            // Raw = anchorToContact - wheelRadius would be negative
            float anchorToContact = k_WheelRadiusRear * 0.5f; // less than radius
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadiusRear, k_MinSpringLen);
            Assert.AreEqual(k_MinSpringLen, result, k_Epsilon,
                "Spring length must clamp to bump stop when wheel is inside ground");
        }

        [Test]
        public void ComputeSpringLength_WheelBarelyTouching_ReturnsZeroOrMin()
        {
            // Contact exactly at wheel radius distance — raw spring length = 0
            float anchorToContact = k_WheelRadiusRear;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadiusRear, k_MinSpringLen);
            // raw = 0, but minSpringLen = 0.032, so clamps up
            Assert.AreEqual(k_MinSpringLen, result, k_Epsilon,
                "Zero raw spring length should clamp to min spring length");
        }

        [Test]
        public void ComputeSpringLength_NormalDriving_ReturnsRawDistance()
        {
            // Wheel at a normal driving distance from anchor
            float anchorToContact = 0.40f;
            float expected = anchorToContact - k_WheelRadiusRear;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadiusRear, k_MinSpringLen);
            Assert.AreEqual(expected, result, k_Epsilon,
                "Normal distance should return raw distance minus radius");
        }

        [Test]
        public void ComputeSpringLength_ZeroMinSpringLen_AllowsZeroLength()
        {
            float anchorToContact = k_WheelRadiusRear; // raw = 0
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadiusRear, 0f);
            Assert.AreEqual(0f, result, k_Epsilon,
                "With min=0, raw=0 should return exactly 0");
        }


        // =====================================================================
        // SuspensionMath — ComputeSuspensionForceWithDamping
        // =====================================================================

        [Test]
        public void SuspensionForce_SpringAtRestNoVelocity_ZeroForce()
        {
            // Physically: spring at rest with no motion produces no force
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, k_RestDistance, k_RestDistance, k_Dt);
            Assert.AreEqual(0f, force, 0.01f,
                "Spring at rest with zero velocity should produce zero force");
        }

        [Test]
        public void SuspensionForce_CompressedSpring_HookesLawFEqualsKX()
        {
            // Hooke's law: F = k * x where x = rest - current
            float springLen = 0.10f;
            float compression = k_RestDistance - springLen; // 0.10m
            float expectedForce = k_SpringK * compression;  // 75 * 0.10 = 7.5 N
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, springLen, springLen, k_Dt);
            Assert.AreEqual(expectedForce, force, 0.01f,
                "Compressed spring with no velocity should follow F = k * x");
        }

        [Test]
        public void SuspensionForce_ExtendingWithHighDamping_ClampsToZero()
        {
            // Scenario: spring slightly compressed but extending fast with heavy damping
            // Damping subtraction should drive total negative, which clamps to 0
            float springLen = 0.18f; // slight compression (0.02m)
            float prevLen = 0.10f;   // extending fast (prev was much shorter)
            float heavyDamping = 50f;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, heavyDamping, k_RestDistance, springLen, prevLen, k_Dt);
            Assert.AreEqual(0f, force, 0.01f,
                "High damping during extension should clamp force to zero (no pull)");
        }

        [Test]
        public void SuspensionForce_NeverReturnsNegative()
        {
            // Spring fully extended beyond rest — physical spring cannot pull
            float springLen = 0.30f; // far beyond 0.20m rest
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, springLen, springLen, k_Dt);
            Assert.GreaterOrEqual(force, 0f,
                "Suspension force must never be negative (no tension rule)");
        }

        [Test]
        public void SuspensionForce_CompressionVelocity_AddsToSpringForce()
        {
            // Wheel compressing: prev > current means spring is getting shorter
            float springLen = 0.15f;
            float prevLen = 0.18f; // was longer, now shorter = compressing
            float forceWithDamping = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, springLen, prevLen, k_Dt);
            float forceWithout = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, springLen, springLen, k_Dt);
            Assert.Greater(forceWithDamping, forceWithout,
                "Compression velocity should ADD to spring force (resist compression)");
        }

        [Test]
        public void SuspensionForce_ExtensionVelocity_SubtractsFromSpringForce()
        {
            // Wheel extending: prev < current means spring is getting longer
            float springLen = 0.15f;
            float prevLen = 0.14f; // was shorter, now longer = extending
            float forceWithRebound = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, springLen, prevLen, k_Dt);
            float forceStatic = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, springLen, springLen, k_Dt);
            Assert.Less(forceWithRebound, forceStatic,
                "Extension velocity should SUBTRACT from spring force (resist extension)");
        }

        [Test]
        public void SuspensionForce_ZeroDeltaTime_NoCrashNoInfinity()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, 0.15f, 0.18f, 0f);
            Assert.IsFalse(float.IsNaN(force), "Force must not be NaN with dt=0");
            Assert.IsFalse(float.IsInfinity(force), "Force must not be Infinity with dt=0");
        }

        [Test]
        public void SuspensionForce_DoubleCompression_DoubleForce()
        {
            // Hooke's law is linear: double the compression => double the force
            float len1 = 0.15f; // 0.05m compression
            float len2 = 0.10f; // 0.10m compression
            float f1 = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, len1, len1, k_Dt);
            float f2 = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, len2, len2, k_Dt);
            Assert.AreEqual(f2, f1 * 2f, 0.01f,
                "Double the compression should produce double the force (Hooke's law linearity)");
        }


        // =====================================================================
        // SuspensionMath — ComputeGripLoad
        // =====================================================================

        [Test]
        public void GripLoad_ZeroCompression_ZeroGripLoad()
        {
            // At rest distance, compression = 0, so load = 0
            float load = SuspensionMath.ComputeGripLoad(k_SpringK, k_RestDistance, k_RestDistance, k_MaxSpringForce);
            Assert.AreEqual(0f, load, k_Epsilon,
                "Zero compression should produce zero grip load");
        }

        [Test]
        public void GripLoad_HighCompression_ClampsToMax()
        {
            // Very stiff spring with high compression should clamp
            float load = SuspensionMath.ComputeGripLoad(
                5000f, k_RestDistance, k_MinSpringLen, k_MaxSpringForce);
            Assert.AreEqual(k_MaxSpringForce, load, k_Epsilon,
                "Grip load must clamp to max spring force");
        }

        [Test]
        public void GripLoad_NegativeCompression_ZeroGripLoad()
        {
            // Extended beyond rest — negative compression produces negative force, clamped to 0
            float load = SuspensionMath.ComputeGripLoad(k_SpringK, k_RestDistance, 0.30f, k_MaxSpringForce);
            Assert.AreEqual(0f, load, k_Epsilon,
                "Negative compression (extended spring) should produce zero grip load");
        }

        [Test]
        public void GripLoad_ProportionalToCompression()
        {
            float len1 = 0.15f;
            float len2 = 0.10f;
            float load1 = SuspensionMath.ComputeGripLoad(k_SpringK, k_RestDistance, len1, k_MaxSpringForce);
            float load2 = SuspensionMath.ComputeGripLoad(k_SpringK, k_RestDistance, len2, k_MaxSpringForce);
            Assert.AreEqual(load2, load1 * 2f, 0.01f,
                "Double compression should give double grip load (linear spring)");
        }


        // =====================================================================
        // SuspensionMath — ComputeRayLength
        // =====================================================================

        [Test]
        public void RayLength_SumOfComponents()
        {
            float result = SuspensionMath.ComputeRayLength(k_RestDistance, k_OverExtend, k_WheelRadiusRear);
            float expected = k_RestDistance + k_OverExtend + k_WheelRadiusRear;
            Assert.AreEqual(expected, result, k_Epsilon,
                "Ray length should equal rest + overextend + wheelRadius");
        }

        [Test]
        public void RayLength_ZeroOverExtend_StillWorks()
        {
            float result = SuspensionMath.ComputeRayLength(k_RestDistance, 0f, k_WheelRadiusRear);
            Assert.AreEqual(k_RestDistance + k_WheelRadiusRear, result, k_Epsilon);
        }

        [Test]
        public void RayLength_AlwaysPositiveWithPositiveInputs()
        {
            float result = SuspensionMath.ComputeRayLength(0.1f, 0.05f, 0.05f);
            Assert.Greater(result, 0f);
        }


        // =====================================================================
        // SuspensionMath — ComputeSuspensionForce (without damping param)
        // =====================================================================

        [Test]
        public void SuspensionForceNoDamping_Compressed_ReturnsPositive()
        {
            float force = SuspensionMath.ComputeSuspensionForce(
                k_SpringK, k_RestDistance, 0.10f, 0.10f, k_Dt);
            Assert.Greater(force, 0f,
                "Compressed spring should produce positive force");
        }

        [Test]
        public void SuspensionForceNoDamping_Extended_ClampsToZero()
        {
            float force = SuspensionMath.ComputeSuspensionForce(
                k_SpringK, k_RestDistance, 0.30f, 0.30f, k_Dt);
            Assert.AreEqual(0f, force, k_Epsilon,
                "Extended spring should produce zero force (no tension)");
        }

        [Test]
        public void SuspensionForceNoDamping_AtRest_Zero()
        {
            float force = SuspensionMath.ComputeSuspensionForce(
                k_SpringK, k_RestDistance, k_RestDistance, k_RestDistance, k_Dt);
            Assert.AreEqual(0f, force, k_Epsilon);
        }


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


        // =====================================================================
        // DrivetrainMath — ComputeAxleSplit
        // =====================================================================

        [Test]
        public void AxleSplit_OpenDiff_Always5050()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 500f, 200f, 0, 10f); // Open diff = 0
            Assert.AreEqual(force * 0.5f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(force * 0.5f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_LeftWheelOff_AllForceToRight()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, false, true, 0f, 500f, 0, 10f);
            Assert.AreEqual(0f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(force, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_RightWheelOff_AllForceToLeft()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, false, 500f, 0f, 0, 10f);
            Assert.AreEqual(force, split.LeftShare, k_Epsilon);
            Assert.AreEqual(0f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_BothOff_5050()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, false, false, 0f, 0f, 0, 10f);
            Assert.AreEqual(force * 0.5f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(force * 0.5f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_BallDiff_EqualSpeed_5050()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 300f, 300f, 1, 20f); // BallDiff = 1
            Assert.AreEqual(force * 0.5f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(force * 0.5f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_BallDiff_LeftSpinningFaster_LessForceToLeft()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 600f, 300f, 1, 50f); // BallDiff, left faster
            Assert.Less(split.LeftShare, force * 0.5f,
                "Ball diff should send less force to the faster-spinning left wheel");
            Assert.Greater(split.RightShare, force * 0.5f,
                "Ball diff should send more force to the slower right wheel");
        }

        [Test]
        public void AxleSplit_BallDiff_CouplingClampedToPreload()
        {
            // With very large speed difference but small preload,
            // coupling should be limited to preload
            float force = 100f;
            float preload = 5f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 10000f, 0f, 1, preload);
            // Max coupling = preload = 5, so left = 50 - 5 = 45, right = 50 + 5 = 55
            Assert.AreEqual(force * 0.5f - preload, split.LeftShare, 0.01f);
            Assert.AreEqual(force * 0.5f + preload, split.RightShare, 0.01f);
        }

        [Test]
        public void AxleSplit_Spool_StrongerCouplingThanBallDiff()
        {
            float force = 100f;
            float preload = 5f;
            var ballSplit = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 600f, 300f, 1, preload); // BallDiff
            var spoolSplit = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 600f, 300f, 2, preload); // Spool

            float ballDelta = Mathf.Abs(ballSplit.LeftShare - ballSplit.RightShare);
            float spoolDelta = Mathf.Abs(spoolSplit.LeftShare - spoolSplit.RightShare);
            Assert.GreaterOrEqual(spoolDelta, ballDelta,
                "Spool should have stronger coupling than ball diff with small preload");
        }

        [Test]
        public void AxleSplit_ForceConserved_AllDiffTypes()
        {
            float force = 123.456f;
            int[] diffTypes = { 0, 1, 2 }; // Open, BallDiff, Spool
            foreach (int dt in diffTypes)
            {
                var split = DrivetrainMath.ComputeAxleSplit(
                    force, true, true, 500f, 300f, dt, 15f);
                Assert.AreEqual(force, split.LeftShare + split.RightShare, 0.01f,
                    $"Total force must be conserved for diff type {dt}");
            }
        }

        [Test]
        public void AxleSplit_NegativeForce_StillConserved()
        {
            float force = -80f; // reverse
            var split = DrivetrainMath.ComputeAxleSplit(
                force, true, true, 300f, 300f, 1, 10f);
            Assert.AreEqual(force, split.LeftShare + split.RightShare, 0.01f,
                "Negative force (reverse) must be conserved");
        }

        [Test]
        public void AxleSplit_ZeroForce_ZeroShares()
        {
            var split = DrivetrainMath.ComputeAxleSplit(
                0f, true, true, 500f, 300f, 1, 10f);
            Assert.AreEqual(0f, split.LeftShare, k_Epsilon);
            Assert.AreEqual(0f, split.RightShare, k_Epsilon);
        }

        [Test]
        public void AxleSplit_OneWheelOff_ForceConserved()
        {
            float force = 100f;
            var split = DrivetrainMath.ComputeAxleSplit(
                force, false, true, 0f, 500f, 2, 10f);
            Assert.AreEqual(force, split.LeftShare + split.RightShare, k_Epsilon,
                "Force must be conserved even with one wheel off ground");
        }


        // =====================================================================
        // DrivetrainMath — ComputeCenterDiffSplit
        // =====================================================================

        [Test]
        public void CenterDiff_Open_UsesBiasDirectly()
        {
            float engine = 100f;
            float bias = 0.35f;
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(
                engine, bias, 300f, 300f, 0, 10f); // Open
            Assert.AreEqual(engine * bias, front, k_Epsilon);
            Assert.AreEqual(engine * (1f - bias), rear, k_Epsilon);
        }

        [Test]
        public void CenterDiff_ForceConserved()
        {
            float engine = 200f;
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(
                engine, 0.4f, 500f, 300f, 1, 15f);
            Assert.AreEqual(engine, front + rear, 0.01f,
                "Center diff must conserve total force (front + rear = input)");
        }

        [Test]
        public void CenterDiff_FrontSpinningFaster_MoreForceToRear()
        {
            float engine = 100f;
            float bias = 0.5f; // 50/50 base
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(
                engine, bias, 600f, 300f, 1, 50f); // BallDiff, front faster
            Assert.Less(front, engine * bias,
                "Front spinning faster should transfer force away from front");
            Assert.Greater(rear, engine * (1f - bias),
                "Front spinning faster should transfer force toward rear");
        }

        [Test]
        public void CenterDiff_EqualRpm_NoCouplingEffect()
        {
            float engine = 100f;
            float bias = 0.4f;
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(
                engine, bias, 300f, 300f, 1, 20f);
            Assert.AreEqual(engine * bias, front, k_Epsilon,
                "Equal RPM should produce no coupling effect");
            Assert.AreEqual(engine * (1f - bias), rear, k_Epsilon);
        }

        [Test]
        public void CenterDiff_ZeroEngine_ZeroOutput()
        {
            var (front, rear) = DrivetrainMath.ComputeCenterDiffSplit(
                0f, 0.35f, 300f, 300f, 1, 10f);
            Assert.AreEqual(0f, front, k_Epsilon);
            Assert.AreEqual(0f, rear, k_Epsilon);
        }


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


        // =====================================================================
        // TumbleMath — ComputeTumbleFactor
        // =====================================================================

        [Test]
        public void TumbleFactor_Upright_Zero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 0f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Upright car (0 degrees) should have zero tumble factor");
        }

        [Test]
        public void TumbleFactor_BelowEngageAngle_Zero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 30f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Below engage angle should return zero");
        }

        [Test]
        public void TumbleFactor_AtFullAngle_One()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 90f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(1f, factor, k_Epsilon,
                "At full angle should return 1.0");
        }

        [Test]
        public void TumbleFactor_BeyondFullAngle_ClampedToOne()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 120f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(1f, factor, k_Epsilon,
                "Beyond full angle should be clamped to 1.0");
        }

        [Test]
        public void TumbleFactor_Airborne_AlwaysZero()
        {
            // Even at extreme tilt, airborne should return 0
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 90f, isAirborne: true, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Airborne should ALWAYS return zero regardless of tilt");
        }

        [Test]
        public void TumbleFactor_Airborne_WasTumbling_StillZero()
        {
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 90f, isAirborne: true, wasTumbling: true,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factor, k_Epsilon,
                "Airborne should override tumbling state and return zero");
        }

        [Test]
        public void TumbleFactor_HysteresisLowersThresholdWhenTumbling()
        {
            // Without hysteresis (not tumbling): engage=45, so 43 degrees = 0
            float factorNotTumbling = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 43f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factorNotTumbling, k_Epsilon,
                "Not tumbling: 43 deg below engage 45 should be zero");

            // With hysteresis (was tumbling): effective engage = 45-5 = 40, so 43 > 40
            float factorWasTumbling = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 43f, isAirborne: false, wasTumbling: true,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.Greater(factorWasTumbling, 0f,
                "Was tumbling: 43 deg above effective engage 40 should be nonzero (hysteresis)");
        }

        [Test]
        public void TumbleFactor_HysteresisPreventsOscillation()
        {
            // At exactly the engage angle, toggling wasTumbling should produce different results
            float factorOff = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 44f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            float factorOn = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 44f, isAirborne: false, wasTumbling: true,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 5f);
            Assert.AreEqual(0f, factorOff, k_Epsilon,
                "Not tumbling at 44 deg (below 45 engage) should be zero");
            Assert.Greater(factorOn, 0f,
                "Was tumbling at 44 deg (above 40 effective engage) should be nonzero");
        }

        [Test]
        public void TumbleFactor_MidpointUsesSmoothing()
        {
            // Midpoint between engage (45) and full (90) = 67.5 degrees
            // t = (67.5 - 45) / (90 - 45) = 0.5
            // smoothstep(0.5) = 0.5
            float factor = TumbleMath.ComputeTumbleFactor(
                tiltAngle: 67.5f, isAirborne: false, wasTumbling: false,
                engageDeg: 45f, fullDeg: 90f, hysteresisDeg: 0f);
            Assert.AreEqual(0.5f, factor, 0.01f,
                "Midpoint should give smoothstep(0.5) = 0.5");
        }


        // =====================================================================
        // TumbleMath — Smoothstep
        // =====================================================================

        [Test]
        public void Smoothstep_ZeroReturnsZero()
        {
            Assert.AreEqual(0f, TumbleMath.Smoothstep(0f), k_Epsilon);
        }

        [Test]
        public void Smoothstep_HalfReturnsHalf()
        {
            // 3*(0.5)^2 - 2*(0.5)^3 = 0.75 - 0.25 = 0.5
            Assert.AreEqual(0.5f, TumbleMath.Smoothstep(0.5f), k_Epsilon);
        }

        [Test]
        public void Smoothstep_OneReturnsOne()
        {
            Assert.AreEqual(1f, TumbleMath.Smoothstep(1f), k_Epsilon);
        }

        [Test]
        public void Smoothstep_BelowZero_ClampsToZero()
        {
            Assert.AreEqual(0f, TumbleMath.Smoothstep(-0.5f), k_Epsilon,
                "Negative input should clamp to 0");
        }

        [Test]
        public void Smoothstep_AboveOne_ClampsToOne()
        {
            Assert.AreEqual(1f, TumbleMath.Smoothstep(1.5f), k_Epsilon,
                "Input above 1 should clamp to 1");
        }

        [Test]
        public void Smoothstep_MonotonicallyIncreasing()
        {
            float v25 = TumbleMath.Smoothstep(0.25f);
            float v50 = TumbleMath.Smoothstep(0.50f);
            float v75 = TumbleMath.Smoothstep(0.75f);
            Assert.Less(v25, v50, "0.25 < 0.50");
            Assert.Less(v50, v75, "0.50 < 0.75");
        }

        [Test]
        public void Smoothstep_SymmetricAroundHalf()
        {
            // smoothstep(t) + smoothstep(1-t) = 1 for Hermite smoothstep
            float a = TumbleMath.Smoothstep(0.25f);
            float b = TumbleMath.Smoothstep(0.75f);
            Assert.AreEqual(1f, a + b, k_Epsilon,
                "Hermite smoothstep should be symmetric: f(t) + f(1-t) = 1");
        }


        // =====================================================================
        // TumbleMath — ComputeTiltAngle
        // =====================================================================

        [Test]
        public void TiltAngle_VectorUp_ZeroDegrees()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.up);
            Assert.AreEqual(0f, angle, 0.01f,
                "Upright car (up vector = world up) should be 0 degrees");
        }

        [Test]
        public void TiltAngle_VectorDown_180Degrees()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.down);
            Assert.AreEqual(180f, angle, 0.01f,
                "Inverted car (up vector = world down) should be 180 degrees");
        }

        [Test]
        public void TiltAngle_VectorRight_90Degrees()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.right);
            Assert.AreEqual(90f, angle, 0.01f,
                "Car on its side (up = world right) should be 90 degrees");
        }

        [Test]
        public void TiltAngle_45DegreeTilt()
        {
            // A vector tilted 45 degrees from up toward forward
            Vector3 tilted = new Vector3(0f, 1f, 1f).normalized;
            float angle = TumbleMath.ComputeTiltAngle(tilted);
            Assert.AreEqual(45f, angle, 0.1f,
                "45-degree tilt should report approximately 45 degrees");
        }

        [Test]
        public void TiltAngle_VectorForward_90Degrees()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.forward);
            Assert.AreEqual(90f, angle, 0.01f,
                "Car nose-up (up = forward) should be 90 degrees");
        }


        // =====================================================================
        // InputMath — ApplyDeadzone
        // =====================================================================

        [Test]
        public void Deadzone_ZeroInput_Zero()
        {
            float result = InputMath.ApplyDeadzone(0f, 0.1f);
            Assert.AreEqual(0f, result, k_Epsilon,
                "Zero input should always be zero");
        }

        [Test]
        public void Deadzone_BelowThreshold_Zero()
        {
            float result = InputMath.ApplyDeadzone(0.05f, 0.1f);
            Assert.AreEqual(0f, result, k_Epsilon,
                "Input below deadzone should return zero");
        }

        [Test]
        public void Deadzone_AtThreshold_Zero()
        {
            // At exactly the deadzone edge, should be zero (inclusive)
            float result = InputMath.ApplyDeadzone(0.1f, 0.1f);
            Assert.AreEqual(0f, result, k_Epsilon,
                "Input exactly at deadzone should return zero");
        }

        [Test]
        public void Deadzone_AtMax_One()
        {
            float result = InputMath.ApplyDeadzone(1.0f, 0.1f);
            Assert.AreEqual(1.0f, result, k_Epsilon,
                "Full deflection should return 1.0");
        }

        [Test]
        public void Deadzone_SmoothRemap_NoJump()
        {
            // Just above deadzone should be very close to 0 (smooth transition)
            float result = InputMath.ApplyDeadzone(0.11f, 0.1f);
            Assert.Greater(result, 0f, "Just above deadzone should be nonzero");
            Assert.Less(result, 0.05f,
                "Just above deadzone should be very small (smooth remap, no jump)");
        }

        [Test]
        public void Deadzone_NegativeBelowThreshold_Zero()
        {
            float result = InputMath.ApplyDeadzone(-0.05f, 0.1f);
            Assert.AreEqual(0f, result, k_Epsilon,
                "Negative input below threshold should return zero");
        }

        [Test]
        public void Deadzone_NegativeAboveThreshold_ReturnsPositive()
        {
            // ApplyDeadzone returns Clamp01(sign * remapped), so negative input
            // with sign * remapped < 0 gets clamped to 0
            float result = InputMath.ApplyDeadzone(-0.5f, 0.1f);
            // sign = -1, abs = 0.5, remapped = (0.5-0.1)/0.9 = 0.444
            // sign * remapped = -0.444, Clamp01 => 0
            Assert.AreEqual(0f, result, k_Epsilon,
                "Negative input produces negative remapped which Clamp01 zeroes");
        }

        [Test]
        public void Deadzone_ZeroDeadzone_PassthroughPositive()
        {
            float result = InputMath.ApplyDeadzone(0.5f, 0f);
            Assert.AreEqual(0.5f, result, k_Epsilon,
                "Zero deadzone should pass through positive input unchanged");
        }


        // =====================================================================
        // InputMath — ApplySteeringCurve
        // =====================================================================

        [Test]
        public void SteeringCurve_Exponent1_Linear()
        {
            float result = InputMath.ApplySteeringCurve(0.5f, 1.0f);
            Assert.AreEqual(0.5f, result, k_Epsilon,
                "Exponent 1.0 should give linear response (output = input)");
        }

        [Test]
        public void SteeringCurve_ExponentGreaterThan1_ReducesSmallInputs()
        {
            float linear = InputMath.ApplySteeringCurve(0.5f, 1.0f);
            float curved = InputMath.ApplySteeringCurve(0.5f, 2.0f);
            Assert.Less(curved, linear,
                "Exponent > 1 should reduce magnitude of small inputs");
        }

        [Test]
        public void SteeringCurve_FullDeflection_AlwaysOne()
        {
            Assert.AreEqual(1f, InputMath.ApplySteeringCurve(1.0f, 1.0f), k_Epsilon);
            Assert.AreEqual(1f, InputMath.ApplySteeringCurve(1.0f, 2.0f), k_Epsilon);
            Assert.AreEqual(1f, InputMath.ApplySteeringCurve(1.0f, 3.5f), k_Epsilon);
        }

        [Test]
        public void SteeringCurve_PreservesSignForNegativeInput()
        {
            float result = InputMath.ApplySteeringCurve(-0.5f, 2.0f);
            Assert.Less(result, 0f,
                "Negative input should produce negative output");
            float posResult = InputMath.ApplySteeringCurve(0.5f, 2.0f);
            Assert.AreEqual(-posResult, result, k_Epsilon,
                "Magnitude should be same for positive and negative, just sign-flipped");
        }

        [Test]
        public void SteeringCurve_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, InputMath.ApplySteeringCurve(0f, 1.5f), k_Epsilon);
            Assert.AreEqual(0f, InputMath.ApplySteeringCurve(0f, 3.0f), k_Epsilon);
        }

        [Test]
        public void SteeringCurve_NegativeFullDeflection_NegativeOne()
        {
            float result = InputMath.ApplySteeringCurve(-1.0f, 2.0f);
            Assert.AreEqual(-1f, result, k_Epsilon,
                "Full negative deflection should always be -1.0");
        }


        // =====================================================================
        // InputMath — MergeInputs
        // =====================================================================

        [Test]
        public void MergeInputs_TakesLargerAbsoluteValue()
        {
            float result = InputMath.MergeInputs(0.3f, 0.7f);
            Assert.AreEqual(0.7f, result, k_Epsilon,
                "Should take the value with the larger absolute magnitude");
        }

        [Test]
        public void MergeInputs_NegativeCanWinOverSmallerPositive()
        {
            float result = InputMath.MergeInputs(0.3f, -0.8f);
            Assert.AreEqual(-0.8f, result, k_Epsilon,
                "Negative with larger magnitude should win");
        }

        [Test]
        public void MergeInputs_EqualValues_ReturnsSecond()
        {
            // When abs(a) == abs(b), the condition Abs(a) > Abs(b) is false, so returns b
            float result = InputMath.MergeInputs(0.5f, 0.5f);
            Assert.AreEqual(0.5f, result, k_Epsilon,
                "Equal absolute values should return second input");
        }

        [Test]
        public void MergeInputs_BothZero_Zero()
        {
            float result = InputMath.MergeInputs(0f, 0f);
            Assert.AreEqual(0f, result, k_Epsilon);
        }

        [Test]
        public void MergeInputs_EqualOppositeSign_ReturnsSecond()
        {
            // abs(0.5) == abs(-0.5), so condition is false, returns b
            float result = InputMath.MergeInputs(0.5f, -0.5f);
            Assert.AreEqual(-0.5f, result, k_Epsilon,
                "Equal magnitude opposite sign: should return second");
        }

        [Test]
        public void MergeInputs_FirstLarger_ReturnsFirst()
        {
            float result = InputMath.MergeInputs(-0.9f, 0.2f);
            Assert.AreEqual(-0.9f, result, k_Epsilon,
                "First input with larger magnitude should win");
        }
    }
}

#pragma warning restore CS0618
