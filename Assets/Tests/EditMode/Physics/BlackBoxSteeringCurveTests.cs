#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Input;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for InputMath.ApplySteeringCurve.
    /// Deadzone tests live in BlackBoxDeadzoneTests.cs.
    /// MergeInputs tests live in BlackBoxMergeInputsTests.cs.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxSteeringCurveTests
    {
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
    }
}

#pragma warning restore CS0618
