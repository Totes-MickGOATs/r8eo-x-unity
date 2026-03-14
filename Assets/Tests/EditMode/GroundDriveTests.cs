using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for C6 (coast drag / IsBraking), M5 (engine cutoff),
    /// and M6 (reverse ESC threshold).
    /// </summary>
    public class GroundDriveTests
    {
        // Default motor values (13.5T preset)
        const float k_EngineForceMax = 26f;
        const float k_BrakeForce = 22.1f;
        const float k_ReverseForce = 14.3f;
        const float k_CoastDrag = 3.0f;
        const float k_MaxSpeed = 27f;
        const float k_ReverseSpeedThreshold = 0.25f;
        const float k_ForwardSpeedClearThreshold = 0.50f;
        const float k_ReverseBrakeMinThreshold = 0.1f;

        // ---- C6: Coasting does NOT set IsBraking ----

        [Test]
        public void Coast_ZeroThrottleZeroBrake_BrakeForceIsZero()
        {
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 0f, brakeIn: 0f, forwardSpeed: 5f,
                reverseEngaged: false,
                engineForceMax: k_EngineForceMax, brakeForce: k_BrakeForce,
                reverseForce: k_ReverseForce, coastDrag: k_CoastDrag,
                maxSpeed: k_MaxSpeed, velocityMagnitude: 5f,
                reverseSpeedThreshold: k_ReverseSpeedThreshold,
                forwardSpeedClearThreshold: k_ForwardSpeedClearThreshold,
                reverseBrakeMinThreshold: k_ReverseBrakeMinThreshold);

            Assert.AreEqual(0f, result.BrakeForce, 0.0001f,
                "Coasting should set BrakeForce to 0, not coastDrag");
            Assert.AreEqual(k_CoastDrag, result.CoastDragForce, 0.0001f,
                "Coast drag should be reported separately");
        }

        [Test]
        public void Coast_DoesNotTriggerIsBraking()
        {
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 0f, brakeIn: 0f, forwardSpeed: 5f,
                reverseEngaged: false,
                engineForceMax: k_EngineForceMax, brakeForce: k_BrakeForce,
                reverseForce: k_ReverseForce, coastDrag: k_CoastDrag,
                maxSpeed: k_MaxSpeed, velocityMagnitude: 5f,
                reverseSpeedThreshold: k_ReverseSpeedThreshold,
                forwardSpeedClearThreshold: k_ForwardSpeedClearThreshold,
                reverseBrakeMinThreshold: k_ReverseBrakeMinThreshold);

            // IsBraking is determined by BrakeForce > 0 in the caller.
            // With BrakeForce = 0, no wheel should be marked as braking.
            Assert.AreEqual(0f, result.BrakeForce,
                "BrakeForce must be 0 during coast so IsBraking stays false");
        }

        // ---- M5: Engine cutoff uses forward speed, not velocity magnitude ----

        [Test]
        public void EngineCutoff_SidewaysSlideAtMaxSpeed_StillAllowsForwardPower()
        {
            // Car sliding sideways: velocity magnitude >= maxSpeed, but forward speed is low
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 1f, brakeIn: 0f, forwardSpeed: 5f,
                reverseEngaged: false,
                engineForceMax: k_EngineForceMax, brakeForce: k_BrakeForce,
                reverseForce: k_ReverseForce, coastDrag: k_CoastDrag,
                maxSpeed: k_MaxSpeed, velocityMagnitude: k_MaxSpeed + 1f,
                reverseSpeedThreshold: k_ReverseSpeedThreshold,
                forwardSpeedClearThreshold: k_ForwardSpeedClearThreshold,
                reverseBrakeMinThreshold: k_ReverseBrakeMinThreshold);

            Assert.Greater(result.EngineForce, 0f,
                "Should still allow forward power when forward speed < maxSpeed, even if velocity magnitude >= maxSpeed");
        }

        [Test]
        public void EngineCutoff_ForwardSpeedAtMax_CutsEngine()
        {
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 1f, brakeIn: 0f, forwardSpeed: k_MaxSpeed,
                reverseEngaged: false,
                engineForceMax: k_EngineForceMax, brakeForce: k_BrakeForce,
                reverseForce: k_ReverseForce, coastDrag: k_CoastDrag,
                maxSpeed: k_MaxSpeed, velocityMagnitude: k_MaxSpeed,
                reverseSpeedThreshold: k_ReverseSpeedThreshold,
                forwardSpeedClearThreshold: k_ForwardSpeedClearThreshold,
                reverseBrakeMinThreshold: k_ReverseBrakeMinThreshold);

            Assert.AreEqual(0f, result.EngineForce, 0.0001f,
                "Engine should cut off when forward speed >= maxSpeed");
        }

        [Test]
        public void EngineCutoff_ReverseSpeedAtMax_CutsEngine()
        {
            // Going in reverse at max speed (negative forward speed)
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 0f, brakeIn: 1f, forwardSpeed: -k_MaxSpeed,
                reverseEngaged: true,
                engineForceMax: k_EngineForceMax, brakeForce: k_BrakeForce,
                reverseForce: k_ReverseForce, coastDrag: k_CoastDrag,
                maxSpeed: k_MaxSpeed, velocityMagnitude: k_MaxSpeed,
                reverseSpeedThreshold: k_ReverseSpeedThreshold,
                forwardSpeedClearThreshold: k_ForwardSpeedClearThreshold,
                reverseBrakeMinThreshold: k_ReverseBrakeMinThreshold);

            // abs(-maxSpeed) >= maxSpeed, so reverse engine should also cut
            Assert.AreEqual(0f, result.EngineForce, 0.0001f,
                "Reverse engine should cut when abs(forwardSpeed) >= maxSpeed");
        }

        // ---- M6: Reverse ESC minimum brake threshold ----

        [Test]
        public void ReverseESC_TinyBrakeValue_DoesNotTriggerReverse()
        {
            // brakeIn = 0.05 (below 0.1 threshold), forward speed near zero
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 0f, brakeIn: 0.05f, forwardSpeed: 0.1f,
                reverseEngaged: false,
                engineForceMax: k_EngineForceMax, brakeForce: k_BrakeForce,
                reverseForce: k_ReverseForce, coastDrag: k_CoastDrag,
                maxSpeed: k_MaxSpeed, velocityMagnitude: 0.1f,
                reverseSpeedThreshold: k_ReverseSpeedThreshold,
                forwardSpeedClearThreshold: k_ForwardSpeedClearThreshold,
                reverseBrakeMinThreshold: k_ReverseBrakeMinThreshold);

            Assert.IsFalse(result.ReverseEngaged,
                "Tiny brake value (0.05) should NOT trigger reverse engagement");
        }

        [Test]
        public void ReverseESC_StrongBrake_TriggersReverse()
        {
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 0f, brakeIn: 0.5f, forwardSpeed: 0.1f,
                reverseEngaged: false,
                engineForceMax: k_EngineForceMax, brakeForce: k_BrakeForce,
                reverseForce: k_ReverseForce, coastDrag: k_CoastDrag,
                maxSpeed: k_MaxSpeed, velocityMagnitude: 0.1f,
                reverseSpeedThreshold: k_ReverseSpeedThreshold,
                forwardSpeedClearThreshold: k_ForwardSpeedClearThreshold,
                reverseBrakeMinThreshold: k_ReverseBrakeMinThreshold);

            Assert.IsTrue(result.ReverseEngaged,
                "Strong brake (0.5) at low speed should trigger reverse");
        }

        [Test]
        public void ReverseESC_BrakeAtExactThreshold_DoesNotTriggerReverse()
        {
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 0f, brakeIn: 0.1f, forwardSpeed: 0.1f,
                reverseEngaged: false,
                engineForceMax: k_EngineForceMax, brakeForce: k_BrakeForce,
                reverseForce: k_ReverseForce, coastDrag: k_CoastDrag,
                maxSpeed: k_MaxSpeed, velocityMagnitude: 0.1f,
                reverseSpeedThreshold: k_ReverseSpeedThreshold,
                forwardSpeedClearThreshold: k_ForwardSpeedClearThreshold,
                reverseBrakeMinThreshold: k_ReverseBrakeMinThreshold);

            Assert.IsFalse(result.ReverseEngaged,
                "Brake exactly at threshold (0.1) should NOT trigger reverse (must be strictly greater)");
        }
    }
}
