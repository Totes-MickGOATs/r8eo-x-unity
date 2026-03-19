using NUnit.Framework;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the runtime tuning setter API on RCCar.
    /// Verifies that setter methods correctly update internal fields
    /// and push values to child wheel components.
    /// </summary>
    public class TuningApiTests
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


        // ---- SetSuspension Tests ----

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


        // ---- SetAxleSuspension Tests ----

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
                    $"Wheel {w.name} spring strength should be {expectedK} for {(isFront ? "front" : "rear")} axle");
                Assert.AreEqual(expectedDamp, w.SpringDamping, k_Epsilon,
                    $"Wheel {w.name} damping should be {expectedDamp} for {(isFront ? "front" : "rear")} axle");
            }

            TestVehicleFactory.DestroyTestCar(car);
        }


        // ---- SetTraction Tests ----

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


        // ---- SetSteeringParams Tests ----

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


        // ---- SetCrashParams Tests ----

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


        // ---- SetCentreOfMass Tests ----

        [Test]
        public void SetCentreOfMass_UpdatesComGroundY()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            car.SetCentreOfMass(-0.15f);

            Assert.AreEqual(-0.15f, car.ComGroundY, k_Epsilon);

            TestVehicleFactory.DestroyTestCar(car);
        }


        // ---- SetMass Tests ----

        [Test]
        public void SetMass_UpdatesRigidbodyMass()
        {
            var car = TestVehicleFactory.CreateTestCar();
            TestVehicleFactory.InitialiseCar(car);

            car.SetMass(2.5f);

            Assert.AreEqual(2.5f, car.Mass, k_Epsilon);

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
