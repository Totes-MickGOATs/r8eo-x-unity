using NUnit.Framework;
using R8EOX.Input;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for input processing math: deadzone remapping and steering curves.
    /// </summary>
    public class InputMathTests
    {
        // ---- ApplyDeadzone ----

        [Test]
        public void ApplyDeadzone_BelowThreshold_ReturnsZero()
        {
            float result = InputMath.ApplyDeadzone(0.1f, 0.15f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void ApplyDeadzone_AtThreshold_ReturnsZero()
        {
            float result = InputMath.ApplyDeadzone(0.15f, 0.15f);
            Assert.AreEqual(0f, result, 0.001f);
        }

        [Test]
        public void ApplyDeadzone_FullDeflection_ReturnsOne()
        {
            float result = InputMath.ApplyDeadzone(1f, 0.15f);
            Assert.AreEqual(1f, result, 0.001f);
        }

        [Test]
        public void ApplyDeadzone_MidRange_RemapsCorrectly()
        {
            // raw=0.575, dz=0.15 → (0.575-0.15)/(1-0.15) = 0.425/0.85 = 0.5
            float result = InputMath.ApplyDeadzone(0.575f, 0.15f);
            Assert.AreEqual(0.5f, result, 0.01f);
        }

        [Test]
        public void ApplyDeadzone_NegativeInput_ReturnsZero()
        {
            // Negative below threshold
            float result = InputMath.ApplyDeadzone(-0.1f, 0.15f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        // ---- ApplySteeringCurve ----

        [Test]
        public void ApplySteeringCurve_LinearExponent_ReturnsInput()
        {
            float result = InputMath.ApplySteeringCurve(0.5f, 1.0f);
            Assert.AreEqual(0.5f, result, 0.0001f);
        }

        [Test]
        public void ApplySteeringCurve_HighExponent_ReducesSmallInputs()
        {
            float linear = InputMath.ApplySteeringCurve(0.5f, 1.0f);
            float curved = InputMath.ApplySteeringCurve(0.5f, 1.5f);
            Assert.Less(curved, linear, "Higher exponent should reduce mid-range values");
        }

        [Test]
        public void ApplySteeringCurve_FullDeflection_ReturnsOne()
        {
            float result = InputMath.ApplySteeringCurve(1f, 1.5f);
            Assert.AreEqual(1f, result, 0.0001f);
        }

        [Test]
        public void ApplySteeringCurve_NegativeInput_PreservesSign()
        {
            float result = InputMath.ApplySteeringCurve(-0.5f, 1.5f);
            Assert.Less(result, 0f, "Negative input should produce negative output");
        }

        [Test]
        public void ApplySteeringCurve_Zero_ReturnsZero()
        {
            float result = InputMath.ApplySteeringCurve(0f, 1.5f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        // ---- MergeInputs ----

        [Test]
        public void MergeInputs_FirstLarger_ReturnsFirst()
        {
            float result = InputMath.MergeInputs(0.8f, 0.3f);
            Assert.AreEqual(0.8f, result, 0.0001f);
        }

        [Test]
        public void MergeInputs_SecondLarger_ReturnsSecond()
        {
            float result = InputMath.MergeInputs(0.2f, 0.9f);
            Assert.AreEqual(0.9f, result, 0.0001f);
        }

        [Test]
        public void MergeInputs_NegativeLarger_ReturnsNegative()
        {
            float result = InputMath.MergeInputs(-0.7f, 0.3f);
            Assert.AreEqual(-0.7f, result, 0.0001f);
        }
    }
}
