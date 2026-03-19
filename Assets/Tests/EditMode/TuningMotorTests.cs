using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for motor-related tuning setters on RCCar:
    /// SetMotorParams, SetThrottleResponse, and SelectMotorPreset.
    /// </summary>
    public class TuningMotorTests
    {
        // ---- Constants ----

        const float k_Epsilon = 0.001f;


        // ---- SetMotorParams Tests ----

        [Test]
        public void SetMotorParams_UpdatesEngineForce_ReturnsNewValue()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            car.SetMotorParams(50f, 40f, 30f, 20f, 5f);

            Assert.AreEqual(50f, car.EngineForceMax, k_Epsilon);
            Assert.AreEqual(40f, car.MaxSpeed, k_Epsilon);
            Assert.AreEqual(30f, car.BrakeForce, k_Epsilon);
            Assert.AreEqual(20f, car.ReverseForce, k_Epsilon);
            Assert.AreEqual(5f, car.CoastDrag, k_Epsilon);
            Assert.AreEqual(RCCar.MotorPreset.Custom, car.ActiveMotorPreset);

            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetMotorParams_SetsPresetToCustom()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            // Initially should be 13.5T
            Assert.AreEqual(RCCar.MotorPreset.Motor13_5T, car.ActiveMotorPreset);

            car.SetMotorParams(10f, 10f, 10f, 10f, 1f);

            Assert.AreEqual(RCCar.MotorPreset.Custom, car.ActiveMotorPreset);

            TestVehicleFactory.DestroyTestCar(car);
        }


        // ---- SetThrottleResponse Tests ----

        [Test]
        public void SetThrottleResponse_UpdatesRampRates()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            car.SetThrottleResponse(8f, 15f);

            Assert.AreEqual(8f, car.ThrottleRampUp, k_Epsilon);
            Assert.AreEqual(15f, car.ThrottleRampDown, k_Epsilon);

            TestVehicleFactory.DestroyTestCar(car);
        }


        // ---- SelectMotorPreset Tests ----

        [Test]
        public void SelectMotorPreset_AppliesPresetValues()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            car.SelectMotorPreset(RCCar.MotorPreset.Motor21_5T);

            Assert.AreEqual(RCCar.MotorPreset.Motor21_5T, car.ActiveMotorPreset);
            Assert.AreEqual(155f, car.EngineForceMax, k_Epsilon);
            Assert.AreEqual(13f, car.MaxSpeed, k_Epsilon);

            TestVehicleFactory.DestroyTestCar(car);
        }
    }
}
