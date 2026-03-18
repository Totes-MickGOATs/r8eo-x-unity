using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    public static class TestVehicleFactory
    {
        public static GameObject CreateTestCar()
        {
            var go = new GameObject("TestCar");
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var car = go.AddComponent<RCCar>();

            // Create 4 wheel children
            string[] wheelNames = { "WheelFL", "WheelFR", "WheelRL", "WheelRR" };
            Vector3[] positions = {
                new Vector3(-0.15f, 0f, 0.17f),
                new Vector3(0.15f, 0f, 0.17f),
                new Vector3(-0.15f, 0f, -0.17f),
                new Vector3(0.15f, 0f, -0.17f)
            };

            for (int i = 0; i < 4; i++)
            {
                var wheelGO = new GameObject(wheelNames[i]);
                wheelGO.transform.SetParent(go.transform);
                wheelGO.transform.localPosition = positions[i];
                var wheel = wheelGO.AddComponent<RaycastWheel>();
                wheel.IsMotor = i >= 2; // rear wheels
                wheel.IsSteer = i < 2; // front wheels
            }

            return go;
        }

        public static void DestroyTestCar(GameObject car)
        {
            Object.DestroyImmediate(car);
        }
    }
}
