using NUnit.Framework;
using R8EOX.Input;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Additional input processing tests covering edge cases that could cause
    /// incorrect vehicle behavior. Complements InputMathTests with bug-catching scenarios.
    /// </summary>
    public class InputProcessingTests
    {
        const float k_TriggerDeadzone = 0.15f;
        const float k_SteerDeadzone = 0.1f;
        const float k_SteerCurveExponent = 1.5f;


        // ---- Deadzone Edge Cases ----

        [Test]
        public void Deadzone_SmallValue_ReturnsZero()
        {
            // Values well below the deadzone must return exactly zero
            // to prevent phantom input causing the car to creep
            float result = InputMath.ApplyDeadzone(0.05f, k_TriggerDeadzone);

            Assert.AreEqual(0f, result, 0.0001f,
                "Input below deadzone must return exactly zero — phantom input causes creep");
        }

        [Test]
        public void Deadzone_JustAboveThreshold_ReturnsSmallPositive()
        {
            // Just above deadzone should return a small but non-zero value
            // This tests that remapping produces a smooth transition, not a jump
            float justAbove = k_TriggerDeadzone + 0.02f; // 0.17
            float result = InputMath.ApplyDeadzone(justAbove, k_TriggerDeadzone);

            Assert.Greater(result, 0f,
                "Input just above deadzone should return positive value");
            Assert.Less(result, 0.1f,
                "Input just above deadzone should be small (smooth ramp, not jump)");
        }

        [Test]
        public void Deadzone_NegativeBelow_ReturnsZero()
        {
            // Negative values below deadzone must also return zero
            float result = InputMath.ApplyDeadzone(-0.05f, k_TriggerDeadzone);

            Assert.AreEqual(0f, result, 0.0001f,
                "Negative input below deadzone must return zero");
        }

        [Test]
        public void Deadzone_NegativeAbove_ClampsToZero()
        {
            // ApplyDeadzone returns Clamp01(sign * remapped)
            // For negative inputs: sign=-1, remapped is positive,
            // so sign * remapped is negative, Clamp01 returns 0.
            // This means the deadzone function only works for positive triggers.
            float result = InputMath.ApplyDeadzone(-0.5f, k_TriggerDeadzone);

            // The function uses Clamp01, which clamps negative values to 0.
            // This is correct for triggers (0-1 range) but worth documenting.
            Assert.AreEqual(0f, result, 0.0001f,
                "Negative values above deadzone clamp to 0 via Clamp01 (triggers are 0-1)");
        }


        // ---- Steering Curve ----

        [Test]
        public void SteeringCurve_PreservesSign()
        {
            // Critical: steering curve must preserve the sign of input
            // A sign bug here would make the car steer the wrong way
            float positiveResult = InputMath.ApplySteeringCurve(0.7f, k_SteerCurveExponent);
            float negativeResult = InputMath.ApplySteeringCurve(-0.7f, k_SteerCurveExponent);

            Assert.Greater(positiveResult, 0f, "Positive input must produce positive output");
            Assert.Less(negativeResult, 0f, "Negative input must produce negative output");

            // Magnitudes should be equal (symmetric curve)
            Assert.AreEqual(positiveResult, -negativeResult, 0.0001f,
                "Steering curve must be symmetric — |f(x)| == |f(-x)|");
        }

        [Test]
        public void SteeringCurve_ExponentReducesMidRange()
        {
            // Higher exponent should make mid-range inputs smaller (more precision near center)
            float linear = InputMath.ApplySteeringCurve(0.5f, 1.0f);
            float curved = InputMath.ApplySteeringCurve(0.5f, k_SteerCurveExponent);

            Assert.Less(curved, linear,
                "Exponent > 1 should reduce mid-range values for more center precision");
        }

        [Test]
        public void SteeringCurve_FullDeflection_Unchanged()
        {
            // At ±1.0, the curve should return ±1.0 regardless of exponent
            float result = InputMath.ApplySteeringCurve(1.0f, k_SteerCurveExponent);
            Assert.AreEqual(1.0f, result, 0.0001f,
                "Full right deflection must return 1.0 regardless of curve exponent");

            float negResult = InputMath.ApplySteeringCurve(-1.0f, k_SteerCurveExponent);
            Assert.AreEqual(-1.0f, negResult, 0.0001f,
                "Full left deflection must return -1.0 regardless of curve exponent");
        }


        // ---- Merge Inputs ----

        [Test]
        public void MergeInputs_TakesLargerAbsoluteValue()
        {
            // Merge should take whichever source has larger absolute value
            // This allows keyboard override of gamepad and vice versa
            float result1 = InputMath.MergeInputs(0.3f, 0.8f);
            Assert.AreEqual(0.8f, result1, 0.0001f,
                "Should take the larger absolute value");

            float result2 = InputMath.MergeInputs(-0.9f, 0.4f);
            Assert.AreEqual(-0.9f, result2, 0.0001f,
                "Should take larger absolute value even when negative");
        }

        [Test]
        public void MergeInputs_BothZero_ReturnsZero()
        {
            float result = InputMath.MergeInputs(0f, 0f);
            Assert.AreEqual(0f, result, 0.0001f,
                "Both zero should return zero");
        }

        [Test]
        public void MergeInputs_OppositeDirections_TakesLarger()
        {
            // If gamepad says left and keyboard says right, take the one with
            // larger absolute value — this prevents conflicting inputs from canceling
            float result = InputMath.MergeInputs(-0.2f, 0.8f);
            Assert.AreEqual(0.8f, result, 0.0001f,
                "Conflicting directions: should take larger absolute value, not cancel");
        }
    }
}
