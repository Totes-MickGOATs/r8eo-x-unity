using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug.Validators;
using R8EOX.Input;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for VehicleContractValidator.
    /// Tests the static validation method directly — no MonoBehaviour orchestration needed.
    /// </summary>
    [TestFixture]
    public class VehicleContractValidatorTests
    {
#if UNITY_EDITOR || DEBUG
        // ---- Helpers ----

        private GameObject _carGo;
        private RCCar _car;

        [SetUp]
        public void SetUp()
        {
            _carGo = new GameObject("VehicleContractValidatorTestCar");
            _carGo.AddComponent<Rigidbody>();
            _car = _carGo.AddComponent<RCCar>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_carGo != null)
                UnityEngine.Object.DestroyImmediate(_carGo);
        }


        // ---- Null safety ----

        [Test]
        public void ValidateVehicleContracts_NullCar_ReturnsZeroViolations()
        {
            int violations = VehicleContractValidator.ValidateVehicleContracts(null, null, false);
            Assert.AreEqual(0, violations);
        }


        // ---- Default state (fresh RCCar all zeros/defaults) ----

        [Test]
        public void ValidateVehicleContracts_DefaultCar_ReturnsZeroViolations()
        {
            // A freshly instantiated RCCar with all zero forces should produce no violations
            int violations = VehicleContractValidator.ValidateVehicleContracts(_car, null, false);
            Assert.AreEqual(0, violations);
        }

        [Test]
        public void ValidateVehicleContracts_DefaultCar_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                VehicleContractValidator.ValidateVehicleContracts(_car, null, false));
        }


        // ---- With null input ----

        [Test]
        public void ValidateVehicleContracts_NullInput_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                VehicleContractValidator.ValidateVehicleContracts(_car, null, false));
        }


        // ---- logAllValues flag ----

        [Test]
        public void ValidateVehicleContracts_LogAllValues_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                VehicleContractValidator.ValidateVehicleContracts(_car, null, true));
        }
#endif // UNITY_EDITOR || DEBUG
    }
}
