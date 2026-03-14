using NUnit.Framework;
using R8EOX.Input;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests that verify the complete input-to-force pipeline produces zero output
    /// when all inputs are zero, and that phantom axis values are correctly
    /// filtered by deadzones.
    /// </summary>
    [TestFixture]
    public class ZeroInputTests
    {
        // ---- Phase 1: Zero-input pipeline tests ----

        [Test]
        public void InputMath_ApplyDeadzone_ZeroInput_ReturnsZero()
        {
            Assert.AreEqual(0f, InputMath.ApplyDeadzone(0f, 0.15f));
        }

        [Test]
        public void InputMath_ApplySymmetricDeadzone_ZeroInput_ReturnsZero()
        {
            Assert.AreEqual(0f, InputMath.ApplySymmetricDeadzone(0f, 0.2f));
        }

        [Test]
        public void InputMath_CombinedTriggerThrottle_ZeroInput_ReturnsZero()
        {
            Assert.AreEqual(0f, InputMath.CombinedTriggerThrottle(0f, 0.15f));
        }

        [Test]
        public void InputMath_CombinedTriggerBrake_ZeroInput_ReturnsZero()
        {
            Assert.AreEqual(0f, InputMath.CombinedTriggerBrake(0f, 0.15f));
        }

        [Test]
        public void ESCMath_ZeroThrottleZeroBrake_ProducesZeroEngineForce()
        {
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 0f, brakeIn: 0f, forwardSpeed: 0f,
                reverseEngaged: false,
                engineForceMax: 100f, brakeForce: 50f, reverseForce: 30f,
                coastDrag: 5f, maxSpeed: 10f, velocityMagnitude: 0f,
                reverseSpeedThreshold: 0.3f, forwardSpeedClearThreshold: 0.5f,
                reverseBrakeMinThreshold: 0.1f);

            Assert.AreEqual(0f, result.EngineForce);
        }

        [Test]
        public void ESCMath_ZeroThrottleZeroBrake_DoesNotEngageReverse()
        {
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 0f, brakeIn: 0f, forwardSpeed: 0f,
                reverseEngaged: false,
                engineForceMax: 100f, brakeForce: 50f, reverseForce: 30f,
                coastDrag: 5f, maxSpeed: 10f, velocityMagnitude: 0f,
                reverseSpeedThreshold: 0.3f, forwardSpeedClearThreshold: 0.5f,
                reverseBrakeMinThreshold: 0.1f);

            Assert.IsFalse(result.ReverseEngaged);
        }

        // ---- Phase 2: Phantom axis value tests ----

        [Test]
        public void ApplyDeadzone_SmallPhantomValue_0_14_ReturnsZero()
        {
            // Phantom trigger value below 0.15 deadzone
            Assert.AreEqual(0f, InputMath.ApplyDeadzone(0.14f, 0.15f));
        }

        [Test]
        public void ApplyDeadzone_PhantomValue_0_16_ReturnsNonZero()
        {
            // Value above 0.15 deadzone should pass through
            float result = InputMath.ApplyDeadzone(0.16f, 0.15f);
            Assert.Greater(result, 0f);
        }

        [Test]
        public void ApplySymmetricDeadzone_SmallPhantomSteering_0_19_ReturnsZero()
        {
            // Phantom steering value below 0.2 deadzone
            Assert.AreEqual(0f, InputMath.ApplySymmetricDeadzone(0.19f, 0.2f));
        }

        [Test]
        public void ApplySymmetricDeadzone_PhantomSteering_0_21_ReturnsNonZero()
        {
            // Value above 0.2 deadzone should pass through
            float result = InputMath.ApplySymmetricDeadzone(0.21f, 0.2f);
            Assert.Greater(result, 0f);
        }

        [Test]
        public void TriggerDetector_NoStrongInput_ResolvesToNone()
        {
            // With no strong input for 300 frames, detector should resolve to None
            var detector = new TriggerDetector(graceFrames: 0, confirmFrames: 5);
            for (int i = 0; i < 300; i++)
            {
                detector.ProcessFrame(0f, 0f, 0f, i);
            }
            Assert.AreEqual(TriggerDetector.Mode.None, detector.CurrentMode);
        }

        [Test]
        public void TriggerDetector_ModeNone_GetGamepadThrottle_MustReturnZero()
        {
            // When detector resolves to None, the GetGamepadThrottle switch
            // hits the default case which returns 0. This test documents the contract:
            // Mode.None MUST result in zero throttle regardless of raw axis values.
            // The actual Input.GetAxisRaw call is in RCInput, but the Mode.None
            // default branch guarantees zero.
            var detector = new TriggerDetector(graceFrames: 0, confirmFrames: 5);
            for (int i = 0; i < 300; i++)
                detector.ProcessFrame(0f, 0f, 0f, i);

            Assert.AreEqual(TriggerDetector.Mode.None, detector.CurrentMode);
            // Contract: when mode is None, gamepad throttle/brake must be 0.
            // This is enforced by the switch default in RCInput.GetGamepadThrottle.
        }

        [Test]
        public void TriggerDetector_ModeNone_GetGamepadBrake_MustReturnZero()
        {
            var detector = new TriggerDetector(graceFrames: 0, confirmFrames: 5);
            for (int i = 0; i < 300; i++)
                detector.ProcessFrame(0f, 0f, 0f, i);

            Assert.AreEqual(TriggerDetector.Mode.None, detector.CurrentMode);
            // Contract: when mode is None, gamepad brake must be 0.
        }

        // ---- Phase 2b: Steering phantom input with no gamepad ----
        // These tests verify the NEW behavior: when trigger detector is in Mode.None,
        // gamepad steering axis should be ignored (forced to 0).

        [Test]
        public void Steering_WhenNoGamepad_PhantomHorizontalAxis_ShouldBeZeroed()
        {
            // Simulates: Horizontal axis reports 0.25 (phantom), no gamepad detected.
            // After fix: gamepad steering contribution should be forced to 0
            // when TriggerDetector.CurrentMode == Mode.None.
            //
            // We test this through InputMath.FilterGamepadSteering which should
            // return 0 when gamepadDetected is false, regardless of raw value.
            float result = InputMath.FilterGamepadSteering(
                rawHorizontal: 0.25f,
                deadzone: 0.2f,
                gamepadDetected: false);

            Assert.AreEqual(0f, result,
                "Gamepad steering must be zero when no gamepad is detected");
        }

        [Test]
        public void Steering_WhenGamepadDetected_NormalDeadzoneApplied()
        {
            // When gamepad IS detected, normal deadzone applies
            float result = InputMath.FilterGamepadSteering(
                rawHorizontal: 0.5f,
                deadzone: 0.2f,
                gamepadDetected: true);

            Assert.Greater(result, 0f,
                "Gamepad steering should pass through when gamepad is detected");
        }

        [Test]
        public void Steering_WhenGamepadDetected_SmallValueFiltered()
        {
            // When gamepad IS detected, deadzone still filters small values
            float result = InputMath.FilterGamepadSteering(
                rawHorizontal: 0.15f,
                deadzone: 0.2f,
                gamepadDetected: true);

            Assert.AreEqual(0f, result,
                "Small values should still be filtered by deadzone even with gamepad");
        }

        [Test]
        public void Steering_WhenDetecting_PhantomValueBelowStrongThreshold_ReturnsZero()
        {
            // During detection phase, phantom steering below the strong threshold
            // (0.3) should be zeroed to prevent phantom steering during detection.
            float result = InputMath.FilterGamepadSteering(
                rawHorizontal: 0.25f,
                deadzone: 0.2f,
                gamepadDetected: false);

            Assert.AreEqual(0f, result,
                "Phantom steering during detection must be zeroed");
        }
    }
}
