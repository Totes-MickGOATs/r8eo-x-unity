#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Black-box unit tests for SuspensionMath.ComputeSpringLength.</summary>
    [Category("Fast")]
    public class BlackBoxSpringLengthTests
    {
        [Test]
        public void ComputeSpringLength_WheelInsideGround_ClampsToMinimum()
        {
            float anchorToContact = k_WheelRadiusRear * 0.5f;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadiusRear, k_MinSpringLen);
            Assert.AreEqual(k_MinSpringLen, result, k_Epsilon,
                "Spring length must clamp to bump stop when wheel is inside ground");
        }

        [Test]
        public void ComputeSpringLength_WheelBarelyTouching_ReturnsZeroOrMin()
        {
            float anchorToContact = k_WheelRadiusRear;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadiusRear, k_MinSpringLen);
            Assert.AreEqual(k_MinSpringLen, result, k_Epsilon,
                "Zero raw spring length should clamp to min spring length");
        }

        [Test]
        public void ComputeSpringLength_NormalDriving_ReturnsRawDistance()
        {
            float anchorToContact = 0.40f;
            float expected = anchorToContact - k_WheelRadiusRear;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadiusRear, k_MinSpringLen);
            Assert.AreEqual(expected, result, k_Epsilon,
                "Normal distance should return raw distance minus radius");
        }

        [Test]
        public void ComputeSpringLength_ZeroMinSpringLen_AllowsZeroLength()
        {
            float anchorToContact = k_WheelRadiusRear;
            float result = SuspensionMath.ComputeSpringLength(anchorToContact, k_WheelRadiusRear, 0f);
            Assert.AreEqual(0f, result, k_Epsilon,
                "With min=0, raw=0 should return exactly 0");
        }
    }
}

#pragma warning restore CS0618
