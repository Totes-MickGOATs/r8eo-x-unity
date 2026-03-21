using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for TelemetryHudRenderer — pure drawing helpers extracted from TelemetryHUD.
    /// </summary>
    [TestFixture]
    public class TelemetryHudRendererTests
    {
        private GameObject _carGo;
        private RCCar _car;

        [SetUp]
        public void SetUp()
        {
            _carGo = new GameObject("TestCar");
            _carGo.AddComponent<Rigidbody>();
            _car = _carGo.AddComponent<RCCar>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_carGo != null)
                Object.DestroyImmediate(_carGo);
        }

        // ---- GetVehicleLines ----

        [Test]
        public void GetVehicleLines_WithCar_ReturnsSixLines()
        {
            var rb = _car.GetComponent<Rigidbody>();
            var lines = TelemetryHudRenderer.GetVehicleLines(_car, rb);

            // Speed, FwdSpeed, Throttle/Engine, Brake/Reverse, Steering, State/Tumble
            Assert.AreEqual(6, lines.Length);
        }

        [Test]
        public void GetVehicleLines_SpeedLine_ContainsKmh()
        {
            var rb = _car.GetComponent<Rigidbody>();
            var lines = TelemetryHudRenderer.GetVehicleLines(_car, rb);

            StringAssert.Contains("km/h", lines[0]);
        }

        [Test]
        public void GetVehicleLines_ThrottleLine_ContainsEngine()
        {
            var rb = _car.GetComponent<Rigidbody>();
            var lines = TelemetryHudRenderer.GetVehicleLines(_car, rb);

            StringAssert.Contains("Engine", lines[2]);
        }

        [Test]
        public void GetVehicleLines_StateLine_ContainsGrounded()
        {
            var rb = _car.GetComponent<Rigidbody>();
            var lines = TelemetryHudRenderer.GetVehicleLines(_car, rb);

            // Default RCCar is not airborne → should show GROUNDED
            StringAssert.Contains("GROUNDED", lines[5]);
        }

        // ---- GetWheelLines ----

        [Test]
        public void GetWheelLines_NullWheels_ReturnsEmpty()
        {
            var lines = TelemetryHudRenderer.GetWheelLines(null);

            Assert.IsNotNull(lines);
            Assert.AreEqual(0, lines.Length);
        }

        [Test]
        public void GetWheelLines_WithWheelArray_ReturnsOneLinePerWheel()
        {
            var wheelGo1 = new GameObject("FL");
            var w1 = wheelGo1.AddComponent<RaycastWheel>();
            var wheelGo2 = new GameObject("FR");
            var w2 = wheelGo2.AddComponent<RaycastWheel>();
            var wheels = new[] { w1, w2 };

            try
            {
                var lines = TelemetryHudRenderer.GetWheelLines(wheels);
                Assert.AreEqual(2, lines.Length);
            }
            finally
            {
                Object.DestroyImmediate(wheelGo1);
                Object.DestroyImmediate(wheelGo2);
            }
        }
    }
}
