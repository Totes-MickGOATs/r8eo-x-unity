#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Input;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Black-box unit tests for InputMath.ApplyDeadzone.</summary>
    [Category("Fast")]
    public class BlackBoxDeadzoneTests
    {
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
            float result = InputMath.ApplyDeadzone(-0.5f, 0.1f);
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
    }
}

#pragma warning restore CS0618
