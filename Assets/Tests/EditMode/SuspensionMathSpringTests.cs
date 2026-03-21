using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for SuspensionMath spring length, force with damping, and ray length.
    /// GripLoad tests live in SuspensionMathGripLoadTests.cs.
    /// </summary>
    public class SuspensionMathSpringTests
    {
        const float k_RestDistance = 0.20f;
        const float k_WheelRadius = 0.166f;
        const float k_MinSpringLen = 0.032f;
        const float k_SpringStrength = 75f;
        const float k_SpringDamping = 4.25f;
        const float k_OverExtend = 0.08f;
        const float k_DefaultDt = 0.008333f;

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
            Assert.AreEqual(k_RestDistance + k_OverExtend, result, 0.0001f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_AtRest_ReturnsZero()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, k_RestDistance, k_RestDistance, k_DefaultDt);
            Assert.AreEqual(0f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_Compressed_ReturnsPositiveForce()
        {
            float springLen = 0.10f;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, springLen, k_DefaultDt);
            Assert.AreEqual(7.5f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_Extended_ReturnsZero_NoTension()
        {
            float springLen = 0.25f;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, springLen, k_DefaultDt);
            Assert.AreEqual(0f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_CompressingVelocity_AddsDamping()
        {
            float springLen = 0.15f;
            float prevLen = 0.18f;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, prevLen, k_DefaultDt);
            Assert.Greater(force, 3.75f, "Damping should add to spring force when compressing");
            Assert.AreEqual(19.05f, force, 0.5f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_ExtendingVelocity_ReducesForce()
        {
            float springLen = 0.15f;
            float prevLen = 0.12f;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, prevLen, k_DefaultDt);
            Assert.AreEqual(0f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_BumpStop_MaxCompression()
        {
            float springLen = k_MinSpringLen;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, springLen, k_DefaultDt);
            Assert.AreEqual(12.6f, force, 0.1f);
        }

        [Test]
        public void ComputeRayLength_DefaultValues_ReturnsSumOfComponents()
        {
            float expected = k_RestDistance + k_OverExtend + k_WheelRadius;
            float result = SuspensionMath.ComputeRayLength(k_RestDistance, k_OverExtend, k_WheelRadius);
            Assert.AreEqual(expected, result, 0.0001f);
        }
    }
}
