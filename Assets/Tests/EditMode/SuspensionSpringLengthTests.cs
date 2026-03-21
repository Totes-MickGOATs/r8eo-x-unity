using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for SuspensionMath.ComputeSpringLength and ComputeRayLength.</summary>
    public class SuspensionSpringLengthTests
    {
        const float k_RestDistance = 0.20f;
        const float k_WheelRadius = 0.166f;
        const float k_MinSpringLen = 0.032f;
        const float k_OverExtend = 0.08f;

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
        public void ComputeRayLength_DefaultValues_ReturnsSumOfComponents()
        {
            float expected = k_RestDistance + k_OverExtend + k_WheelRadius;
            float result = SuspensionMath.ComputeRayLength(k_RestDistance, k_OverExtend, k_WheelRadius);
            Assert.AreEqual(expected, result, 0.0001f);
        }
    }
}
