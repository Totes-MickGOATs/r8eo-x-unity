using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    public static class TestVehicleFactory
    {
        /// <summary>Creates a minimal RCCar hierarchy with wheels for testing.</summary>
        public static RCCar CreateTestCar(int wheelCount = 4)
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
        public static void DestroyTestCar(RCCar car)
        {
            Object.DestroyImmediate(car.gameObject);
        }

        /// <summary>
        /// Initialises RCCar by calling its Awake and Start methods via reflection-free approach.
        /// We call the public API after manual wheel discovery.
        /// </summary>
        public static void InitialiseCar(RCCar car)
        {
            // Trigger Awake + Start by enabling the component in edit mode
            // In edit mode, we manually invoke the lifecycle
            car.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
            car.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }
    }
}
