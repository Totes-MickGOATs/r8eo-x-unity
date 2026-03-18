using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug;
using R8EOX.Input;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for ContractDebugger contract validation logic.</summary>
    [TestFixture]
    public class ContractDebuggerTests
    {
#if UNITY_EDITOR || DEBUG
        // ---- Constants ----

        const float k_ValidThrottle = 0.5f;
        const float k_ValidBrake = 0.3f;
        const float k_ValidSteer = 0.7f;

        // ---- Helpers ----

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

        // ---- Toggle Tests ----

        [Test]
        public void EnableInputContracts_DefaultsToTrue()
        {
            Assert.IsTrue(_debugger.EnableInputContracts);
        }

        [Test]
        public void EnableVehicleContracts_DefaultsToTrue()
        {
            Assert.IsTrue(_debugger.EnableVehicleContracts);
        }

        [Test]
        public void EnableWheelContracts_DefaultsToTrue()
        {
            Assert.IsTrue(_debugger.EnableWheelContracts);
        }

        [Test]
        public void EnableObservableContracts_DefaultsToTrue()
        {
            Assert.IsTrue(_debugger.EnableObservableContracts);
        }

        [Test]
        public void DisableInputContracts_CanBeToggled()
        {
            _debugger.EnableInputContracts = false;
            Assert.IsFalse(_debugger.EnableInputContracts);
        }

        [Test]
        public void DisableVehicleContracts_CanBeToggled()
        {
            _debugger.EnableVehicleContracts = false;
            Assert.IsFalse(_debugger.EnableVehicleContracts);
        }

        [Test]
        public void DisableWheelContracts_CanBeToggled()
        {
            _debugger.EnableWheelContracts = false;
            Assert.IsFalse(_debugger.EnableWheelContracts);
        }

        [Test]
        public void DisableObservableContracts_CanBeToggled()
        {
            _debugger.EnableObservableContracts = false;
            Assert.IsFalse(_debugger.EnableObservableContracts);
        }

        // ---- Counter Tests ----

        [Test]
        public void ResetCounters_AllCountersZero()
        {
            _debugger.ResetCounters();
            Assert.AreEqual(0, _debugger.InputViolationCount);
            Assert.AreEqual(0, _debugger.VehicleViolationCount);
            Assert.AreEqual(0, _debugger.WheelViolationCount);
            Assert.AreEqual(0, _debugger.ObservableViolationCount);
        }


        // ---- Validation (no refs = no violations) ----

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


        // ---- SetTarget API ----

        [Test]
        public void SetTarget_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _debugger.SetTarget(null));
        }

        [Test]
        public void SetTarget_WithCar_AcquiresReferences()
        {
            // Create a minimal RCCar setup
            var carGo = new GameObject("TestCar");
            var rb = carGo.AddComponent<Rigidbody>();
            var car = carGo.AddComponent<RCCar>();

            try
            {
                // Should not throw even without wheels
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
            // Integration safety: ValidateInputContracts with RCInput wired up
            // should not throw even without real gamepad input.
            var carGo = new GameObject("TestCarSafe");
            try
            {
                var rb = carGo.AddComponent<Rigidbody>();
                var car = carGo.AddComponent<RCCar>();
                carGo.AddComponent<RCInput>();

                _debugger.SetTarget(car);
                Assert.DoesNotThrow(() => _debugger.ValidateInputContracts(),
                    "ValidateInputContracts should not throw with RCInput attached");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(carGo);
            }
        }

#endif // UNITY_EDITOR || DEBUG
    }
}
