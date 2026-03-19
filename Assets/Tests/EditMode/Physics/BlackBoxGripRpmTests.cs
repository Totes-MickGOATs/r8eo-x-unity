#pragma warning disable CS0618 // Obsolete members under test
using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using static R8EOX.Tests.EditMode.PhysicsTestConstants;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box unit tests for GripMath.ComputeWheelRpm.
    /// Extracted from BlackBoxGripTests to keep each file under 200 lines.
    /// </summary>
    [Category("Fast")]
    public class BlackBoxGripRpmTests
    {
        // =====================================================================
        // GripMath — ComputeWheelRpm
        // =====================================================================

        [Test]
        public void WheelRpm_KnownSpeedAndRadius_CorrectRpm()
        {
            // v = omega * r, RPM = omega * 60 / (2 * pi)
            // RPM = (v / r) * 60 / (2 * pi)
            float speed = 5f;
            float expectedRpm = (speed / k_WheelRadiusRear) * 60f / (2f * UnityEngine.Mathf.PI);
            float rpm = GripMath.ComputeWheelRpm(speed, k_WheelRadiusRear);
            Assert.AreEqual(expectedRpm, rpm, 0.01f,
                "RPM should follow omega = v / r converted to rev/min");
        }

        [Test]
        public void WheelRpm_ZeroSpeed_ZeroRpm()
        {
            float rpm = GripMath.ComputeWheelRpm(0f, k_WheelRadiusRear);
            Assert.AreEqual(0f, rpm, k_Epsilon,
                "Stationary wheel should have zero RPM");
        }

        [Test]
        public void WheelRpm_NegativeSpeed_NegativeRpm()
        {
            float rpm = GripMath.ComputeWheelRpm(-3f, k_WheelRadiusRear);
            Assert.Less(rpm, 0f,
                "Reverse speed should produce negative RPM (direction matters)");
        }

        [Test]
        public void WheelRpm_ZeroRadius_ZeroRpm()
        {
            float rpm = GripMath.ComputeWheelRpm(5f, 0f);
            Assert.AreEqual(0f, rpm, k_Epsilon,
                "Zero radius should return zero RPM (avoid division by zero)");
        }

        [Test]
        public void WheelRpm_NegativeRadius_ZeroRpm()
        {
            float rpm = GripMath.ComputeWheelRpm(5f, -0.1f);
            Assert.AreEqual(0f, rpm, k_Epsilon,
                "Negative radius is physically invalid — should return zero RPM");
        }
    }
}

#pragma warning restore CS0618
