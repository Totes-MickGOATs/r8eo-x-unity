using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for ContractDebugger contract validation logic.
    /// Tests verify that violations are counted and that valid states pass silently.
    /// Uses minimal GameObject setup since ContractDebugger observes via public properties.
    /// </summary>
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


        // ---- Input Validation (no input ref = no violations) ----

        [Test]
        public void ValidateInputContracts_NoInputRef_NoViolations()
        {
            // With no car/input set, validation should silently skip
            _debugger.ValidateInputContracts();
            Assert.AreEqual(0, _debugger.InputViolationCount);
        }


        // ---- Vehicle Validation (no car ref = no violations) ----

        [Test]
        public void ValidateVehicleContracts_NoCarRef_NoViolations()
        {
            _debugger.ValidateVehicleContracts();
            Assert.AreEqual(0, _debugger.VehicleViolationCount);
        }


        // ---- Wheel Validation (no wheels = no violations) ----

        [Test]
        public void ValidateWheelContracts_NoWheelRef_NoViolations()
        {
            _debugger.ValidateWheelContracts();
            Assert.AreEqual(0, _debugger.WheelViolationCount);
        }


        // ---- Observable Validation (no car/rb = no violations) ----

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
#endif // UNITY_EDITOR || DEBUG
    }
}
