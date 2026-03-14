using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for Hooke's law suspension calculations.
    /// Tests spring force, damping, bump stops, and grip load clamping.
    /// </summary>
    public class SuspensionMathTests
    {
        // ---- Constants matching production defaults ----
        const float k_RestDistance = 0.20f;
        const float k_WheelRadius = 0.166f;
        const float k_MinSpringLen = 0.032f;
        const float k_SpringStrength = 75f;
        const float k_SpringDamping = 4.25f;
        const float k_MaxSpringForce = 50f;
        const float k_OverExtend = 0.08f;
        const float k_DefaultDt = 0.008333f; // 120 Hz


        // ---- ComputeSpringLength ----

        [Test]
        public void ComputeSpringLength_NormalContact_ReturnsDistanceMinusRadius()
        {
            float anchorToContact = 0.35f;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadius, k_MinSpringLen);
            Assert.AreEqual(anchorToContact - k_WheelRadius, result, 0.0001f);
        }

        [Test]
        public void ComputeSpringLength_FullCompression_ClampsToMinSpringLen()
        {
            // Contact point very close to anchor — would compress below bump stop
            float anchorToContact = k_WheelRadius + 0.01f;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadius, k_MinSpringLen);
            Assert.AreEqual(k_MinSpringLen, result, 0.0001f);
        }

        [Test]
        public void ComputeSpringLength_AtRestDistance_ReturnsRestDistance()
        {
            float anchorToContact = k_RestDistance + k_WheelRadius;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadius, k_MinSpringLen);
            Assert.AreEqual(k_RestDistance, result, 0.0001f);
        }

        [Test]
        public void ComputeSpringLength_BeyondRest_ReturnsExtendedLength()
        {
            float anchorToContact = k_RestDistance + k_OverExtend + k_WheelRadius;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadius, k_MinSpringLen);
            float expected = k_RestDistance + k_OverExtend;
            Assert.AreEqual(expected, result, 0.0001f);
        }


        // ---- ComputeSuspensionForceWithDamping ----

        [Test]
        public void ComputeSuspensionForceWithDamping_AtRest_ReturnsZero()
        {
            // Spring at rest length, no velocity change
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, k_RestDistance, k_RestDistance, k_DefaultDt);
            Assert.AreEqual(0f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_Compressed_ReturnsPositiveForce()
        {
            float springLen = 0.10f; // 0.10m below rest (0.20m)
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, springLen, k_DefaultDt);
            // F = 75 * (0.20 - 0.10) = 7.5 N (no damping since prev == cur)
            Assert.AreEqual(7.5f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_Extended_ReturnsZero_NoTension()
        {
            // Spring extended beyond rest — would produce negative force
            float springLen = 0.25f; // Beyond rest
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, springLen, k_DefaultDt);
            // F = 75 * (0.20 - 0.25) = -3.75, clamped to 0
            Assert.AreEqual(0f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_CompressingVelocity_AddsDamping()
        {
            float springLen = 0.15f;
            float prevLen = 0.18f; // Was longer, now shorter = compressing
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, prevLen, k_DefaultDt);
            // Spring: 75 * (0.20 - 0.15) = 3.75 N
            // Damping: 4.25 * (0.18 - 0.15) / 0.008333 = 4.25 * 3.6 = 15.3 N
            // Total: 3.75 + 15.3 = 19.05 N
            Assert.Greater(force, 3.75f, "Damping should add to spring force when compressing");
            Assert.AreEqual(19.05f, force, 0.5f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_ExtendingVelocity_ReducesForce()
        {
            float springLen = 0.15f;
            float prevLen = 0.12f; // Was shorter, now longer = extending (rebound)
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, prevLen, k_DefaultDt);
            // Spring: 75 * (0.20 - 0.15) = 3.75 N
            // Damping: 4.25 * (0.12 - 0.15) / 0.008333 = 4.25 * (-3.6) = -15.3 N
            // Total: 3.75 - 15.3 = -11.55, clamped to 0
            Assert.AreEqual(0f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_BumpStop_MaxCompression()
        {
            float springLen = k_MinSpringLen; // 0.032m — maximum compression
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, springLen, k_DefaultDt);
            // F = 75 * (0.20 - 0.032) = 75 * 0.168 = 12.6 N
            Assert.AreEqual(12.6f, force, 0.1f);
        }


        // ---- ComputeGripLoad ----

        [Test]
        public void ComputeGripLoad_NormalCompression_ReturnsSpringForce()
        {
            float springLen = 0.15f;
            float load = SuspensionMath.ComputeGripLoad(k_SpringStrength, k_RestDistance, springLen, k_MaxSpringForce);
            // F = 75 * (0.20 - 0.15) = 3.75 N
            Assert.AreEqual(3.75f, load, 0.01f);
        }

        [Test]
        public void ComputeGripLoad_HighCompression_ClampsToMax()
        {
            float springLen = k_MinSpringLen; // Very compressed
            float load = SuspensionMath.ComputeGripLoad(k_SpringStrength, k_RestDistance, springLen, k_MaxSpringForce);
            // F = 75 * 0.168 = 12.6 N — below 50N max, so not clamped here
            Assert.LessOrEqual(load, k_MaxSpringForce);

            // Use extreme spring to force clamping
            float extremeLoad = SuspensionMath.ComputeGripLoad(
                1000f, k_RestDistance, k_MinSpringLen, k_MaxSpringForce);
            // F = 1000 * 0.168 = 168 N, clamped to 50
            Assert.AreEqual(k_MaxSpringForce, extremeLoad, 0.01f);
        }

        [Test]
        public void ComputeGripLoad_Extended_ReturnsZero()
        {
            float springLen = 0.25f; // Beyond rest — no compression
            float load = SuspensionMath.ComputeGripLoad(k_SpringStrength, k_RestDistance, springLen, k_MaxSpringForce);
            Assert.AreEqual(0f, load, 0.01f);
        }


        // ---- ComputeRayLength ----

        [Test]
        public void ComputeRayLength_DefaultValues_ReturnsSumOfComponents()
        {
            float expected = k_RestDistance + k_OverExtend + k_WheelRadius;
            float result = SuspensionMath.ComputeRayLength(k_RestDistance, k_OverExtend, k_WheelRadius);
            Assert.AreEqual(expected, result, 0.0001f);
        }
    }
}
