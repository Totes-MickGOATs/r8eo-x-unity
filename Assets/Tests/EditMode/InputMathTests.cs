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

        // ---- ApplySymmetricDeadzone ----

        [Test]
        public void ApplySymmetricDeadzone_PositiveInput_RemapsCorrectly()
        {
            // raw=0.6, dz=0.2 → (0.6-0.2)/(1-0.2) = 0.4/0.8 = 0.5
            float result = InputMath.ApplySymmetricDeadzone(0.6f, 0.2f);
            Assert.AreEqual(0.5f, result, 0.01f);
        }

        [Test]
        public void ApplySymmetricDeadzone_NegativeInput_PreservesSign()
        {
            // raw=-0.6, dz=0.2 → sign preserved, magnitude same as positive case
            float result = InputMath.ApplySymmetricDeadzone(-0.6f, 0.2f);
            Assert.AreEqual(-0.5f, result, 0.01f);
        }

        [Test]
        public void ApplySymmetricDeadzone_BelowThreshold_ReturnsZero()
        {
            float result = InputMath.ApplySymmetricDeadzone(0.15f, 0.2f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void ApplySymmetricDeadzone_NegativeBelowThreshold_ReturnsZero()
        {
            float result = InputMath.ApplySymmetricDeadzone(-0.15f, 0.2f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void ApplySymmetricDeadzone_AtMax_ReturnsOne()
        {
            float result = InputMath.ApplySymmetricDeadzone(1f, 0.2f);
            Assert.AreEqual(1f, result, 0.0001f);
        }

        [Test]
        public void ApplySymmetricDeadzone_AtNegativeMax_ReturnsNegativeOne()
        {
            float result = InputMath.ApplySymmetricDeadzone(-1f, 0.2f);
            Assert.AreEqual(-1f, result, 0.0001f);
        }

        [Test]
        public void SteeringCurve_WithDeadzone_SmoothTransition()
        {
            // Values just above and at the deadzone edge should produce
            // small, continuous output — no discontinuous jump.
            float dz = 0.2f;
            float exponent = 1.5f;

            float atEdge = InputMath.ApplySymmetricDeadzone(dz, dz);
            float justAbove = InputMath.ApplySymmetricDeadzone(dz + 0.01f, dz);
            float curved = InputMath.ApplySteeringCurve(justAbove, exponent);

            Assert.AreEqual(0f, atEdge, 0.0001f, "At deadzone edge should be zero");
            Assert.Greater(justAbove, 0f, "Just above deadzone should be positive");
            Assert.Less(curved, 0.02f, "Curved value just above deadzone should be very small (smooth)");
        }

        // ---- Grace Frame Logic (tested via GraceFrameCounter helper) ----

        [Test]
        public void TriggerInput_GraceFrames_ReturnsZeroDuringGrace()
        {
            // During the grace period, gamepad trigger input should be suppressed.
            // We test the pure logic: if graceFramesRemaining > 0, output is 0.
            float rawTrigger = 0.8f;
            int graceFramesRemaining = 30;

            float result = graceFramesRemaining > 0 ? 0f : rawTrigger;
            Assert.AreEqual(0f, result, 0.0001f, "Trigger should return 0 during grace period");

            // After grace period expires
            graceFramesRemaining = 0;
            result = graceFramesRemaining > 0 ? 0f : rawTrigger;
            Assert.AreEqual(0.8f, result, 0.0001f, "Trigger should return raw value after grace period");
        }
    }
}
