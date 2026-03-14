using NUnit.Framework;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for reverse ESC (Electronic Speed Controller) state machine logic.
    /// Extracted from RCCar.ApplyGroundDrive to verify pure state transitions.
    ///
    /// The ESC reverse logic uses three inputs:
    ///   - throttleIn: 0-1 throttle input
    ///   - brakeIn: 0-1 brake input
    ///   - fwdSpeed: signed forward speed in m/s
    ///
    /// State transitions:
    ///   - Reverse engages when: brake > 0 AND fwdSpeed < 0.25 (nearly stopped)
    ///   - Reverse disengages when: throttle > 0 OR fwdSpeed > 0.50
    ///   - No input (throttle=0, brake=0): reverse always disengages, coast drag applies
    /// </summary>
    public class ReverseESCTests
    {
        // ---- Constants matching RCCar ----
        const float k_ReverseSpeedThreshold = 0.25f;
        const float k_ForwardSpeedClearThreshold = 0.50f;
        const float k_EngineForceMax = 26f;
        const float k_ReverseForce = 14.3f;
        const float k_BrakeForce = 22.1f;
        const float k_CoastDrag = 3.0f;


        /// <summary>
        /// Simulates ApplyGroundDrive logic and returns (reverseEngaged, engineForce, brakeForce).
        /// Mirrors the exact logic from RCCar.ApplyGroundDrive.
        /// </summary>
        private (bool reverseEngaged, float engineForce, float brakeForce) SimulateGroundDrive(
            float throttleIn, float brakeIn, float fwdSpeed, bool priorReverseEngaged)
        {
            bool reverseEngaged = priorReverseEngaged;
            float engineForce;
            float brakeForce;

            // Reverse ESC state machine
            if (throttleIn > 0f || fwdSpeed > k_ForwardSpeedClearThreshold)
                reverseEngaged = false;
            else if (brakeIn > 0f && fwdSpeed < k_ReverseSpeedThreshold)
                reverseEngaged = true;

            // Drive force selection
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


        // ---- State Transition Tests ----

        [Test]
        public void ReverseESC_NoInput_DoesNotEngage()
        {
            // throttle=0, brake=0, fwdSpeed=0 -> reverse should NOT engage
            var result = SimulateGroundDrive(
                throttleIn: 0f, brakeIn: 0f, fwdSpeed: 0f,
                priorReverseEngaged: false);

            Assert.IsFalse(result.reverseEngaged,
                "Reverse must not engage with zero throttle and zero brake");
            Assert.AreEqual(0f, result.engineForce, 0.0001f,
                "Engine force should be zero with no input");
            Assert.AreEqual(k_CoastDrag, result.brakeForce, 0.0001f,
                "Coast drag should apply as brake force when idle");
        }

        [Test]
        public void ReverseESC_BrakeWhileStopped_Engages()
        {
            // throttle=0, brake>0, fwdSpeed<0.25 -> reverse engages
            var result = SimulateGroundDrive(
                throttleIn: 0f, brakeIn: 0.8f, fwdSpeed: 0.1f,
                priorReverseEngaged: false);

            Assert.IsTrue(result.reverseEngaged,
                "Reverse should engage when braking while nearly stopped");
            Assert.Less(result.engineForce, 0f,
                "Engine force should be negative (reverse direction) when reverse is engaged");
        }

        [Test]
        public void ReverseESC_ThrottleClears_Disengages()
        {
            // After reverse engaged, throttle>0 -> reverse disengages
            var result = SimulateGroundDrive(
                throttleIn: 0.5f, brakeIn: 0f, fwdSpeed: 0f,
                priorReverseEngaged: true);

            Assert.IsFalse(result.reverseEngaged,
                "Reverse must disengage when throttle is applied");
            Assert.Greater(result.engineForce, 0f,
                "Engine force should be positive (forward) when throttle is applied");
        }

        [Test]
        public void ReverseESC_ForwardSpeedClears_Disengages()
        {
            // After reverse engaged, fwdSpeed>0.50 -> reverse disengages
            var result = SimulateGroundDrive(
                throttleIn: 0f, brakeIn: 0.5f, fwdSpeed: 1.0f,
                priorReverseEngaged: true);

            Assert.IsFalse(result.reverseEngaged,
                "Reverse must disengage when car is moving forward above clear threshold");
            Assert.AreEqual(0f, result.engineForce, 0.0001f,
                "Engine force should be zero when braking (not reversed) at speed");
            Assert.Greater(result.brakeForce, 0f,
                "Brake force should be positive when braking at speed");
        }

        [Test]
        public void ReverseESC_BrakeWhileMoving_DoesNotEngage()
        {
            // throttle=0, brake>0, fwdSpeed=2.0 -> reverse does NOT engage (only brakes)
            var result = SimulateGroundDrive(
                throttleIn: 0f, brakeIn: 1.0f, fwdSpeed: 2.0f,
                priorReverseEngaged: false);

            Assert.IsFalse(result.reverseEngaged,
                "Reverse must not engage when braking at speed (fwdSpeed > 0.50)");
            Assert.AreEqual(0f, result.engineForce, 0.0001f,
                "No engine force when braking at speed without reverse");
            Assert.Greater(result.brakeForce, 0f,
                "Brake force should apply when braking at speed");
        }

        [Test]
        public void ReverseESC_EngageAndDrive_ProducesCorrectReverseForce()
        {
            // Full brake while stopped: reverse force = -brakeIn * reverseForce
            var result = SimulateGroundDrive(
                throttleIn: 0f, brakeIn: 1.0f, fwdSpeed: 0f,
                priorReverseEngaged: false);

            Assert.IsTrue(result.reverseEngaged);
            Assert.AreEqual(-k_ReverseForce, result.engineForce, 0.01f,
                "Full brake in reverse should produce -reverseForce engine force");
            Assert.AreEqual(0f, result.brakeForce, 0.0001f,
                "Brake force should be zero when reverse is engaged (engine handles it)");
        }

        [Test]
        public void ReverseESC_NoInputAfterReverse_DisengagesAndCoasts()
        {
            // Release all inputs after being in reverse: should disengage and coast
            var result = SimulateGroundDrive(
                throttleIn: 0f, brakeIn: 0f, fwdSpeed: -0.5f,
                priorReverseEngaged: true);

            Assert.IsFalse(result.reverseEngaged,
                "Reverse must disengage when all inputs are released");
            Assert.AreEqual(0f, result.engineForce, 0.0001f,
                "No engine force when coasting");
            Assert.AreEqual(k_CoastDrag, result.brakeForce, 0.0001f,
                "Coast drag should apply when no inputs");
        }

        [Test]
        public void ReverseESC_BrakeAtExactThreshold_DoesNotEngage()
        {
            // fwdSpeed exactly at threshold boundary (0.25) — should still engage
            // because the check is fwdSpeed < 0.25, and fwdSpeed IS 0.25 -> does NOT engage
            var result = SimulateGroundDrive(
                throttleIn: 0f, brakeIn: 1.0f, fwdSpeed: k_ReverseSpeedThreshold,
                priorReverseEngaged: false);

            Assert.IsFalse(result.reverseEngaged,
                "Reverse should not engage when fwdSpeed is exactly at threshold (< not <=)");
        }
    }
}
