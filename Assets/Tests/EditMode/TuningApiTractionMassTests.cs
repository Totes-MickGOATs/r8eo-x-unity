using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for traction, crash, centre of mass, and mass tuning setters.
    /// </summary>
    public class TuningApiTractionMassTests
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
