using NUnit.Framework;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for reverse ESC state machine logic extracted from RCCar.ApplyGroundDrive.
    /// Inputs: throttleIn (0-1), brakeIn (0-1), fwdSpeed (m/s, signed).
    /// </summary>
    public class ReverseESCTests
    {
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
            Assert.IsFalse(result.reverseEngaged);
            Assert.AreEqual(0f, result.engineForce, 0.0001f);
            Assert.AreEqual(k_CoastDrag, result.brakeForce, 0.0001f);
        }

        [Test]
        public void ReverseESC_BrakeWhileStopped_Engages()
        {
            var result = SimulateGroundDrive(0f, 0.8f, 0.1f, false);
            Assert.IsTrue(result.reverseEngaged);
            Assert.Less(result.engineForce, 0f);
        }

        [Test]
        public void ReverseESC_ThrottleClears_Disengages()
        {
            var result = SimulateGroundDrive(0.5f, 0f, 0f, true);
            Assert.IsFalse(result.reverseEngaged);
            Assert.Greater(result.engineForce, 0f);
        }

        [Test]
        public void ReverseESC_ForwardSpeedClears_Disengages()
        {
            var result = SimulateGroundDrive(0f, 0.5f, 1.0f, true);
            Assert.IsFalse(result.reverseEngaged);
            Assert.AreEqual(0f, result.engineForce, 0.0001f);
            Assert.Greater(result.brakeForce, 0f);
        }

        [Test]
        public void ReverseESC_BrakeWhileMoving_DoesNotEngage()
        {
            var result = SimulateGroundDrive(0f, 1.0f, 2.0f, false);
            Assert.IsFalse(result.reverseEngaged);
            Assert.AreEqual(0f, result.engineForce, 0.0001f);
            Assert.Greater(result.brakeForce, 0f);
        }

        [Test]
        public void ReverseESC_EngageAndDrive_ProducesCorrectReverseForce()
        {
            var result = SimulateGroundDrive(0f, 1.0f, 0f, false);
            Assert.IsTrue(result.reverseEngaged);
            Assert.AreEqual(-k_ReverseForce, result.engineForce, 0.01f);
            Assert.AreEqual(0f, result.brakeForce, 0.0001f);
        }

        [Test]
        public void ReverseESC_NoInputAfterReverse_DisengagesAndCoasts()
        {
            var result = SimulateGroundDrive(0f, 0f, -0.5f, true);
            Assert.IsFalse(result.reverseEngaged);
            Assert.AreEqual(0f, result.engineForce, 0.0001f);
            Assert.AreEqual(k_CoastDrag, result.brakeForce, 0.0001f);
        }

        [Test]
        public void ReverseESC_BrakeAtExactThreshold_DoesNotEngage()
        {
            var result = SimulateGroundDrive(0f, 1.0f, k_ReverseSpeedThreshold, false);
            Assert.IsFalse(result.reverseEngaged);
        }
    }
}
