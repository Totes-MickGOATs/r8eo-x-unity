using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for chassis suspension and traction tuning setters on RCCar.
    /// Steering, crash, CoM, and mass tests live in TuningChassisOtherTests.cs.
    /// </summary>
    public class TuningChassisSuspensionTests
    {
        const float k_Epsilon = 0.001f;

        [Test]
        public void SetSuspension_PushesSpringStrengthToAllWheels()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            float newSpring = 120f;
            float newDamping = 6.5f;
            car.SetSuspension(newSpring, newDamping);

            var wheels = car.GetAllWheels();
            Assert.IsNotNull(wheels);
            Assert.Greater(wheels.Length, 0);

            foreach (var w in wheels)
            {
                Assert.AreEqual(newSpring, w.SpringStrength, k_Epsilon,
                    $"Wheel {w.name} spring strength not updated");
                Assert.AreEqual(newDamping, w.SpringDamping, k_Epsilon,
                    $"Wheel {w.name} spring damping not updated");
            }

            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetSuspension_UpdatesRCCarProperties()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);
            car.SetSuspension(100f, 5f);
            Assert.AreEqual(100f, car.FrontSpringStrength, k_Epsilon);
            Assert.AreEqual(5f, car.FrontSpringDamping, k_Epsilon);
            Assert.AreEqual(100f, car.RearSpringStrength, k_Epsilon);
            Assert.AreEqual(5f, car.RearSpringDamping, k_Epsilon);
            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetAxleSuspension_UpdatesPerAxleProperties()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);
            car.SetAxleSuspension(700f, 41f, 350f, 29f);
            Assert.AreEqual(700f, car.FrontSpringStrength, k_Epsilon);
            Assert.AreEqual(41f, car.FrontSpringDamping, k_Epsilon);
            Assert.AreEqual(350f, car.RearSpringStrength, k_Epsilon);
            Assert.AreEqual(29f, car.RearSpringDamping, k_Epsilon);
            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetAxleSuspension_PushesCorrectValuesToFrontAndRearWheels()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            car.SetAxleSuspension(700f, 41f, 350f, 29f);

            var wheels = car.GetAllWheels();
            Assert.IsNotNull(wheels);
            Assert.Greater(wheels.Length, 0);

            foreach (var w in wheels)
            {
                bool isFront = w.transform.localPosition.z > 0f;
                float expectedK = isFront ? 700f : 350f;
                float expectedDamp = isFront ? 41f : 29f;
                Assert.AreEqual(expectedK, w.SpringStrength, k_Epsilon,
                    $"Wheel {w.name} spring strength should be {expectedK}");
                Assert.AreEqual(expectedDamp, w.SpringDamping, k_Epsilon,
                    $"Wheel {w.name} damping should be {expectedDamp}");
            }

            TestVehicleFactory.DestroyTestCar(car);
        }

        [Test]
        public void SetTraction_PushesGripCoeffToAllWheels()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            float newGrip = 0.9f;
            car.SetTraction(newGrip);

            var wheels = car.GetAllWheels();
            Assert.IsNotNull(wheels);

            foreach (var w in wheels)
            {
                Assert.AreEqual(newGrip, w.GripCoeff, k_Epsilon,
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
    }
}
