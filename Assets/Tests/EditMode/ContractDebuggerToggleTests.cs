using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for ContractDebugger toggle flags and counter reset.
    /// Validation and SetTarget tests live in ContractDebuggerValidationTests.cs.
    /// </summary>
    [TestFixture]
    public class ContractDebuggerToggleTests
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

        [Test]
        public void ResetCounters_AllCountersZero()
        {
            _debugger.ResetCounters();
            Assert.AreEqual(0, _debugger.InputViolationCount);
            Assert.AreEqual(0, _debugger.VehicleViolationCount);
            Assert.AreEqual(0, _debugger.WheelViolationCount);
            Assert.AreEqual(0, _debugger.ObservableViolationCount);
        }
#endif // UNITY_EDITOR || DEBUG
    }
}
