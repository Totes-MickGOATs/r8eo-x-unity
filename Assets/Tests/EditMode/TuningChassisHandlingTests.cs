using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Chassis handling tuning tests: SetTraction, SetSteeringParams, SetCrashParams,
    /// SetCentreOfMass, SetMass.
    /// </summary>
    public class TuningChassisHandlingTests
    {
        const float k_Epsilon = 0.001f;

        [Test]
        public void SetTraction_PushesGripCoeffToAllWheels()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            car.SetTraction(0.9f);

            var wheels = car.GetAllWheels();
            Assert.IsNotNull(wheels);

            foreach (var w in wheels)
            {
                Assert.AreEqual(0.9f, w.GripCoeff, k_Epsilon,
                    $"Wheel {w.name} grip coefficient not updated");
            }

            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetTraction_UpdatesRCCarProperty()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);
            car.SetTraction(0.85f);
            Assert.AreEqual(0.85f, car.GripCoeff, k_Epsilon);
            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetSteeringParams_UpdatesAllSteeringValues()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);
            car.SetSteeringParams(0.6f, 8f, 10f, 0.3f);
            Assert.AreEqual(0.6f, car.SteeringMax, k_Epsilon);
            Assert.AreEqual(8f, car.SteeringSpeed, k_Epsilon);
            Assert.AreEqual(10f, car.SteeringSpeedLimit, k_Epsilon);
            Assert.AreEqual(0.3f, car.SteeringHighSpeedFactor, k_Epsilon);
            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetCrashParams_UpdatesTumbleValues()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);
            car.SetCrashParams(45f, 65f, 0.4f, 0.25f);
            Assert.AreEqual(45f, car.TumbleEngageDeg, k_Epsilon);
            Assert.AreEqual(65f, car.TumbleFullDeg, k_Epsilon);
            Assert.AreEqual(0.4f, car.TumbleBounce, k_Epsilon);
            Assert.AreEqual(0.25f, car.TumbleFriction, k_Epsilon);
            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetCentreOfMass_UpdatesComGroundY()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);
            car.SetCentreOfMass(-0.15f);
            Assert.AreEqual(-0.15f, car.ComGroundY, k_Epsilon);
            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetMass_UpdatesRigidbodyMass()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);
            car.SetMass(2.5f);
            Assert.AreEqual(2.5f, car.Mass, k_Epsilon);
            TestVehicleFactory.DestroyTestCar(car);
        }
    }
}
