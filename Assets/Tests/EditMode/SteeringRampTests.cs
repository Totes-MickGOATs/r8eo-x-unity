using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    public class SteeringRampTests
    {
        const float k_Epsilon = 0.001f;

        [Test]
        public void Step_ZeroSteerInput_MovesTowardZero()
        {
            float result = SteeringRamp.Step(0.3f, 0.1f, 0f, 0f, 0.5f, 7f, 8f, 0.4f);
            Assert.Less(result, 0.3f);
        }

        [Test]
        public void Step_FullSteerAtLowSpeed_ReachesSteeringMax()
        {
            float current = 0f;
            for (int i = 0; i < 100; i++)
                current = SteeringRamp.Step(current, 0.1f, 1f, 0f, 0.5f, 7f, 8f, 0.4f);
            Assert.AreEqual(0.5f, current, k_Epsilon);
        }

        [Test]
        public void Step_HighSpeed_ReducesEffectiveMax()
        {
            // At high speed (= speedLimit), effectiveMax = steeringMax * highSpeedFactor
            float current = 0f;
            for (int i = 0; i < 200; i++)
                current = SteeringRamp.Step(current, 0.05f, 1f, 8f, 0.5f, 7f, 8f, 0.4f);
            float expected = 0.5f * 0.4f;
            Assert.AreEqual(expected, current, k_Epsilon);
        }

        [Test]
        public void Step_ReverseSpeed_InvertsSteerSign()
        {
            // Negative forward speed beyond threshold → steerSign = -1
            float current = 0f;
            for (int i = 0; i < 200; i++)
                current = SteeringRamp.Step(current, 0.05f, 1f, -1f, 0.5f, 7f, 8f, 0.4f);
            Assert.Less(current, 0f, "Steering should be negative when reversing");
        }

        [Test]
        public void Step_RampLimitedBySpeed()
        {
            // With dt=0.001 and steeringSpeed=7, max change per step = 0.007
            float result = SteeringRamp.Step(0f, 0.001f, 1f, 0f, 0.5f, 7f, 8f, 0.4f);
            Assert.AreEqual(0.007f, result, k_Epsilon);
        }
    }
}
