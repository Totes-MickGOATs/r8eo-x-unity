#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Input;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for InputMath public functions.
    /// Tests verify physically correct behavior from inputs/outputs only.
    /// Uses realistic 1/10th scale RC car values throughout.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxInputTests
    {
        // =====================================================================
        // InputMath — ApplyDeadzone
        // =====================================================================

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
            // At exactly the deadzone edge, should be zero (inclusive)
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
            // Just above deadzone should be very close to 0 (smooth transition)
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
            // ApplyDeadzone returns Clamp01(sign * remapped), so negative input
            // with sign * remapped < 0 gets clamped to 0
            float result = InputMath.ApplyDeadzone(-0.5f, 0.1f);
            // sign = -1, abs = 0.5, remapped = (0.5-0.1)/0.9 = 0.444
            // sign * remapped = -0.444, Clamp01 => 0
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


        // =====================================================================
        // InputMath — ApplySteeringCurve
        // =====================================================================

        [Test]
        public void SteeringCurve_Exponent1_Linear()
        {
            float result = InputMath.ApplySteeringCurve(0.5f, 1.0f);
            Assert.AreEqual(0.5f, result, k_Epsilon,
                "Exponent 1.0 should give linear response (output = input)");
        }

        [Test]
        public void SteeringCurve_ExponentGreaterThan1_ReducesSmallInputs()
        {
            float linear = InputMath.ApplySteeringCurve(0.5f, 1.0f);
            float curved = InputMath.ApplySteeringCurve(0.5f, 2.0f);
            Assert.Less(curved, linear,
                "Exponent > 1 should reduce magnitude of small inputs");
        }

        [Test]
        public void SteeringCurve_FullDeflection_AlwaysOne()
        {
            Assert.AreEqual(1f, InputMath.ApplySteeringCurve(1.0f, 1.0f), k_Epsilon);
            Assert.AreEqual(1f, InputMath.ApplySteeringCurve(1.0f, 2.0f), k_Epsilon);
            Assert.AreEqual(1f, InputMath.ApplySteeringCurve(1.0f, 3.5f), k_Epsilon);
        }

        [Test]
        public void SteeringCurve_PreservesSignForNegativeInput()
        {
            float result = InputMath.ApplySteeringCurve(-0.5f, 2.0f);
            Assert.Less(result, 0f,
                "Negative input should produce negative output");
            float posResult = InputMath.ApplySteeringCurve(0.5f, 2.0f);
            Assert.AreEqual(-posResult, result, k_Epsilon,
                "Magnitude should be same for positive and negative, just sign-flipped");
        }

        [Test]
        public void SteeringCurve_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, InputMath.ApplySteeringCurve(0f, 1.5f), k_Epsilon);
            Assert.AreEqual(0f, InputMath.ApplySteeringCurve(0f, 3.0f), k_Epsilon);
        }

        [Test]
        public void SteeringCurve_NegativeFullDeflection_NegativeOne()
        {
            float result = InputMath.ApplySteeringCurve(-1.0f, 2.0f);
            Assert.AreEqual(-1f, result, k_Epsilon,
                "Full negative deflection should always be -1.0");
        }


        // =====================================================================
        // InputMath — MergeInputs
        // =====================================================================

        [Test]
        public void MergeInputs_TakesLargerAbsoluteValue()
        {
            float result = InputMath.MergeInputs(0.3f, 0.7f);
            Assert.AreEqual(0.7f, result, k_Epsilon,
                "Should take the value with the larger absolute magnitude");
        }

        [Test]
        public void MergeInputs_NegativeCanWinOverSmallerPositive()
        {
            float result = InputMath.MergeInputs(0.3f, -0.8f);
            Assert.AreEqual(-0.8f, result, k_Epsilon,
                "Negative with larger magnitude should win");
        }

        [Test]
        public void MergeInputs_EqualValues_ReturnsSecond()
        {
            // When abs(a) == abs(b), the condition Abs(a) > Abs(b) is false, so returns b
            float result = InputMath.MergeInputs(0.5f, 0.5f);
            Assert.AreEqual(0.5f, result, k_Epsilon,
                "Equal absolute values should return second input");
        }

        [Test]
        public void MergeInputs_BothZero_Zero()
        {
            float result = InputMath.MergeInputs(0f, 0f);
            Assert.AreEqual(0f, result, k_Epsilon);
        }

        [Test]
        public void MergeInputs_EqualOppositeSign_ReturnsSecond()
        {
            // abs(0.5) == abs(-0.5), so condition is false, returns b
            float result = InputMath.MergeInputs(0.5f, -0.5f);
            Assert.AreEqual(-0.5f, result, k_Epsilon,
                "Equal magnitude opposite sign: should return second");
        }

        [Test]
        public void MergeInputs_FirstLarger_ReturnsFirst()
        {
            float result = InputMath.MergeInputs(-0.9f, 0.2f);
            Assert.AreEqual(-0.9f, result, k_Epsilon,
                "First input with larger magnitude should win");
        }
    }
}

#pragma warning restore CS0618
