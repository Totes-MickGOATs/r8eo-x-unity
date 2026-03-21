using NUnit.Framework;
using R8EOX.Vehicle;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Smoke tests for <see cref="WheelConfig"/> defaults and field presence.
    /// Verifies that the config struct holds sane default values after extraction
    /// from <see cref="RaycastWheel"/>.
    /// </summary>
    public class WheelConfigTests
    {
        WheelConfig _config;

        [SetUp]
        public void SetUp() => _config = new WheelConfig();

        [Test]
        public void RestDistance_DefaultValue_IsPositive()
        {
            Assert.Greater(_config.restDistance, 0f,
                "restDistance default must be positive.");
        }

        [Test]
        public void WheelRadius_DefaultValue_IsPositive()
        {
            Assert.Greater(_config.wheelRadius, 0f,
                "wheelRadius default must be positive.");
        }

        [Test]
        public void GripCurve_DefaultValue_IsNotNull()
        {
            Assert.IsNotNull(_config.gripCurve,
                "gripCurve default must not be null.");
        }

        [Test]
        public void GripCurve_DefaultValue_HasKeys()
        {
            Assert.Greater(_config.gripCurve.length, 0,
                "Default grip curve must have at least one keyframe.");
        }

        [Test]
        public void ZTraction_DefaultValue_IsPositive()
        {
            Assert.Greater(_config.zTraction, 0f,
                "zTraction default must be positive (non-zero longitudinal traction).");
        }

        [Test]
        public void IsMotor_DefaultValue_IsFalse()
        {
            Assert.IsFalse(_config.isMotor,
                "isMotor should default to false.");
        }

        [Test]
        public void IsSteer_DefaultValue_IsFalse()
        {
            Assert.IsFalse(_config.isSteer,
                "isSteer should default to false.");
        }
    }
}
