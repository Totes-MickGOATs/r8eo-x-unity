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

    }
}
