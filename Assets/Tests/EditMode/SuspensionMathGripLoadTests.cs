using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for SuspensionMath.ComputeGripLoad and ComputeGripLoadFromSuspensionForce.
    /// Spring and damping tests live in SuspensionMathSpringTests.cs.
    /// </summary>
    public class SuspensionMathGripLoadTests
    {
        const float k_RestDistance = 0.20f;
        const float k_MinSpringLen = 0.032f;
        const float k_SpringStrength = 75f;
        const float k_MaxSpringForce = 50f;

        [Test]
        public void ComputeGripLoad_NormalCompression_ReturnsSpringForce()
        {
            float springLen = 0.15f;
            float load = SuspensionMath.ComputeGripLoad(k_SpringStrength, k_RestDistance, springLen, k_MaxSpringForce);
            Assert.AreEqual(3.75f, load, 0.01f);
        }

        [Test]
        public void ComputeGripLoad_HighCompression_ClampsToMax()
        {
            float springLen = k_MinSpringLen;
            float load = SuspensionMath.ComputeGripLoad(k_SpringStrength, k_RestDistance, springLen, k_MaxSpringForce);
            Assert.LessOrEqual(load, k_MaxSpringForce);

            float extremeLoad = SuspensionMath.ComputeGripLoad(
                1000f, k_RestDistance, k_MinSpringLen, k_MaxSpringForce);
            Assert.AreEqual(k_MaxSpringForce, extremeLoad, 0.01f);
        }

        [Test]
        public void ComputeGripLoad_Extended_ReturnsZero()
        {
            float springLen = 0.25f;
            float load = SuspensionMath.ComputeGripLoad(k_SpringStrength, k_RestDistance, springLen, k_MaxSpringForce);
            Assert.AreEqual(0f, load, 0.01f);
        }
    }
}
