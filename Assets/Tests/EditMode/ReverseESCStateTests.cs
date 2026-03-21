using NUnit.Framework;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// State transition tests for reverse ESC (Electronic Speed Controller).
    /// Covers engage / disengage conditions extracted from RCCar.ApplyGroundDrive.
    /// Force-output tests live in ReverseESCForceTests.cs.
    /// </summary>
    public class ReverseESCStateTests
    {
        // ---- Constants matching RCCar ----
        const float k_ReverseSpeedThreshold = 0.25f;
        const float k_ForwardSpeedClearThreshold = 0.50f;
        const float k_EngineForceMax = 26f;
        const float k_ReverseForce = 14.3f;
        const float k_BrakeForce = 22.1f;
        const float k_CoastDrag = 3.0f;

        private (bool reverseEngaged, float engineForce, float brakeForce) SimulateGroundDrive(
            float throttleIn, float brakeIn, float fwdSpeed, bool priorReverseEngaged)
        {
            bool reverseEngaged = priorReverseEngaged;
            float engineForce;
            float brakeForce;
            if (throttleIn > 0f || fwdSpeed > k_ForwardSpeedClearThreshold)
                reverseEngaged = false;
            else if (brakeIn > 0f && fwdSpeed < k_ReverseSpeedThreshold)
                reverseEngaged = true;
            if (throttleIn > 0f)
            {
                engineForce = throttleIn * k_EngineForceMax;
                brakeForce = 0f;
            }
            else if (brakeIn > 0f)
            {
                if (reverseEngaged)
                {
                    engineForce = -brakeIn * k_ReverseForce;
                    brakeForce = 0f;
                }
                else
                {
                    engineForce = 0f;
                    brakeForce = brakeIn * k_BrakeForce;
                }
            }
            else
            {
                reverseEngaged = false;
                engineForce = 0f;
                brakeForce = k_CoastDrag;
            }
            return (reverseEngaged, engineForce, brakeForce);
        }

        [Test]
        public void ReverseESC_NoInput_DoesNotEngage()
        {
            var result = SimulateGroundDrive(0f, 0f, 0f, false);
            Assert.IsFalse(result.reverseEngaged, "Reverse must not engage with zero inputs");
            Assert.AreEqual(0f, result.engineForce, 0.0001f);
            Assert.AreEqual(k_CoastDrag, result.brakeForce, 0.0001f);
        }

        [Test]
        public void ReverseESC_BrakeWhileStopped_Engages()
        {
            var result = SimulateGroundDrive(0f, 0.8f, 0.1f, false);
            Assert.IsTrue(result.reverseEngaged, "Reverse should engage when braking while nearly stopped");
            Assert.Less(result.engineForce, 0f, "Engine force should be negative when reverse engaged");
        }

        [Test]
        public void ReverseESC_ThrottleClears_Disengages()
        {
            var result = SimulateGroundDrive(0.5f, 0f, 0f, true);
            Assert.IsFalse(result.reverseEngaged, "Reverse must disengage when throttle is applied");
            Assert.Greater(result.engineForce, 0f, "Engine force should be positive (forward)");
        }

        [Test]
        public void ReverseESC_ForwardSpeedClears_Disengages()
        {
            var result = SimulateGroundDrive(0f, 0.5f, 1.0f, true);
            Assert.IsFalse(result.reverseEngaged, "Reverse must disengage above clear threshold");
            Assert.AreEqual(0f, result.engineForce, 0.0001f);
            Assert.Greater(result.brakeForce, 0f);
        }

        [Test]
        public void ReverseESC_BrakeWhileMoving_DoesNotEngage()
        {
            var result = SimulateGroundDrive(0f, 1.0f, 2.0f, false);
            Assert.IsFalse(result.reverseEngaged, "Reverse must not engage when braking at speed");
            Assert.AreEqual(0f, result.engineForce, 0.0001f);
            Assert.Greater(result.brakeForce, 0f);
        }

        [Test]
        public void ReverseESC_NoInputAfterReverse_DisengagesAndCoasts()
        {
            var result = SimulateGroundDrive(0f, 0f, -0.5f, true);
            Assert.IsFalse(result.reverseEngaged, "Reverse must disengage when inputs released");
            Assert.AreEqual(0f, result.engineForce, 0.0001f);
            Assert.AreEqual(k_CoastDrag, result.brakeForce, 0.0001f);
        }

        [Test]
        public void ReverseESC_BrakeAtExactThreshold_DoesNotEngage()
        {
            var result = SimulateGroundDrive(0f, 1.0f, k_ReverseSpeedThreshold, false);
            Assert.IsFalse(result.reverseEngaged, "Reverse should not engage at exact threshold (< not <=)");
        }
    }
}
