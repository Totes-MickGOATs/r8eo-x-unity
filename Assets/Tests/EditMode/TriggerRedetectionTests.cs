using NUnit.Framework;
using R8EOX.Input;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for Issue #50: after TriggerDetector resolves to Mode.None (no gamepad
    /// triggers pressed within 300 frames), it should re-enter Detecting mode when
    /// strong input appears. Without this fix, gamepad throttle/brake/steering are
    /// permanently locked out once None is reached.
    /// </summary>
    public class TriggerRedetectionTests
    {
        // ---- Helpers ----

        /// <summary>
        /// Advance a detector to Mode.None by processing 300 frames with no input.
        /// Grace frames are set to 0 so all 300 frames count toward the timeout.
        /// </summary>
        private static TriggerDetector CreateDetectorAtNone(int confirmFrames = 5)
        {
            var detector = new TriggerDetector(graceFrames: 0, confirmFrames: confirmFrames);
            for (int i = 0; i < 300; i++)
                detector.ProcessFrame(0f, 0f, 0f, frameCount: i);

            Assert.AreEqual(TriggerDetector.Mode.None, detector.CurrentMode,
                "Precondition: detector should be in None after 300 empty frames");
            return detector;
        }

        // ---- Test 1: Strong input after None triggers re-detection ----

        [Test]
        public void TriggerDetector_AfterNoneTimeout_StrongSeparateInput_ReEntersDetecting()
        {
            var detector = CreateDetectorAtNone();

            // Strong RT input should cause re-entry to Detecting
            detector.ProcessFrame(0.5f, 0f, 0f, frameCount: 301);

            Assert.AreNotEqual(TriggerDetector.Mode.None, detector.CurrentMode,
                "Strong separate input after None should leave None mode");
        }

        [Test]
        public void TriggerDetector_AfterNoneTimeout_StrongCombinedInput_ReEntersDetecting()
        {
            var detector = CreateDetectorAtNone();

            // Strong combined input should cause re-entry to Detecting
            detector.ProcessFrame(0f, 0f, 0.5f, frameCount: 301);

            Assert.AreNotEqual(TriggerDetector.Mode.None, detector.CurrentMode,
                "Strong combined input after None should leave None mode");
        }

        [Test]
        public void TriggerDetector_AfterNoneTimeout_StrongLTInput_ReEntersDetecting()
        {
            var detector = CreateDetectorAtNone();

            // Strong LT input should also trigger re-detection
            detector.ProcessFrame(0f, 0.5f, 0f, frameCount: 301);

            Assert.AreNotEqual(TriggerDetector.Mode.None, detector.CurrentMode,
                "Strong LT input after None should leave None mode");
        }

        // ---- Test 2: Re-detection can lock to Separate ----

        [Test]
        public void TriggerDetector_AfterRedetection_SeparateConfirmed_LocksSeparate()
        {
            var detector = CreateDetectorAtNone();

            // Process enough frames of strong separate input to trigger re-detection
            // AND lock to Separate mode. The re-detection frame itself starts
            // the process, and subsequent frames confirm it.
            float[] values = { 0.50f, 0.52f, 0.48f, 0.51f, 0.49f, 0.53f };
            for (int i = 0; i < values.Length; i++)
                detector.ProcessFrame(values[i], 0f, 0f, frameCount: 301 + i);

            Assert.AreEqual(TriggerDetector.Mode.Separate, detector.CurrentMode,
                "After re-detection, sustained separate input should lock to Separate");
        }

        // ---- Test 3: Re-detection can lock to Combined ----

        [Test]
        public void TriggerDetector_AfterRedetection_CombinedConfirmed_LocksCombined()
        {
            var detector = CreateDetectorAtNone();

            // Process enough frames of varying combined input to trigger re-detection
            // AND lock to Combined mode.
            float[] values = { 0.50f, 0.52f, 0.48f, 0.51f, 0.49f, 0.53f };
            for (int i = 0; i < values.Length; i++)
                detector.ProcessFrame(0f, 0f, values[i], frameCount: 301 + i);

            Assert.AreEqual(TriggerDetector.Mode.Combined, detector.CurrentMode,
                "After re-detection, sustained varying combined input should lock to Combined");
        }

        // ---- Test 4: Weak input stays None (noise rejection) ----

        [Test]
        public void TriggerDetector_ModeNone_WeakInput_StaysNone()
        {
            var detector = CreateDetectorAtNone();

            // Weak input below the strong threshold (0.3) should NOT trigger re-detection
            for (int i = 0; i < 10; i++)
                detector.ProcessFrame(0.2f, 0.1f, 0.15f, frameCount: 301 + i);

            Assert.AreEqual(TriggerDetector.Mode.None, detector.CurrentMode,
                "Weak input (below 0.3 threshold) should not trigger re-detection");
        }

        // ---- Test 5: Re-detection resets internal state cleanly ----

        [Test]
        public void TriggerDetector_RedetectionResetsConfirmCounters()
        {
            var detector = CreateDetectorAtNone();

            // Start re-detection with strong input, then go silent.
            // The detector should be in Detecting, not locked, because
            // we didn't provide enough consecutive frames.
            detector.ProcessFrame(0.5f, 0f, 0f, frameCount: 301);

            // Now go silent for a frame — confirm counter should reset
            detector.ProcessFrame(0f, 0f, 0f, frameCount: 302);

            // Then provide 3 frames of separate (less than confirmFrames=5)
            for (int i = 0; i < 3; i++)
                detector.ProcessFrame(0.5f, 0f, 0f, frameCount: 303 + i);

            Assert.AreEqual(TriggerDetector.Mode.Detecting, detector.CurrentMode,
                "Re-detection should start fresh — interrupted input should not lock");
        }

        // ---- Test 6: Re-detection timeout back to None works ----

        [Test]
        public void TriggerDetector_AfterRedetection_CanTimeoutToNoneAgain()
        {
            var detector = CreateDetectorAtNone();

            // Trigger re-detection
            detector.ProcessFrame(0.5f, 0f, 0f, frameCount: 301);

            Assert.AreEqual(TriggerDetector.Mode.Detecting, detector.CurrentMode,
                "Should be in Detecting after re-detection trigger");

            // Now provide 300 empty frames — should timeout to None again
            for (int i = 0; i < 300; i++)
                detector.ProcessFrame(0f, 0f, 0f, frameCount: 302 + i);

            Assert.AreEqual(TriggerDetector.Mode.None, detector.CurrentMode,
                "After re-detection, 300 empty frames should timeout to None again");
        }
    }
}
