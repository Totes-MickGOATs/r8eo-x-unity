#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for SuspensionMath.ComputeGripLoad, ComputeRayLength,
    /// and ComputeSuspensionForce (no-damping overload).
    /// Extracted from BlackBoxSuspensionTests to keep each file under 200 lines.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxSuspensionGripTests
    {
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
