using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for SuspensionMath damping force and grip load.</summary>
    public class SuspensionForceTests
    {
        const float k_RestDistance = 0.20f;
        const float k_MinSpringLen = 0.032f;
        const float k_SpringStrength = 75f;
        const float k_SpringDamping = 4.25f;
        const float k_MaxSpringForce = 50f;
        const float k_DefaultDt = 0.008333f;

        [Test]
        public void ComputeSuspensionForceWithDamping_AtRest_ReturnsZero()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping, k_RestDistance, k_RestDistance, k_RestDistance, k_DefaultDt);
            Assert.AreEqual(0f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_Compressed_ReturnsPositiveForce()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping, k_RestDistance, 0.10f, 0.10f, k_DefaultDt);
            Assert.AreEqual(7.5f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_Extended_ReturnsZero_NoTension()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping, k_RestDistance, 0.25f, 0.25f, k_DefaultDt);
            Assert.AreEqual(0f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_CompressingVelocity_AddsDamping()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping, k_RestDistance, 0.15f, 0.18f, k_DefaultDt);
            Assert.Greater(force, 3.75f, "Damping should add to spring force when compressing");
            Assert.AreEqual(19.05f, force, 0.5f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_ExtendingVelocity_ReducesForce()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping, k_RestDistance, 0.15f, 0.12f, k_DefaultDt);
            Assert.AreEqual(0f, force, 0.01f);
        }

        [Test]
        public void ComputeSuspensionForceWithDamping_BumpStop_MaxCompression()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping, k_RestDistance, k_MinSpringLen, k_MinSpringLen, k_DefaultDt);
            Assert.AreEqual(12.6f, force, 0.1f);
        }

        [Test]
        public void ComputeGripLoad_NormalCompression_ReturnsSpringForce()
        {
            float load = SuspensionMath.ComputeGripLoad(k_SpringStrength, k_RestDistance, 0.15f, k_MaxSpringForce);
            Assert.AreEqual(3.75f, load, 0.01f);
        }

        [Test]
        public void ComputeGripLoad_HighCompression_ClampsToMax()
        {
            float extremeLoad = SuspensionMath.ComputeGripLoad(1000f, k_RestDistance, k_MinSpringLen, k_MaxSpringForce);
            Assert.AreEqual(k_MaxSpringForce, extremeLoad, 0.01f);
        }

        [Test]
        public void ComputeGripLoad_Extended_ReturnsZero()
        {
            float load = SuspensionMath.ComputeGripLoad(k_SpringStrength, k_RestDistance, 0.25f, k_MaxSpringForce);
            Assert.AreEqual(0f, load, 0.01f);
        }
    }
}
