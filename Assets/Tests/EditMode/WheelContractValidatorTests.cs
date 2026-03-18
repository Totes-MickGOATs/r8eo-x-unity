using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug.Validators;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for WheelContractValidator.
    /// Tests the static validation method directly — no MonoBehaviour orchestration needed.
    /// </summary>
    [TestFixture]
    public class WheelContractValidatorTests
    {
#if UNITY_EDITOR || DEBUG
        // ---- Helpers ----

        private GameObject _wheelGo;
        private RaycastWheel _wheel;

        [SetUp]
        public void SetUp()
        {
            _wheelGo = new GameObject("WheelContractValidatorTestWheel");
            _wheel = _wheelGo.AddComponent<RaycastWheel>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_wheelGo != null)
                UnityEngine.Object.DestroyImmediate(_wheelGo);
        }


        // ---- Null safety ----

        [Test]
        public void ValidateWheelContracts_NullArray_ReturnsZeroViolations()
        {
            int violations = WheelContractValidator.ValidateWheelContracts(null, false);
            Assert.AreEqual(0, violations);
        }

        [Test]
        public void ValidateWheelContracts_EmptyArray_ReturnsZeroViolations()
        {
            int violations = WheelContractValidator.ValidateWheelContracts(new RaycastWheel[0], false);
            Assert.AreEqual(0, violations);
        }


        // ---- Default state (fresh RaycastWheel all zeros/defaults) ----

        [Test]
        public void ValidateWheelContracts_DefaultWheel_ReturnsZeroViolations()
        {
            // A freshly created RaycastWheel is not grounded and all values at zero/default
            var wheels = new RaycastWheel[] { _wheel };
            int violations = WheelContractValidator.ValidateWheelContracts(wheels, false);
            Assert.AreEqual(0, violations);
        }

        [Test]
        public void ValidateWheelContracts_DefaultWheel_DoesNotThrow()
        {
            var wheels = new RaycastWheel[] { _wheel };
            Assert.DoesNotThrow(() =>
                WheelContractValidator.ValidateWheelContracts(wheels, false));
        }


        // ---- Motor wheel with zero force has no violation ----

        [Test]
        public void ValidateWheelContracts_MotorWheelZeroForce_ReturnsZeroViolations()
        {
            _wheel.IsMotor = true;
            _wheel.MotorForceShare = 0f;
            var wheels = new RaycastWheel[] { _wheel };
            int violations = WheelContractValidator.ValidateWheelContracts(wheels, false);
            Assert.AreEqual(0, violations);
        }


        // ---- logAllValues flag ----

        [Test]
        public void ValidateWheelContracts_LogAllValues_DoesNotThrow()
        {
            var wheels = new RaycastWheel[] { _wheel };
            Assert.DoesNotThrow(() =>
                WheelContractValidator.ValidateWheelContracts(wheels, true));
        }
#endif // UNITY_EDITOR || DEBUG
    }
}
