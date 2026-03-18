using System.Collections;
using UnityEngine;

namespace R8EOX.Tests.PlayMode.Helpers
{
    /// <summary>
    /// Shared setup/teardown and utility methods for VehicleIntegration* test classes.
    /// Creates a minimal flat-ground scene with a 4-wheel RC buggy wired via components.
    /// No RCInput is added — tests manipulate wheels and Rigidbody directly.
    /// </summary>
    public class VehicleIntegrationHelper
    {
        // ---- Timing constants ----
        public const int k_SettleFrames = 120; // 1 second at 120 Hz
        public const int k_DriveFrames  = 60;

        // ---- Suspension rest-length constants ----
        // front: restDist(0.25) - sag(0.4×15×9.81/2/700=0.042) = 0.208 m (B6.4 red spring)
        // rear : restDist(0.25) - sag(0.6×15×9.81/2/350=0.126) = 0.124 m (B6.4 gray spring)
        public const float k_FrontRestLen   = 0.208f;
        public const float k_RearRestLen    = 0.124f;
        public const float k_RestTolerance  = 0.06f;

        // ---- Layer assignments ----
        public const int k_CarLayer    = 8;
        public const int k_GroundLayer = 9;

        // ---- Scene objects (public so test classes can read them) ----
        public GameObject Ground  { get; private set; }
        public GameObject Car     { get; private set; }
        public Rigidbody  CarRb   { get; private set; }
        public R8EOX.Vehicle.RCCar RcCar { get; private set; }
        public R8EOX.Vehicle.RaycastWheel[] Wheels { get; private set; }

        /// <summary>Call from [SetUp] in each test class.</summary>
        public void SetUp()
        {
            // Flat ground
            Ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Ground.name = "Ground";
            Ground.transform.position = Vector3.zero;
            Ground.transform.localScale = new Vector3(50f, 0.1f, 50f);
            Ground.layer = k_GroundLayer;

            // Car root
            Car = new GameObject("TestCar");
            Car.layer = k_CarLayer;
            Car.transform.position = new Vector3(0f, 0.5f, 0f);

            // Body collider
            var bodyCollider = Car.AddComponent<BoxCollider>();
            bodyCollider.size   = new Vector3(0.30f, 0.10f, 0.45f);
            bodyCollider.center = Vector3.zero;

            // RCCar (RequireComponent adds Rigidbody automatically)
            RcCar = Car.AddComponent<R8EOX.Vehicle.RCCar>();
            CarRb = Car.GetComponent<Rigidbody>();

            // Drivetrain child
            var drivetrainObj = new GameObject("Drivetrain");
            drivetrainObj.transform.SetParent(Car.transform, false);
            drivetrainObj.AddComponent<R8EOX.Vehicle.Drivetrain>();

            // Four wheels
            CreateWheel("WheelFL", new Vector3(-0.15f, 0f,  0.15f), isSteer: true,  isMotor: false);
            CreateWheel("WheelFR", new Vector3( 0.15f, 0f,  0.15f), isSteer: true,  isMotor: false);
            CreateWheel("WheelRL", new Vector3(-0.15f, 0f, -0.15f), isSteer: false, isMotor: true);
            CreateWheel("WheelRR", new Vector3( 0.15f, 0f, -0.15f), isSteer: false, isMotor: true);

            Wheels = Car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>();

            // Exclude car layer from ground raycasts
            LayerMask groundMask = ~(1 << k_CarLayer);
            foreach (var w in Wheels)
                w.GroundMask = groundMask;
        }

        /// <summary>Call from [TearDown] in each test class.</summary>
        public void TearDown()
        {
            if (Car    != null) Object.DestroyImmediate(Car);
            if (Ground != null) Object.DestroyImmediate(Ground);
        }

        /// <summary>Yields one WaitForFixedUpdate per frame for <paramref name="count"/> frames.</summary>
        public static IEnumerator WaitPhysicsFrames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }

        // ---- Private ----

        private void CreateWheel(string name, Vector3 localPos, bool isSteer, bool isMotor)
        {
            var wheelObj = new GameObject(name);
            wheelObj.transform.SetParent(Car.transform, false);
            wheelObj.transform.localPosition = localPos;
            wheelObj.layer = k_CarLayer;

            var wheel = wheelObj.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            wheel.IsSteer = isSteer;
            wheel.IsMotor = isMotor;
        }
    }
}
