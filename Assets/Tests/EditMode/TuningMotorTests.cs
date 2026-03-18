using NUnit.Framework;
using UnityEngine;
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


        // ---- SelectMotorPreset Tests ----

        [Test]
        public void SelectMotorPreset_AppliesPresetValues()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.SelectMotorPreset(RCCar.MotorPreset.Motor21_5T);

            Assert.AreEqual(RCCar.MotorPreset.Motor21_5T, car.ActiveMotorPreset);
            Assert.AreEqual(155f, car.EngineForceMax, k_Epsilon);
            Assert.AreEqual(13f, car.MaxSpeed, k_Epsilon);

            DestroyTestCar(car);
        }
    }
}
