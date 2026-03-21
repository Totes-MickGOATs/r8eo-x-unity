#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Input;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Black-box unit tests for InputMath.ApplySteeringCurve and MergeInputs.</summary>
    [Category("Fast")]
    public class BlackBoxSteeringCurveMergeTests
    {
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
