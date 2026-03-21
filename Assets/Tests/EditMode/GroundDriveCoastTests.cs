using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// C6 (coast drag / IsBraking) and M5 (engine cutoff) tests.
    /// M6 (reverse ESC threshold) tests live in GroundDriveReverseTests.cs.
    /// </summary>
    public class GroundDriveCoastTests
    {
        const float k_EngineForceMax = 26f;
        const float k_BrakeForce = 22.1f;
        const float k_ReverseForce = 14.3f;
        const float k_CoastDrag = 3.0f;
        const float k_MaxSpeed = 27f;
        const float k_ReverseSpeedThreshold = 0.25f;
        const float k_ForwardSpeedClearThreshold = 0.50f;
        const float k_ReverseBrakeMinThreshold = 0.1f;

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

            Assert.AreEqual(0f, result.BrakeForce,
                "BrakeForce must be 0 during coast so IsBraking stays false");
        }

        [Test]
        public void EngineCutoff_SidewaysSlideAtMaxSpeed_StillAllowsForwardPower()
        {
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
            var result = ESCMath.ComputeGroundDrive(
                throttleIn: 0f, brakeIn: 1f, forwardSpeed: -k_MaxSpeed,
                reverseEngaged: true,
                engineForceMax: k_EngineForceMax, brakeForce: k_BrakeForce,
                reverseForce: k_ReverseForce, coastDrag: k_CoastDrag,
                maxSpeed: k_MaxSpeed, velocityMagnitude: k_MaxSpeed,
                reverseSpeedThreshold: k_ReverseSpeedThreshold,
                forwardSpeedClearThreshold: k_ForwardSpeedClearThreshold,
                reverseBrakeMinThreshold: k_ReverseBrakeMinThreshold);

            Assert.AreEqual(0f, result.EngineForce, 0.0001f,
                "Reverse engine should cut when abs(forwardSpeed) >= maxSpeed");
        }
    }
}
