using NUnit.Framework;
using R8EOX.Input;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for phantom gamepad trigger input bug.
    /// Root cause: CombinedTriggers axis reports -1.0 at rest on some gamepads
    /// (e.g., Xbox controllers on Windows). Abs(-1.0) = 1.0 exceeds detection
    /// threshold, locking TriggerDetector to Combined mode and producing
    /// constant phantom brake input.
    /// </summary>
    public class PhantomTriggerTests
    {
        // ---- TriggerDetector: constant axis should NOT lock Combined ----

        [Test]
        public void TriggerDetector_ConstantCombinedAxis_ShouldNotLockCombined()
        {
            // A combined axis stuck at 1.0 (abs of -1.0 rest value) every frame
            // should NOT lock to Combined mode — it's a resting value, not user input.
            var detector = new TriggerDetector(graceFrames: 60, confirmFrames: 5);

            // Simulate 10 frames of constant combined value (all identical)
            for (int i = 0; i < 10; i++)
                detector.ProcessFrame(0f, 0f, 1.0f, frameCount: 61 + i);

            Assert.AreNotEqual(TriggerDetector.Mode.Combined, detector.CurrentMode,
                "Constant combined axis value should not lock to Combined mode — " +
                "it indicates a resting value, not user input");
        }

        [Test]
        public void TriggerDetector_VaryingCombinedAxis_ShouldLockCombined()
        {
            // A combined axis that changes value IS real user input.
            // Real analog triggers have natural jitter, so values vary slightly.
            var detector = new TriggerDetector(graceFrames: 60, confirmFrames: 5);

            // Varying strong input (simulates real trigger press with analog jitter)
            detector.ProcessFrame(0f, 0f, 0.78f, frameCount: 61);
            detector.ProcessFrame(0f, 0f, 0.82f, frameCount: 62);
            detector.ProcessFrame(0f, 0f, 0.79f, frameCount: 63);
            detector.ProcessFrame(0f, 0f, 0.81f, frameCount: 64);
            detector.ProcessFrame(0f, 0f, 0.80f, frameCount: 65);

            Assert.AreEqual(TriggerDetector.Mode.Combined, detector.CurrentMode,
                "Varying combined axis should lock to Combined mode");
        }

        [Test]
        public void TriggerDetector_ConstantSeparateAxis_LocksNormally()
        {
            // Separate triggers rest at 0 (below threshold), so any sustained
            // above-threshold value IS real user input. No variance check needed.
            var detector = new TriggerDetector(graceFrames: 60, confirmFrames: 5);

            for (int i = 0; i < 10; i++)
                detector.ProcessFrame(1.0f, 0f, 0f, frameCount: 61 + i);

            Assert.AreEqual(TriggerDetector.Mode.Separate, detector.CurrentMode,
                "Separate triggers don't have resting value issues — constant input should lock");
        }

        [Test]
        public void TriggerDetector_VaryingSeparateAxis_ShouldLockSeparate()
        {
            var detector = new TriggerDetector(graceFrames: 60, confirmFrames: 5);

            // First frame baseline at 0
            detector.ProcessFrame(0f, 0f, 0f, frameCount: 61);

            // Then real input
            for (int i = 0; i < 5; i++)
                detector.ProcessFrame(0.5f, 0f, 0f, frameCount: 62 + i);

            Assert.AreEqual(TriggerDetector.Mode.Separate, detector.CurrentMode,
                "Varying separate axis should lock to Separate mode");
        }

        // ---- InputMath: combined trigger extraction helpers ----

        [Test]
        public void CombinedTriggerThrottle_NegativeRestValue_ReturnsZero()
        {
            // When combined axis = -1.0 at rest, throttle should be 0
            float result = InputMath.CombinedTriggerThrottle(-1.0f, 0.15f);
            Assert.AreEqual(0f, result, 0.0001f,
                "Negative combined axis (rest state) should produce zero throttle");
        }

        [Test]
        public void CombinedTriggerBrake_NegativeRestValue_ProducesBrake()
        {
            // On a NORMAL combined axis controller, -1.0 means full LT press.
            // CombinedTriggerBrake correctly returns brake for this.
            // The phantom trigger bug is prevented at the TriggerDetector level:
            // a controller with resting -1.0 won't lock to Combined mode because
            // the constant value fails the baseline-change check.
            float result = InputMath.CombinedTriggerBrake(-1.0f, 0.15f);
            Assert.AreEqual(1f, result, 0.001f,
                "Full negative combined axis should produce full brake on normal controllers");
        }

        [Test]
        public void CombinedTriggerThrottle_PositiveInput_ReturnsThrottle()
        {
            // Positive combined axis = right trigger pressed
            float result = InputMath.CombinedTriggerThrottle(0.8f, 0.15f);
            Assert.Greater(result, 0f,
                "Positive combined axis should produce throttle");
        }

        [Test]
        public void CombinedTriggerBrake_NegativeInput_ReturnsBrake()
        {
            // For combined axis: negative from actual LT press (not rest value)
            // should produce brake. But we clamp to positive-only after negation.
            // combined = -0.8 from real LT press → -(-0.8) = 0.8 → brake
            float result = InputMath.CombinedTriggerBrake(-0.8f, 0.15f);
            Assert.Greater(result, 0f,
                "Negative combined axis from real LT press should produce brake");
        }

        [Test]
        public void CombinedTriggerThrottle_ZeroInput_ReturnsZero()
        {
            float result = InputMath.CombinedTriggerThrottle(0f, 0.15f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void CombinedTriggerBrake_ZeroInput_ReturnsZero()
        {
            float result = InputMath.CombinedTriggerBrake(0f, 0.15f);
            Assert.AreEqual(0f, result, 0.0001f);
        }

        [Test]
        public void CombinedTriggerBrake_PositiveInput_ReturnsZero()
        {
            // Positive combined = RT pressed → no brake
            float result = InputMath.CombinedTriggerBrake(0.8f, 0.15f);
            Assert.AreEqual(0f, result, 0.0001f,
                "Positive combined axis (RT pressed) should produce zero brake");
        }

        [Test]
        public void CombinedTriggerThrottle_NegativeInput_ReturnsZero()
        {
            // Negative combined = LT pressed → no throttle
            float result = InputMath.CombinedTriggerThrottle(-0.8f, 0.15f);
            Assert.AreEqual(0f, result, 0.0001f,
                "Negative combined axis (LT pressed) should produce zero throttle");
        }
    }
}
