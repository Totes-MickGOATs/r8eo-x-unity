using UnityEngine;

namespace R8EOX.Tests.PlayMode.Helpers
{
    /// <summary>
    /// Programmatic test environment factory for physics conformance tests.
    /// Creates a minimal flat-ground scene with an RC car built from components
    /// (no prefab dependency). All physics parameters match the reference constants
    /// from adr-001-physics-model.md.
    /// </summary>
    public static class ConformanceSceneSetup
    {
        // ---- RC Car Reference Constants ----

        /// <summary>Vehicle mass in kg (1/10 scale RC buggy).</summary>
        public const float k_Mass = 1.5f;
        /// <summary>Wheel radius in metres.</summary>
        public const float k_WheelRadius = 0.166f;
        /// <summary>Wheelbase (front-to-rear axle) in metres.</summary>
        public const float k_Wheelbase = 0.28f;
        /// <summary>Half-track width (center-to-wheel) in metres.</summary>
        public const float k_HalfTrack = 0.15f;
        /// <summary>Gravity magnitude in m/s^2.</summary>
        public const float k_Gravity = 9.81f;
        /// <summary>Default grip coefficient.</summary>
        public const float k_GripCoeff = 0.7f;

        // ---- Layer Assignments ----

        /// <summary>Layer index for the car (avoids self-raycast).</summary>
        public const int k_CarLayer = 8;
        /// <summary>Layer index for the ground plane.</summary>
        public const int k_GroundLayer = 9;

        // ---- Ground Plane ----

        /// <summary>Ground plane half-extent in metres.</summary>
        public const float k_GroundHalfExtent = 50f;
        /// <summary>Ground plane thickness in metres.</summary>
        public const float k_GroundThickness = 0.1f;

        /// <summary>
        /// Creates a flat ground plane at y=0.
        /// Returns the ground GameObject for cleanup in TearDown.
        /// </summary>
        public static GameObject CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "ConformanceGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(
                k_GroundHalfExtent * 2f, k_GroundThickness, k_GroundHalfExtent * 2f);
            ground.layer = k_GroundLayer;
            return ground;
        }

        /// <summary>
        /// Creates a fully-wired RC buggy at the given spawn position.
        /// Includes: Rigidbody, RCCar, Drivetrain, 4x RaycastWheel, body collider.
        /// No RCInput is added (input is null; tests manipulate wheels directly).
        /// Returns the root car GameObject.
        /// </summary>
        public static GameObject CreateTestVehicle(Vector3 spawnPosition)
        {
            // Root with body collider
            var car = new GameObject("TestCar_Conformance");
            car.layer = k_CarLayer;
            car.transform.position = spawnPosition;

            var bodyCollider = car.AddComponent<BoxCollider>();
            bodyCollider.size = new Vector3(0.30f, 0.10f, 0.45f);
            bodyCollider.center = Vector3.zero;

            // RCCar (will get Rigidbody via RequireComponent)
            car.AddComponent<R8EOX.Vehicle.RCCar>();

            // Drivetrain child
            var drivetrainObj = new GameObject("Drivetrain");
            drivetrainObj.transform.SetParent(car.transform, false);
            drivetrainObj.AddComponent<R8EOX.Vehicle.Drivetrain>();

            // AirPhysics child
            var airPhysicsObj = new GameObject("AirPhysics");
            airPhysicsObj.transform.SetParent(car.transform, false);
            airPhysicsObj.AddComponent<R8EOX.Vehicle.RCAirPhysics>();

            // Four wheels at correct wheelbase/track positions
            float halfWheelbase = k_Wheelbase * 0.5f;
            CreateWheel(car, "WheelFL", new Vector3(-k_HalfTrack, 0f, halfWheelbase), isSteer: true, isMotor: false);
            CreateWheel(car, "WheelFR", new Vector3(k_HalfTrack, 0f, halfWheelbase), isSteer: true, isMotor: false);
            CreateWheel(car, "WheelRL", new Vector3(-k_HalfTrack, 0f, -halfWheelbase), isSteer: false, isMotor: true);
            CreateWheel(car, "WheelRR", new Vector3(k_HalfTrack, 0f, -halfWheelbase), isSteer: false, isMotor: true);

            // Configure ground masks on all wheels
            LayerMask groundMask = ~(1 << k_CarLayer);
            foreach (var w in car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>())
                w.GroundMask = groundMask;

            return car;
        }

        /// <summary>
        /// Creates a single wheel child object under the car root.
        /// </summary>
        private static void CreateWheel(
            GameObject carRoot, string name, Vector3 localPos, bool isSteer, bool isMotor)
        {
            var wheelObj = new GameObject(name);
            wheelObj.transform.SetParent(carRoot.transform, false);
            wheelObj.transform.localPosition = localPos;
            wheelObj.layer = k_CarLayer;

            var wheel = wheelObj.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            wheel.IsSteer = isSteer;
            wheel.IsMotor = isMotor;
        }
    }
}
