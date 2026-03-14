using NUnit.Framework;
using UnityEngine;
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


        // ---- Helper Methods ----

        /// <summary>Creates a minimal RCCar hierarchy with wheels for testing.</summary>
        private static RCCar CreateTestCar(int wheelCount = 4)
        {
            var root = new GameObject("TestCar");
            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var car = root.AddComponent<RCCar>();

            // Create wheel children: first half are front (z > 0), second half rear (z <= 0)
            for (int i = 0; i < wheelCount; i++)
            {
                var wheelGo = new GameObject($"Wheel_{i}");
                wheelGo.transform.parent = root.transform;
                bool isFront = i < wheelCount / 2;
                wheelGo.transform.localPosition = new Vector3(
                    i % 2 == 0 ? -0.1f : 0.1f,
                    0f,
                    isFront ? 0.15f : -0.15f);
                wheelGo.AddComponent<RaycastWheel>();
            }

            return car;
        }

        /// <summary>Destroys the test car GameObject after the test.</summary>
        private static void DestroyTestCar(RCCar car)
        {
            Object.DestroyImmediate(car.gameObject);
        }

        /// <summary>
        /// Initialises RCCar by calling its Awake and Start methods via reflection-free approach.
        /// We call the public API after manual wheel discovery.
        /// </summary>
        private static void InitialiseCar(RCCar car)
        {
            // Trigger Awake + Start by enabling the component in edit mode
            // In edit mode, we manually invoke the lifecycle
            car.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
            car.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }


        // ---- SetMotorParams Tests ----

        [Test]
        public void SetMotorParams_UpdatesEngineForce_ReturnsNewValue()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SetMotorParams(50f, 40f, 30f, 20f, 5f);

            Assert.AreEqual(50f, car.EngineForceMax, k_Epsilon);
            Assert.AreEqual(40f, car.MaxSpeed, k_Epsilon);
            Assert.AreEqual(30f, car.BrakeForce, k_Epsilon);
            Assert.AreEqual(20f, car.ReverseForce, k_Epsilon);
            Assert.AreEqual(5f, car.CoastDrag, k_Epsilon);
            Assert.AreEqual(RCCar.MotorPreset.Custom, car.ActiveMotorPreset);

            DestroyTestCar(car);
        }

        [Test]
        public void SetMotorParams_SetsPresetToCustom()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            // Initially should be 13.5T
            Assert.AreEqual(RCCar.MotorPreset.Motor13_5T, car.ActiveMotorPreset);

            car.SetMotorParams(10f, 10f, 10f, 10f, 1f);

            Assert.AreEqual(RCCar.MotorPreset.Custom, car.ActiveMotorPreset);

            DestroyTestCar(car);
        }


        // ---- SetSuspension Tests ----

        [Test]
        public void SetSuspension_PushesSpringStrengthToAllWheels()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

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

            DestroyTestCar(car);
        }

        [Test]
        public void SetSuspension_UpdatesRCCarProperties()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SetSuspension(100f, 5f);

            Assert.AreEqual(100f, car.SpringStrength, k_Epsilon);
            Assert.AreEqual(5f, car.SpringDamping, k_Epsilon);

            DestroyTestCar(car);
        }


        // ---- SetTraction Tests ----

        [Test]
        public void SetTraction_PushesGripCoeffToAllWheels()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            float newGrip = 0.9f;
            car.SetTraction(newGrip);

            var wheels = car.GetAllWheels();
            Assert.IsNotNull(wheels);

            foreach (var w in wheels)
            {
                Assert.AreEqual(newGrip, w.GripCoeff, k_Epsilon,
                    $"Wheel {w.name} grip coefficient not updated");
            }

            DestroyTestCar(car);
        }

        [Test]
        public void SetTraction_UpdatesRCCarProperty()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SetTraction(0.85f);

            Assert.AreEqual(0.85f, car.GripCoeff, k_Epsilon);

            DestroyTestCar(car);
        }


        // ---- SetThrottleResponse Tests ----

        [Test]
        public void SetThrottleResponse_UpdatesRampRates()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SetThrottleResponse(8f, 15f);

            Assert.AreEqual(8f, car.ThrottleRampUp, k_Epsilon);
            Assert.AreEqual(15f, car.ThrottleRampDown, k_Epsilon);

            DestroyTestCar(car);
        }


        // ---- SetSteeringParams Tests ----

        [Test]
        public void SetSteeringParams_UpdatesAllSteeringValues()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SetSteeringParams(0.6f, 8f, 10f, 0.3f);

            Assert.AreEqual(0.6f, car.SteeringMax, k_Epsilon);
            Assert.AreEqual(8f, car.SteeringSpeed, k_Epsilon);
            Assert.AreEqual(10f, car.SteeringSpeedLimit, k_Epsilon);
            Assert.AreEqual(0.3f, car.SteeringHighSpeedFactor, k_Epsilon);

            DestroyTestCar(car);
        }


        // ---- SetCrashParams Tests ----

        [Test]
        public void SetCrashParams_UpdatesTumbleValues()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SetCrashParams(45f, 65f, 0.4f, 0.25f);

            Assert.AreEqual(45f, car.TumbleEngageDeg, k_Epsilon);
            Assert.AreEqual(65f, car.TumbleFullDeg, k_Epsilon);
            Assert.AreEqual(0.4f, car.TumbleBounce, k_Epsilon);
            Assert.AreEqual(0.25f, car.TumbleFriction, k_Epsilon);

            DestroyTestCar(car);
        }


        // ---- SetCentreOfMass Tests ----

        [Test]
        public void SetCentreOfMass_UpdatesComGroundY()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SetCentreOfMass(-0.15f);

            Assert.AreEqual(-0.15f, car.ComGroundY, k_Epsilon);

            DestroyTestCar(car);
        }


        // ---- SetMass Tests ----

        [Test]
        public void SetMass_UpdatesRigidbodyMass()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SetMass(2.5f);

            Assert.AreEqual(2.5f, car.Mass, k_Epsilon);

            DestroyTestCar(car);
        }


        // ---- SelectMotorPreset Tests ----

        [Test]
        public void SelectMotorPreset_AppliesPresetValues()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SelectMotorPreset(RCCar.MotorPreset.Motor21_5T);

            Assert.AreEqual(RCCar.MotorPreset.Motor21_5T, car.ActiveMotorPreset);
            Assert.AreEqual(15.5f, car.EngineForceMax, k_Epsilon);
            Assert.AreEqual(13f, car.MaxSpeed, k_Epsilon);

            DestroyTestCar(car);
        }
    }
}
