#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Black-box unit tests for SuspensionMath.ComputeSuspensionForceWithDamping.</summary>
    [Category("Fast")]
    public class BlackBoxSuspensionForceTests
    {
        [Test]
        public void SuspensionForce_SpringAtRestNoVelocity_ZeroForce()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, k_RestDistance, k_RestDistance, k_Dt);
            Assert.AreEqual(0f, force, 0.01f,
                "Spring at rest with zero velocity should produce zero force");
        }

        [Test]
        public void SuspensionForce_CompressedSpring_HookesLawFEqualsKX()
        {
            float springLen = 0.10f;
            float compression = k_RestDistance - springLen;
            float expectedForce = k_SpringK * compression;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, springLen, springLen, k_Dt);
            Assert.AreEqual(expectedForce, force, 0.01f,
                "Compressed spring with no velocity should follow F = k * x");
        }

        [Test]
        public void SuspensionForce_ExtendingWithHighDamping_ClampsToZero()
        {
            float springLen = 0.18f;
            float prevLen = 0.10f;
            float heavyDamping = 50f;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, heavyDamping, k_RestDistance, springLen, prevLen, k_Dt);
            Assert.AreEqual(0f, force, 0.01f,
                "High damping during extension should clamp force to zero (no pull)");
        }

        [Test]
        public void SuspensionForce_NeverReturnsNegative()
        {
            float springLen = 0.30f;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, springLen, springLen, k_Dt);
            Assert.GreaterOrEqual(force, 0f,
                "Suspension force must never be negative (no tension rule)");
        }

        [Test]
        public void SuspensionForce_CompressionVelocity_AddsToSpringForce()
        {
            float springLen = 0.15f;
            float prevLen = 0.18f;
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
            float springLen = 0.15f;
            float prevLen = 0.14f;
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
            float len1 = 0.15f;
            float len2 = 0.10f;
            float f1 = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, len1, len1, k_Dt);
            float f2 = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringK, k_Damping, k_RestDistance, len2, len2, k_Dt);
            Assert.AreEqual(f2, f1 * 2f, 0.01f,
                "Double the compression should produce double the force (Hooke's law linearity)");
        }
    }
}

#pragma warning restore CS0618
