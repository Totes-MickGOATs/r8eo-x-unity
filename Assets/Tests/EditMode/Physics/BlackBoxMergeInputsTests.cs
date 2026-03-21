#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Input;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for InputMath.MergeInputs.
    /// Deadzone tests live in BlackBoxDeadzoneTests.cs.
    /// SteeringCurve tests live in BlackBoxSteeringCurveTests.cs.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxMergeInputsTests
    {
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
