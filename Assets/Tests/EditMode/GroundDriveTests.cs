using NUnit.Framework;
using R8EOX.Vehicle.Physics;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Tests for C6 (coast drag / IsBraking), M5 (engine cutoff), and M6 (reverse ESC threshold).</summary>
    public class GroundDriveTests
    {
        const float k_EngineForceMax = 26f;
        const float k_BrakeForce = 22.1f;
        const float k_ReverseForce = 14.3f;
        const float k_CoastDrag = 3.0f;
        const float k_MaxSpeed = 27f;
        const float k_ReverseSpeedThreshold = 0.25f;
        const float k_ForwardSpeedClearThreshold = 0.50f;
        const float k_ReverseBrakeMinThreshold = 0.1f;

        private ESCMath.GroundDriveResult Drive(float throttle, float brake, float fwdSpeed,
            bool reverseEngaged = false, float velocityMagnitude = -1f)
        {
            if (velocityMagnitude < 0f) velocityMagnitude = fwdSpeed;
            return ESCMath.ComputeGroundDrive(
                throttle, brake, fwdSpeed, reverseEngaged,
                k_EngineForceMax, k_BrakeForce, k_ReverseForce, k_CoastDrag,
                k_MaxSpeed, velocityMagnitude,
                k_ReverseSpeedThreshold, k_ForwardSpeedClearThreshold, k_ReverseBrakeMinThreshold);
        }

        [Test]
        public void Coast_ZeroThrottleZeroBrake_BrakeForceIsZero()
        {
            var result = Drive(0f, 0f, 5f);
            Assert.AreEqual(0f, result.BrakeForce, 0.0001f);
            Assert.AreEqual(k_CoastDrag, result.CoastDragForce, 0.0001f);
        }

        [Test]
        public void Coast_DoesNotTriggerIsBraking()
        {
            var result = Drive(0f, 0f, 5f);
            Assert.AreEqual(0f, result.BrakeForce);
        }

        [Test]
        public void EngineCutoff_SidewaysSlideAtMaxSpeed_StillAllowsForwardPower()
        {
            var result = Drive(1f, 0f, 5f, velocityMagnitude: k_MaxSpeed + 1f);
            Assert.Greater(result.EngineForce, 0f);
        }

        [Test]
        public void EngineCutoff_ForwardSpeedAtMax_CutsEngine()
        {
            var result = Drive(1f, 0f, k_MaxSpeed);
            Assert.AreEqual(0f, result.EngineForce, 0.0001f);
        }

        [Test]
        public void EngineCutoff_ReverseSpeedAtMax_CutsEngine()
        {
            var result = Drive(0f, 1f, -k_MaxSpeed, reverseEngaged: true, velocityMagnitude: k_MaxSpeed);
            Assert.AreEqual(0f, result.EngineForce, 0.0001f);
        }

        [Test]
        public void ReverseESC_TinyBrakeValue_DoesNotTriggerReverse()
        {
            var result = Drive(0f, 0.05f, 0.1f);
            Assert.IsFalse(result.ReverseEngaged);
        }

        [Test]
        public void ReverseESC_StrongBrake_TriggersReverse()
        {
            var result = Drive(0f, 0.5f, 0.1f);
            Assert.IsTrue(result.ReverseEngaged);
        }

        [Test]
        public void ReverseESC_BrakeAtExactThreshold_DoesNotTriggerReverse()
        {
            var result = Drive(0f, 0.1f, 0.1f);
            Assert.IsFalse(result.ReverseEngaged);
        }
    }
}
