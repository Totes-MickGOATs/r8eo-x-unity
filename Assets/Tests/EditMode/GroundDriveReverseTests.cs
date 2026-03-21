using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// M6 (reverse ESC minimum brake threshold) tests for ESCMath.ComputeGroundDrive.
    /// C6/M5 (coast and engine cutoff) tests live in GroundDriveCoastTests.cs.
    /// </summary>
    public class GroundDriveReverseTests
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
        public void ReverseESC_TinyBrakeValue_DoesNotTriggerReverse()
        {
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
