using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Regression tests for <see cref="DebugBootstrap.AttachTo"/>.
    /// Validates that the bootstrapper attaches the correct set of debug components
    /// to a car GameObject, never duplicates them, and does not throw when no RCCar
    /// is present in the scene.
    ///
    /// Tests exercise the outcome state rather than the private
    /// <c>[RuntimeInitializeOnLoadMethod]</c> entry-point directly.
    /// </summary>
    [TestFixture]
    public class DebugBootstrapTests
    {
#if UNITY_EDITOR || DEBUG
        // ---- Helpers ----

        private GameObject _carGo;

        [SetUp]
        public void SetUp()
        {
            _carGo = new GameObject("DebugBootstrapTestCar");
            _carGo.AddComponent<Rigidbody>();
            _carGo.AddComponent<RCCar>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_carGo != null)
                Object.DestroyImmediate(_carGo);
        }


        // ---- Attachment Tests ----

        [Test]
        public void Attach_WhenRCCarExists_AddsContractDebugger()
        {
            DebugBootstrap.AttachTo(_carGo);

            Assert.IsNotNull(_carGo.GetComponent<ContractDebugger>(),
                "AttachTo should add a ContractDebugger to the RCCar GameObject");
        }

        [Test]
        public void Attach_WhenRCCarExists_AddsWheelTerrainDiagnostics()
        {
            DebugBootstrap.AttachTo(_carGo);

            Assert.IsNotNull(_carGo.GetComponent<WheelTerrainDiagnostics>(),
                "AttachTo should add WheelTerrainDiagnostics to the RCCar GameObject");
        }

        [Test]
        public void Attach_WhenComponentsAlreadyPresent_DoesNotDuplicate()
        {
            // First call — adds all components
            DebugBootstrap.AttachTo(_carGo);
            // Second call — must be idempotent
            DebugBootstrap.AttachTo(_carGo);

            Assert.AreEqual(1, _carGo.GetComponents<ContractDebugger>().Length,
                "ContractDebugger must not be duplicated on a second AttachTo call");
            Assert.AreEqual(1, _carGo.GetComponents<WheelTerrainDiagnostics>().Length,
                "WheelTerrainDiagnostics must not be duplicated on a second AttachTo call");
        }

        [Test]
        public void Attach_WhenNoRCCar_DoesNotThrow()
        {
            // Passing null must not throw — bootstrapper guard handles this gracefully
            Assert.DoesNotThrow(() => DebugBootstrap.AttachTo(null),
                "AttachTo(null) should not throw");
        }

        [Test]
        public void Attach_WhenRCCarExists_LogsBootstrapMessage()
        {
            // Expect the [DebugBootstrap] log emitted by AttachTo — verify the line is reachable
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(
                @"\[DebugBootstrap\] Attached \d+ debug components to "));

            DebugBootstrap.AttachTo(_carGo);
        }

#endif // UNITY_EDITOR || DEBUG
    }
}
