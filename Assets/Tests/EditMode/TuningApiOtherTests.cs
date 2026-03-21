using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for RCCar misc tuning API: SetCrashParams, SetCentreOfMass, SetMass.
    /// Suspension and traction tests live in TuningApiSuspensionTests.cs.
    /// </summary>
    public class TuningApiOtherTests
    {
        const float k_Epsilon = 0.001f;

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
