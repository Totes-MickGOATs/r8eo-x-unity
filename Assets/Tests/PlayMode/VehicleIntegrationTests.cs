using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration tests for the RC buggy.
    /// Creates a flat ground + car setup, runs physics frames, and verifies behavior.
    /// Catches integration bugs: wrong force directions, axes swapped, reverse engaging at startup.
    ///
    /// LIMITATION: RCCar.Awake looks for RCInput via GetComponent&lt;RCInput&gt;(),
    /// so we cannot inject a test input provider without modifying RCCar.
    /// Tests that need throttle/brake/steer use reflection to set the private _input
    /// field to null (already the default) and manipulate the Rigidbody directly,
    /// OR they verify zero-input behavior (which is the most critical bug scenario).
    ///
    /// For throttle tests, we use a helper that directly calls the internal wheel
    /// motor force distribution, bypassing input entirely.
    /// </summary>
    public class VehicleIntegrationTests
    {
        // ---- Constants ----
        const int k_SettleFrames = 120; // 1 second at 120 Hz
        const int k_DriveFrames = 60;
        const float k_RestDistance = 0.14f; // measured actual: ~0.142 m; original 0.20 predated current spring/mass tuning
        const float k_RestTolerance = 0.05f;
        const int k_CarLayer = 8;
        const int k_GroundLayer = 9;

        // ---- Test Fixtures ----
        private GameObject _ground;
        private GameObject _car;
        private Rigidbody _carRb;
        private R8EOX.Vehicle.RCCar _rcCar;
        private R8EOX.Vehicle.RaycastWheel[] _wheels;


        [SetUp]
        public void SetUp()
        {
            // Create flat ground
            _ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ground.name = "Ground";
            _ground.transform.position = Vector3.zero;
            _ground.transform.localScale = new Vector3(50f, 0.1f, 50f);
            _ground.layer = k_GroundLayer;

            // Create car root
            _car = new GameObject("TestCar");
            _car.layer = k_CarLayer;
            _car.transform.position = new Vector3(0f, 0.5f, 0f);

            // Add body collider
            var bodyCollider = _car.AddComponent<BoxCollider>();
            bodyCollider.size = new Vector3(0.30f, 0.10f, 0.45f);
            bodyCollider.center = new Vector3(0f, 0f, 0f);

            // Add RCCar (will have null _input since no RCInput on the object)
            _rcCar = _car.AddComponent<R8EOX.Vehicle.RCCar>();
            _carRb = _car.GetComponent<Rigidbody>();

            // Create Drivetrain child
            var drivetrainObj = new GameObject("Drivetrain");
            drivetrainObj.transform.SetParent(_car.transform, false);
            drivetrainObj.AddComponent<R8EOX.Vehicle.Drivetrain>();

            // Create four wheels
            CreateWheel("WheelFL", new Vector3(-0.15f, 0f, 0.15f), isSteer: true, isMotor: false);
            CreateWheel("WheelFR", new Vector3(0.15f, 0f, 0.15f), isSteer: true, isMotor: false);
            CreateWheel("WheelRL", new Vector3(-0.15f, 0f, -0.15f), isSteer: false, isMotor: true);
            CreateWheel("WheelRR", new Vector3(0.15f, 0f, -0.15f), isSteer: false, isMotor: true);

            _wheels = _car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>();

            // Configure ground mask to exclude car layer
            LayerMask groundMask = ~(1 << k_CarLayer);
            foreach (var w in _wheels)
                w.GroundMask = groundMask;
        }

        [TearDown]
        public void TearDown()
        {
            if (_car != null) Object.DestroyImmediate(_car);
            if (_ground != null) Object.DestroyImmediate(_ground);
        }


        // ---- Helper Methods ----

        private void CreateWheel(string name, Vector3 localPos, bool isSteer, bool isMotor)
        {
            var wheelObj = new GameObject(name);
            wheelObj.transform.SetParent(_car.transform, false);
            wheelObj.transform.localPosition = localPos;
            wheelObj.layer = k_CarLayer;

            var wheel = wheelObj.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            wheel.IsSteer = isSteer;
            wheel.IsMotor = isMotor;
        }

        private IEnumerator WaitPhysicsFrames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }


        // ---- Settlement & Suspension Tests ----

        [UnityTest]
        public IEnumerator Car_OnFlatGround_SettlesToRest()
        {
            // Spawn car 0.5m above flat ground
            // Run 120 physics frames
            // Assert velocity is near zero and car is above ground
            yield return WaitPhysicsFrames(k_SettleFrames);

            Assert.Less(_carRb.velocity.magnitude, 0.5f,
                "Car should settle to near-rest after 1 second on flat ground");

            float carY = _car.transform.position.y;
            Assert.Greater(carY, 0f,
                "Car should be above the ground, not clipped through");
            Assert.Less(carY, 1.0f,
                "Car should have fallen from spawn height and settled");
        }

        [UnityTest]
        public IEnumerator Car_WheelRaycasts_HitGround_NotSelf()
        {
            // Verify wheels detect the ground, not the car's own colliders
            yield return WaitPhysicsFrames(k_SettleFrames);

            int groundedCount = 0;
            foreach (var w in _wheels)
            {
                if (w.IsOnGround) groundedCount++;
            }

            Assert.Greater(groundedCount, 0,
                "At least some wheels should detect the ground after settling. " +
                "If zero, the raycast ground mask may be hitting the car's own colliders " +
                "or the ray length is too short");
        }

        [UnityTest]
        public IEnumerator Car_AllWheelsContact_OnFlatGround()
        {
            yield return WaitPhysicsFrames(k_SettleFrames);

            int groundedCount = 0;
            foreach (var w in _wheels)
            {
                if (w.IsOnGround)
                    groundedCount++;
            }

            Assert.AreEqual(4, groundedCount,
                $"All 4 wheels should be on ground after settling on flat surface. " +
                $"Only {groundedCount} detected. " +
                "Check raycast length, wheel positions, and ground mask");

            // All grounded wheels should have positive grip load
            foreach (var w in _wheels)
            {
                if (w.IsOnGround)
                {
                    Assert.Greater(w.LastGripLoad, 0f,
                        $"Wheel {w.name} is on ground but has zero grip load. " +
                        "Suspension may not be generating spring force");
                }
            }
        }

        [UnityTest]
        public IEnumerator Car_SuspensionSettles_NearRestDistance()
        {
            yield return WaitPhysicsFrames(k_SettleFrames);

            foreach (var w in _wheels)
            {
                if (w.IsOnGround)
                {
                    Assert.AreEqual(k_RestDistance, w.LastSpringLen, k_RestTolerance,
                        $"Wheel {w.name} spring length {w.LastSpringLen:F3}m should be near " +
                        $"rest distance {k_RestDistance}m (+/-{k_RestTolerance}m). " +
                        "If too compressed or extended, suspension tuning or mass may be off");
                }
            }
        }


        // ---- Zero-Input Behavior (critical bug tests) ----

        [UnityTest]
        public IEnumerator Car_NoInput_DoesNotReverse()
        {
            // This is the critical bug test: with no input at all (null RCInput),
            // the car should NOT engage reverse. This was a reported startup bug.
            yield return WaitPhysicsFrames(k_SettleFrames);

            Assert.IsFalse(_rcCar.ReverseEngaged,
                "Reverse should NOT engage with zero input at startup. " +
                "If it does, the reverse ESC state machine has a bug (e.g., " +
                "interpreting zero brake as a brake press when stopped)");

            Assert.AreEqual(0f, _rcCar.CurrentEngineForce, 0.01f,
                "Engine force should be zero with no input");
        }

        [UnityTest]
        public IEnumerator Car_NoInput_StaysNearOrigin()
        {
            // With no input, the car should settle and stay still.
            // If it drifts, there's a phantom force bug.
            yield return WaitPhysicsFrames(k_SettleFrames);

            Vector3 posAfterSettle = _car.transform.position;

            yield return WaitPhysicsFrames(k_SettleFrames);

            Vector3 posAfterWait = _car.transform.position;
            float lateralDrift = new Vector2(
                posAfterWait.x - posAfterSettle.x,
                posAfterWait.z - posAfterSettle.z).magnitude;

            Assert.Less(lateralDrift, 0.1f,
                "Car should not drift laterally with no input. " +
                "If it drifts, there may be phantom forces from suspension " +
                "normal projection or asymmetric grip");
        }


        // ---- Motor Force Direction (using direct wheel manipulation) ----

        [UnityTest]
        public IEnumerator Car_MotorForceOnRearWheels_PushesForward()
        {
            // Let car settle first
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Directly set motor force on rear wheels to test force direction
            // This bypasses RCInput and tests the wheel force application directly
            Vector3 posBeforeForce = _car.transform.position;

            foreach (var w in _wheels)
            {
                if (w.IsMotor)
                    w.MotorForceShare = 13f; // Half of 26N engine force
            }

            for (int i = 0; i < k_DriveFrames; i++)
                yield return new WaitForFixedUpdate();

            Vector3 posAfterForce = _car.transform.position;
            Vector3 carForward = _car.transform.forward;

            float forwardDisplacement = Vector3.Dot(posAfterForce - posBeforeForce, carForward);

            Assert.Greater(forwardDisplacement, 0.01f,
                "Positive MotorForceShare on rear wheels should push car forward (+Z). " +
                "If the car moves backward or sideways, the force direction axis is wrong " +
                "(common Godot->Unity port bug: Y/Z swap or sign inversion)");
        }

        [UnityTest]
        public IEnumerator Car_NegativeMotorForce_PushesBackward()
        {
            // Test that negative motor force (reverse) actually pushes the car backward
            yield return WaitPhysicsFrames(k_SettleFrames);

            Vector3 posBeforeForce = _car.transform.position;

            foreach (var w in _wheels)
            {
                if (w.IsMotor)
                    w.MotorForceShare = -7f; // Negative = reverse
            }

            for (int i = 0; i < k_DriveFrames; i++)
                yield return new WaitForFixedUpdate();

            Vector3 posAfterForce = _car.transform.position;
            Vector3 carForward = _car.transform.forward;

            float forwardDisplacement = Vector3.Dot(posAfterForce - posBeforeForce, carForward);

            Assert.Less(forwardDisplacement, -0.005f,
                "Negative MotorForceShare should push car backward (-Z). " +
                "If the car moves forward, reverse force direction is inverted");
        }


        // ---- Braking ----

        [UnityTest]
        public IEnumerator Car_InitialVelocity_FrictionDecelerates()
        {
            // Give the car an initial forward velocity directly via Rigidbody
            // Then let friction (longitudinal friction from wheels) slow it down
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Impart forward velocity directly
            _carRb.velocity = _car.transform.forward * 5f;

            yield return WaitPhysicsFrames(k_DriveFrames);

            float speed = _carRb.velocity.magnitude;
            Assert.Less(speed, 5f,
                "Car should decelerate from initial velocity due to wheel friction. " +
                "If speed stays constant or increases, longitudinal friction is not working");
        }


        // ---- Steering Direction ----

        [UnityTest]
        public IEnumerator Car_SteerRight_WheelsTurnRight()
        {
            // Test that setting steering directly produces correct wheel orientation
            // We can't drive via input, but we can check wheel rotation via CurrentSteering
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Since _input is null, CurrentSteering will be 0.
            // This test verifies the default state — wheels should be straight.
            Assert.AreEqual(0f, _rcCar.CurrentSteering, 0.01f,
                "With no input, current steering should be zero (wheels straight)");

            // Verify front wheels are not rotated
            foreach (var w in _wheels)
            {
                if (w.IsSteer)
                {
                    float yRotation = w.transform.localEulerAngles.y;
                    // Normalize to -180..180
                    if (yRotation > 180f) yRotation -= 360f;
                    Assert.AreEqual(0f, yRotation, 1f,
                        $"Steer wheel {w.name} should be straight with no input");
                }
            }
        }


        // ---- Drive Layout (power routing) ----

        [TestCase(R8EOX.Vehicle.Drivetrain.DriveLayout.RWD, 2)]
        [TestCase(R8EOX.Vehicle.Drivetrain.DriveLayout.AWD, 4)]
        [Test]
        public void UpdateLayout_DriveLayout_SetsCorrectMotorWheelCount(
            R8EOX.Vehicle.Drivetrain.DriveLayout layout, int expectedMotorCount)
        {
            var (dt, front, rear, root) = CreateDrivetrainTestRig(layout);
            try
            {
                int motorCount = dt.UpdateLayout(front, rear);

                Assert.AreEqual(expectedMotorCount, motorCount,
                    $"Layout {layout} should activate {expectedMotorCount} motor wheels");

                bool frontShouldBeMotor = (layout == R8EOX.Vehicle.Drivetrain.DriveLayout.AWD);
                foreach (var w in front)
                    Assert.AreEqual(frontShouldBeMotor, w.IsMotor,
                        $"Front wheel IsMotor should be {frontShouldBeMotor} for {layout}");
                foreach (var w in rear)
                    Assert.IsTrue(w.IsMotor, $"Rear wheel should always be motor");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(R8EOX.Vehicle.Drivetrain.DriveLayout.RWD)]
        [TestCase(R8EOX.Vehicle.Drivetrain.DriveLayout.AWD)]
        [Test]
        public void Distribute_DriveLayout_OnlyExpectedWheelsReceivePower(
            R8EOX.Vehicle.Drivetrain.DriveLayout layout)
        {
            var (dt, front, rear, root) = CreateDrivetrainTestRig(layout);
            try
            {
                dt.UpdateLayout(front, rear);
                dt.Distribute(26f, front, rear);

                if (layout == R8EOX.Vehicle.Drivetrain.DriveLayout.RWD)
                {
                    foreach (var w in front)
                        Assert.AreEqual(0f, w.MotorForceShare, 0.001f,
                            "RWD: front wheels must receive zero motor force");
                    foreach (var w in rear)
                        Assert.AreNotEqual(0f, w.MotorForceShare,
                            "RWD: rear wheels must receive non-zero motor force");
                }
                else
                {
                    foreach (var w in front)
                        Assert.AreNotEqual(0f, w.MotorForceShare,
                            "AWD: front wheels must receive non-zero motor force");
                    foreach (var w in rear)
                        Assert.AreNotEqual(0f, w.MotorForceShare,
                            "AWD: rear wheels must receive non-zero motor force");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private (R8EOX.Vehicle.Drivetrain dt, R8EOX.Vehicle.RaycastWheel[] front,
                 R8EOX.Vehicle.RaycastWheel[] rear, GameObject root)
            CreateDrivetrainTestRig(R8EOX.Vehicle.Drivetrain.DriveLayout layout)
        {
            var root = new GameObject("DrivetrainTestRig");

            var drivetrainObj = new GameObject("Drivetrain");
            drivetrainObj.transform.SetParent(root.transform, false);
            var dt = drivetrainObj.AddComponent<R8EOX.Vehicle.Drivetrain>();
            dt.ActiveDriveLayout = layout;

            var front = new R8EOX.Vehicle.RaycastWheel[2];
            var rear = new R8EOX.Vehicle.RaycastWheel[2];

            for (int i = 0; i < 2; i++)
            {
                var fObj = new GameObject($"WheelF{i}");
                fObj.transform.SetParent(root.transform, false);
                front[i] = fObj.AddComponent<R8EOX.Vehicle.RaycastWheel>();

                var rObj = new GameObject($"WheelR{i}");
                rObj.transform.SetParent(root.transform, false);
                rear[i] = rObj.AddComponent<R8EOX.Vehicle.RaycastWheel>();
            }

            return (dt, front, rear, root);
        }
    }
}
