using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for motor force direction, friction deceleration, and steering.
    /// Tests drive layout power routing (RWD vs AWD).
    /// Motor tests bypass RCInput by setting MotorForceShare directly on rear wheels.
    /// </summary>
    public class VehicleDriveTests
    {
        private readonly VehicleIntegrationHelper _h = new VehicleIntegrationHelper();

        [SetUp]    public void SetUp()    => _h.SetUp();
        [TearDown] public void TearDown() => _h.TearDown();

        [UnityTest]
        public IEnumerator Car_MotorForceOnRearWheels_PushesForward()
        {
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);
            Vector3 posBeforeForce = _h.Car.transform.position;

            foreach (var w in _h.Wheels)
            {
                if (w.IsMotor) w.MotorForceShare = 13f; // Half of 26 N engine force
            }

            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_DriveFrames);

            float forwardDisplacement = Vector3.Dot(
                _h.Car.transform.position - posBeforeForce, _h.Car.transform.forward);
            Assert.Greater(forwardDisplacement, 0.01f,
                "Positive MotorForceShare on rear wheels should push car forward (+Z). " +
                "If the car moves backward or sideways, the force direction axis is wrong " +
                "(common Godot->Unity port bug: Y/Z swap or sign inversion)");
        }

        [UnityTest]
        public IEnumerator Car_NegativeMotorForce_PushesBackward()
        {
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);
            Vector3 posBeforeForce = _h.Car.transform.position;

            foreach (var w in _h.Wheels)
            {
                if (w.IsMotor) w.MotorForceShare = -7f; // Negative = reverse
            }

            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_DriveFrames);

            float forwardDisplacement = Vector3.Dot(
                _h.Car.transform.position - posBeforeForce, _h.Car.transform.forward);
            Assert.Less(forwardDisplacement, -0.005f,
                "Negative MotorForceShare should push car backward (-Z). " +
                "If the car moves forward, reverse force direction is inverted");
        }

        [UnityTest]
        public IEnumerator Car_InitialVelocity_FrictionDecelerates()
        {
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);
            _h.CarRb.velocity = _h.Car.transform.forward * 5f;
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_DriveFrames);

            Assert.Less(_h.CarRb.velocity.magnitude, 5f,
                "Car should decelerate from initial velocity due to wheel friction. " +
                "If speed stays constant or increases, longitudinal friction is not working");
        }

        [UnityTest]
        public IEnumerator Car_SteerRight_WheelsTurnRight()
        {
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);
            Assert.AreEqual(0f, _h.RcCar.CurrentSteering, 0.01f,
                "With no input, current steering should be zero (wheels straight)");

            foreach (var w in _h.Wheels)
            {
                if (w.IsSteer)
                {
                    float yRotation = w.transform.localEulerAngles.y;
                    if (yRotation > 180f) yRotation -= 360f; // normalize to -180..180
                    Assert.AreEqual(0f, yRotation, 1f, $"Steer wheel {w.name} should be straight with no input");
                }
            }
        }


        // ---- Drive Layout (power routing) — synchronous, no physics needed ----

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
                    Assert.IsTrue(w.IsMotor, "Rear wheel should always be motor");
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

        private (R8EOX.Vehicle.Drivetrain dt,
                 R8EOX.Vehicle.RaycastWheel[] front,
                 R8EOX.Vehicle.RaycastWheel[] rear,
                 GameObject root)
            CreateDrivetrainTestRig(R8EOX.Vehicle.Drivetrain.DriveLayout layout)
        {
            var root = new GameObject("DrivetrainTestRig");

            var drivetrainObj = new GameObject("Drivetrain");
            drivetrainObj.transform.SetParent(root.transform, false);
            var dt = drivetrainObj.AddComponent<R8EOX.Vehicle.Drivetrain>();
            dt.ActiveDriveLayout = layout;

            var front = new R8EOX.Vehicle.RaycastWheel[2];
            var rear  = new R8EOX.Vehicle.RaycastWheel[2];

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
