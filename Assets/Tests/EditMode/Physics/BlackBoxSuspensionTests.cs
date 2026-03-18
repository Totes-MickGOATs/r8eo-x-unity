#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for SuspensionMath public functions.
    /// Tests verify physically correct behavior from inputs/outputs only.
    /// Uses realistic 1/10th scale RC car values throughout.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxSuspensionTests
    {
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
    }
}

#pragma warning restore CS0618
