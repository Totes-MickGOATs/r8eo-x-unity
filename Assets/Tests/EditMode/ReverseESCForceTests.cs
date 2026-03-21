using NUnit.Framework;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Force-output tests for reverse ESC (Electronic Speed Controller).
    /// Verifies correct engine and brake force magnitudes when engaged.
    /// State-transition tests live in ReverseESCStateTests.cs.
    /// </summary>
    public class ReverseESCForceTests
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
        public void ReverseESC_EngageAndDrive_ProducesCorrectReverseForce()
        {
            var result = SimulateGroundDrive(
                throttleIn: 0f, brakeIn: 1.0f, fwdSpeed: 0f,
                priorReverseEngaged: false);

            Assert.IsTrue(result.reverseEngaged);
            Assert.AreEqual(-k_ReverseForce, result.engineForce, 0.01f,
                "Full brake in reverse should produce -reverseForce engine force");
            Assert.AreEqual(0f, result.brakeForce, 0.0001f,
                "Brake force should be zero when reverse is engaged (engine handles it)");
        }
    }
}
