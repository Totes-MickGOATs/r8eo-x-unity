using NUnit.Framework;
using R8EOX.Input;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for C5 (ghost brake / trigger detection grace period),
    /// M3 (startup grace period), M4 (symmetric steering deadzone),
    /// and related input fixes.
    /// </summary>
    public class InputDetectionTests
    {
        // ---- C5: Trigger detection requires sustained input (k_ConfirmFrames) ----

        [Test]
        public void TriggerDetection_SingleFrameAboveThreshold_DoesNotLock()
        {
            // A single strong input frame should NOT be enough to lock trigger mode.
            // This prevents gamepad noise from ghost-locking to Combined mode.
            var detector = new TriggerDetector(graceFrames: 60, confirmFrames: 5);

            // Simulate one frame of strong combined input, then silence
            detector.ProcessFrame(0f, 0f, 0.5f, frameCount: 61);

            Assert.AreEqual(TriggerDetector.Mode.Detecting, detector.CurrentMode,
                "Single frame should not lock trigger mode");
        }

        [Test]
        public void TriggerDetection_SustainedSeparateInput_LocksSeparate()
        {
            var detector = new TriggerDetector(graceFrames: 60, confirmFrames: 5);

            // Simulate 5 consecutive frames of strong separate trigger input
            for (int i = 0; i < 5; i++)
                detector.ProcessFrame(0.5f, 0f, 0f, frameCount: 61 + i);

            Assert.AreEqual(TriggerDetector.Mode.Separate, detector.CurrentMode,
                "5 consecutive frames of separate input should lock to Separate");
        }

        [Test]
        public void TriggerDetection_SustainedCombinedInput_LocksCombined()
        {
            var detector = new TriggerDetector(graceFrames: 60, confirmFrames: 5);

            for (int i = 0; i < 5; i++)
                detector.ProcessFrame(0f, 0f, 0.5f, frameCount: 61 + i);

            Assert.AreEqual(TriggerDetector.Mode.Combined, detector.CurrentMode,
                "5 consecutive frames of combined input should lock to Combined");
        }

        [Test]
        public void TriggerDetection_DuringGracePeriod_DoesNotDetect()
        {
            // M3: First 60 frames should be ignored
            var detector = new TriggerDetector(graceFrames: 60, confirmFrames: 5);

            // Even strong input during grace period should be ignored
            for (int i = 0; i < 10; i++)
                detector.ProcessFrame(0.9f, 0f, 0f, frameCount: i + 1);

            Assert.AreEqual(TriggerDetector.Mode.Detecting, detector.CurrentMode,
                "Grace period should prevent early detection");
        }

        [Test]
        public void TriggerDetection_InterruptedInput_DoesNotLock()
        {
            var detector = new TriggerDetector(graceFrames: 60, confirmFrames: 5);

            // 3 frames of strong input, then a gap, then 3 more
            for (int i = 0; i < 3; i++)
                detector.ProcessFrame(0.5f, 0f, 0f, frameCount: 61 + i);

            // Gap frame
            detector.ProcessFrame(0f, 0f, 0f, frameCount: 64);

            for (int i = 0; i < 3; i++)
                detector.ProcessFrame(0.5f, 0f, 0f, frameCount: 65 + i);

            // Should NOT have locked because the 5 consecutive frames were interrupted
            // After the gap the counter resets, then 3 more frames is only 3, not 5
            Assert.AreEqual(TriggerDetector.Mode.Detecting, detector.CurrentMode,
                "Interrupted input should reset confirmation counter");
        }

        // ---- M3: Startup grace — first-frame inputs are zero ----

        [Test]
        public void StartupGrace_FrameCountBelowThreshold_ReturnsZeroInputs()
        {
            // When frameCount < 3, all inputs should be zero regardless of raw values
            Assert.IsTrue(InputGuard.ShouldSuppressInput(0),
                "Frame 0 should suppress input");
            Assert.IsTrue(InputGuard.ShouldSuppressInput(1),
                "Frame 1 should suppress input");
            Assert.IsTrue(InputGuard.ShouldSuppressInput(2),
                "Frame 2 should suppress input");
            Assert.IsFalse(InputGuard.ShouldSuppressInput(3),
                "Frame 3 should allow input");
        }

        // ---- M4: Symmetric steering deadzone ----

        [Test]
        public void ApplySymmetricDeadzone_BelowThreshold_ReturnsZero()
        {
            float result = InputMath.ApplySymmetricDeadzone(0.15f, 0.2f);
            Assert.AreEqual(0f, result, 0.0001f,
                "0.15 with deadzone 0.2 should return zero");
        }

        [Test]
        public void ApplySymmetricDeadzone_NegativeBelowThreshold_ReturnsZero()
        {
            float result = InputMath.ApplySymmetricDeadzone(-0.15f, 0.2f);
            Assert.AreEqual(0f, result, 0.0001f,
                "Negative input below deadzone should return zero");
        }

        [Test]
        public void ApplySymmetricDeadzone_AboveThreshold_RemapsWithSign()
        {
            // raw=0.6, dz=0.2 → (0.6-0.2)/(1-0.2) = 0.4/0.8 = 0.5
            float result = InputMath.ApplySymmetricDeadzone(0.6f, 0.2f);
            Assert.AreEqual(0.5f, result, 0.01f,
                "Should remap above deadzone preserving positive sign");
        }

        [Test]
        public void ApplySymmetricDeadzone_NegativeAboveThreshold_RemapsWithSign()
        {
            float result = InputMath.ApplySymmetricDeadzone(-0.6f, 0.2f);
            Assert.AreEqual(-0.5f, result, 0.01f,
                "Should remap above deadzone preserving negative sign");
        }

        [Test]
        public void ApplySymmetricDeadzone_FullDeflection_ReturnsOne()
        {
            Assert.AreEqual(1f, InputMath.ApplySymmetricDeadzone(1f, 0.2f), 0.001f);
        }

        [Test]
        public void ApplySymmetricDeadzone_NegativeFullDeflection_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1f, InputMath.ApplySymmetricDeadzone(-1f, 0.2f), 0.001f);
        }
    }
}
