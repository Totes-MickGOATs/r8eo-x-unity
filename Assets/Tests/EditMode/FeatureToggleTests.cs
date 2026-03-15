using NUnit.Framework;
using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for Inspector-controllable feature toggles on RCAirPhysics and RCCar.
    /// Verifies that disabling air physics skips torque application and
    /// disabling tumble physics forces tumble factor to zero.
    /// </summary>
    public class FeatureToggleTests
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

        /// <summary>Creates an RCAirPhysics component on a test hierarchy.</summary>
        private static RCAirPhysics CreateTestAirPhysics()
        {
            var root = new GameObject("TestCar");
            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;

            var airGo = new GameObject("AirPhysics");
            airGo.transform.parent = root.transform;
            var airPhysics = airGo.AddComponent<RCAirPhysics>();

            // Add wheels so Apply doesn't early-return due to no wheels
            for (int i = 0; i < 4; i++)
            {
                var wheelGo = new GameObject($"Wheel_{i}");
                wheelGo.transform.parent = root.transform;
                wheelGo.AddComponent<RaycastWheel>();
            }

            return airPhysics;
        }

        private static void InitialiseCar(RCCar car)
        {
            car.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
            car.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        private static void InitialiseAirPhysics(RCAirPhysics airPhysics)
        {
            airPhysics.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        private static void DestroyTestObject(Component comp)
        {
            Object.DestroyImmediate(comp.transform.root.gameObject);
        }


        // ---- RCAirPhysics Toggle Tests ----

        [Test]
        public void EnableAirPhysics_DefaultsToTrue()
        {
            var airPhysics = CreateTestAirPhysics();

            Assert.IsTrue(airPhysics.IsEnabled,
                "Air physics should be enabled by default");

            DestroyTestObject(airPhysics);
        }

        [Test]
        public void Apply_WhenDisabled_DoesNotApplyTorque()
        {
            var airPhysics = CreateTestAirPhysics();
            InitialiseAirPhysics(airPhysics);

            var rb = airPhysics.GetComponentInParent<Rigidbody>();
            rb.angularVelocity = Vector3.zero;

            airPhysics.IsEnabled = false;
            airPhysics.Apply(0.02f, 1f, 0f, 0f);

            // Angular velocity should remain zero — no torque was applied
            Assert.AreEqual(Vector3.zero, rb.angularVelocity,
                "Disabling air physics should prevent torque application");

            DestroyTestObject(airPhysics);
        }

        [Test]
        public void Apply_WhenEnabled_RunsNormally()
        {
            var airPhysics = CreateTestAirPhysics();
            InitialiseAirPhysics(airPhysics);

            airPhysics.IsEnabled = true;

            // Should not throw — runs the normal code path
            Assert.DoesNotThrow(() => airPhysics.Apply(0.02f, 1f, 0f, 0f),
                "Enabled air physics should run Apply without error");

            DestroyTestObject(airPhysics);
        }


        // ---- RCCar Tumble Toggle Tests ----

        [Test]
        public void EnableTumblePhysics_DefaultsToTrue()
        {
            var car = CreateTestCar();

            Assert.IsTrue(car.EnableTumblePhysics,
                "Tumble physics should be enabled by default");

            DestroyTestObject(car);
        }

        [Test]
        public void TumbleFactor_WhenTumbleDisabled_AlwaysZero()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.EnableTumblePhysics = false;

            // Tilt the car to a tumble angle (beyond 70 degrees)
            car.transform.rotation = Quaternion.Euler(0f, 0f, 80f);

            // Force a physics tick to recalculate tumble
            car.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.AreEqual(0f, car.TumbleFactor, k_Epsilon,
                "Tumble factor should be zero when tumble physics is disabled");

            DestroyTestObject(car);
        }

        [Test]
        public void TumbleFactor_WhenTumbleEnabled_ComputesNormally()
        {
            var car = CreateTestCar();
            InitialiseCar(car);

            car.EnableTumblePhysics = true;

            // Tilt the car beyond full tumble angle
            car.transform.rotation = Quaternion.Euler(0f, 0f, 80f);

            // Force a physics tick
            car.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);

            Assert.Greater(car.TumbleFactor, 0f,
                "Tumble factor should be computed when tumble physics is enabled");

            DestroyTestObject(car);
        }
    }
}
