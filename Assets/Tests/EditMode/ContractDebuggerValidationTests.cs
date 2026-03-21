using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug;
using R8EOX.Input;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Tests for ContractDebugger validation methods and SetTarget API.</summary>
    [TestFixture]
    public class ContractDebuggerValidationTests
    {
#if UNITY_EDITOR || DEBUG
        private GameObject _rootGo;
        private ContractDebugger _debugger;

        [SetUp]
        public void SetUp()
        {
            _rootGo = new GameObject("ContractDebuggerTest");
            _debugger = _rootGo.AddComponent<ContractDebugger>();
            _debugger.ResetCounters();
        }

        [TearDown]
        public void TearDown()
        {
            if (_rootGo != null)
                UnityEngine.Object.DestroyImmediate(_rootGo);
        }

        [Test]
        public void ValidateInputContracts_NoInputRef_NoViolations()
        {
            _debugger.ValidateInputContracts();
            Assert.AreEqual(0, _debugger.InputViolationCount);
        }

        [Test]
        public void ValidateVehicleContracts_NoCarRef_NoViolations()
        {
            _debugger.ValidateVehicleContracts();
            Assert.AreEqual(0, _debugger.VehicleViolationCount);
        }

        [Test]
        public void ValidateWheelContracts_NoWheelRef_NoViolations()
        {
            _debugger.ValidateWheelContracts();
            Assert.AreEqual(0, _debugger.WheelViolationCount);
        }

        [Test]
        public void ValidateObservableContracts_NoCarRef_NoViolations()
        {
            _debugger.ValidateObservableContracts();
            Assert.AreEqual(0, _debugger.ObservableViolationCount);
        }

        [Test]
        public void SetTarget_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _debugger.SetTarget(null));
        }

        [Test]
        public void SetTarget_WithCar_AcquiresReferences()
        {
            var carGo = new GameObject("TestCar");
            carGo.AddComponent<Rigidbody>();
            var car = carGo.AddComponent<RCCar>();

            try
            {
                Assert.DoesNotThrow(() => _debugger.SetTarget(car));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(carGo);
            }
        }

        [Test]
        public void ValidateInputContracts_WithRCInput_DoesNotThrow()
        {
            var carGo = new GameObject("TestCarSafe");
            try
            {
                carGo.AddComponent<Rigidbody>();
                var car = carGo.AddComponent<RCCar>();
                carGo.AddComponent<RCInput>();
                _debugger.SetTarget(car);
                Assert.DoesNotThrow(() => _debugger.ValidateInputContracts());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(carGo);
            }
        }
#endif
    }
}
