using NUnit.Framework;
using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for SteeringRamp speed-dependent angle reduction and reverse flip.
    /// </summary>
    public class SteeringRampTests
    {
        const float k_Eps = 0.001f;

        [Test]
        public void Update_FullRightInput_ConvergesOnSteeringMax()
        {
            var ramp = new SteeringRamp();
            // Run many frames to reach target
            for (int i = 0; i < 200; i++)
                ramp.Update(0.02f, steerIn: 1f, fwdSpeed: 0f,
                    steeringMax: 0.5f, steeringSpeed: 7f, speedLimit: 8f, highSpeedFactor: 0.4f);

            Assert.AreEqual(0.5f, ramp.CurrentSteering, k_Eps);
        }

        [Test]
        public void Update_HighSpeed_ReducesMaxSteerAngle()
        {
            var ramp = new SteeringRamp();
            // Run at speed beyond speedLimit
            for (int i = 0; i < 200; i++)
                ramp.Update(0.02f, steerIn: 1f, fwdSpeed: 20f,
                    steeringMax: 0.5f, steeringSpeed: 7f, speedLimit: 8f, highSpeedFactor: 0.4f);

            // At high speed, effective max = steeringMax * highSpeedFactor = 0.5 * 0.4 = 0.2
            Assert.AreEqual(0.2f, ramp.CurrentSteering, k_Eps);
        }

        [Test]
        public void Update_ReversingWithFullLeftInput_FlipsSteerSign()
        {
            var ramp = new SteeringRamp();
            // Reversing at -1 m/s (beyond threshold)
            for (int i = 0; i < 200; i++)
                ramp.Update(0.02f, steerIn: 1f, fwdSpeed: -1f,
                    steeringMax: 0.5f, steeringSpeed: 7f, speedLimit: 8f, highSpeedFactor: 0.4f);

            // At reverse speed, steer sign flips: target = 1 * 0.5 * -1 = -0.5
            Assert.AreEqual(-0.5f, ramp.CurrentSteering, k_Eps);
        }

        [Test]
        public void Update_NeutralInput_SteeringMovesTowardZero()
        {
            var ramp = new SteeringRamp();
            // First steer full right
            for (int i = 0; i < 200; i++)
                ramp.Update(0.02f, 1f, 0f, 0.5f, 7f, 8f, 0.4f);
            Assert.Greater(ramp.CurrentSteering, 0f);

            // Then release
            for (int i = 0; i < 200; i++)
                ramp.Update(0.02f, 0f, 0f, 0.5f, 7f, 8f, 0.4f);

            Assert.AreEqual(0f, ramp.CurrentSteering, k_Eps);
        }
    }
}
