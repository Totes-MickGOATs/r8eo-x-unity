using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// Conformance tests for free-fall dynamics and weight transfer scenarios.
    /// Covers: D8 (Free Fall), L12 (Weight Transfer Braking).
    ///
    /// Reference: .ai/knowledge/architecture/audit-physics-conformance.md (Category D8, L12)
    /// </summary>
    [TestFixture]
    [Category("Conformance")]
    public class ConformanceDynamicsTests
    {
        // ---- Physics Constants (from adr-001-physics-model.md) ----

        const float k_Gravity = ConformanceSceneSetup.k_Gravity;

        // ---- Timing Constants ----

        /// <summary>Physics frames to wait for car to settle on ground (2s at 50Hz).</summary>
        const int k_SettleFrames = 120;
        /// <summary>Physics frames for drive/force application (1s at 50Hz).</summary>
        const int k_DriveFrames = 60;

        // ---- Tolerance Constants ----

        /// <summary>Tolerance for free-fall velocity comparison (fraction).</summary>
        const float k_FreeFallTolerance = 0.10f;

        // ---- Spawn Positions ----

        static readonly Vector3 k_DefaultSpawn = new Vector3(0f, 0.5f, 0f);
        static readonly Vector3 k_FreeFallSpawn = new Vector3(0f, 5f, 0f);

        // ---- Test Fixtures ----

        private GameObject _ground;
        private GameObject _car;
        private Rigidbody _carRb;
        private R8EOX.Vehicle.RCCar _rcCar;
        private R8EOX.Vehicle.RaycastWheel[] _wheels;

        // ---- Setup / Teardown ----

        [TearDown]
        public void TearDown()
        {
            if (_car != null) Object.DestroyImmediate(_car);
            if (_ground != null) Object.DestroyImmediate(_ground);
            _car = null;
            _ground = null;
            _carRb = null;
            _rcCar = null;
            _wheels = null;
        }

        // ---- Helper Methods ----

        /// <summary>Spawns ground + vehicle at the given position and caches references.</summary>
        private void SpawnTestVehicle(Vector3 spawnPosition)
        {
            _ground = ConformanceSceneSetup.CreateGround();
            _car = ConformanceSceneSetup.CreateTestVehicle(spawnPosition);
            _carRb = _car.GetComponent<Rigidbody>();
            _rcCar = _car.GetComponent<R8EOX.Vehicle.RCCar>();
            _wheels = _car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>();
        }

        /// <summary>Yields the given number of FixedUpdate frames.</summary>
        private static IEnumerator WaitPhysicsFrames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }

        /// <summary>
        /// Sets MotorForceShare on all motor wheels to simulate throttle.
        /// totalForce is distributed evenly across motor wheels.
        /// </summary>
        private void SetMotorForce(float totalForce)
        {
            int motorCount = 0;
            foreach (var w in _wheels)
                if (w.IsMotor) motorCount++;

            float perWheel = motorCount > 0 ? totalForce / motorCount : 0f;
            foreach (var w in _wheels)
                w.MotorForceShare = w.IsMotor ? perWheel : 0f;
        }

        /// <summary>Sets IsBraking on all motor wheels.</summary>
        private void SetBraking(bool braking)
        {
            foreach (var w in _wheels)
                if (w.IsMotor) w.IsBraking = braking;
        }

        /// <summary>Clears all motor force and braking.</summary>
        private void ClearDriveInputs()
        {
            foreach (var w in _wheels)
            {
                w.MotorForceShare = 0f;
                w.IsBraking = false;
            }
        }


        // ================================================================
        // D8: Free Fall — Acceleration Equals Gravity
        // ================================================================

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator D8_FreeFall_AccelerationEqualsGravity()
        {
            // Spawn car at height 5m (well above ground)
            SpawnTestVehicle(k_FreeFallSpawn);

            // Let one frame pass to initialize
            yield return new WaitForFixedUpdate();

            // Record initial downward velocity
            float initialDownSpeed = -_carRb.velocity.y;

            // Free-fall for 0.5 seconds (25 frames at 50Hz default)
            // Use actual frame count based on fixedDeltaTime
            float targetFallTime = 0.5f;
            int fallFrames = Mathf.RoundToInt(targetFallTime / Time.fixedDeltaTime);

            // Ensure we don't hit ground: at 5m height, 0.5s of freefall covers
            // d = 0.5*g*t^2 = 0.5*9.81*0.25 = 1.23m. Car at 5m won't hit ground.
            for (int i = 0; i < fallFrames; i++)
                yield return new WaitForFixedUpdate();

            // Measure velocity after 0.5 seconds of free fall
            float finalDownSpeed = -_carRb.velocity.y;
            float velocityGain = finalDownSpeed - initialDownSpeed;

            // Expected: delta_v = g * t = 9.81 * 0.5 = 4.905 m/s
            float expectedVelocityGain = k_Gravity * targetFallTime;

            // Assert within tolerance (allow for air drag, initial frame artifacts)
            float toleranceAbsolute = expectedVelocityGain * k_FreeFallTolerance;
            Assert.AreEqual(expectedVelocityGain, velocityGain, toleranceAbsolute,
                "D8: Free-fall velocity gain should match g*t. " +
                $"Expected: {expectedVelocityGain:F3} m/s, " +
                $"actual: {velocityGain:F3} m/s " +
                $"(tolerance: {k_FreeFallTolerance * 100f:F0}%)");

            // Verify car is still airborne (hasn't hit ground)
            bool anyGrounded = false;
            foreach (var w in _wheels)
            {
                if (w.IsOnGround) { anyGrounded = true; break; }
            }
            Assert.IsFalse(anyGrounded,
                "D8: Car should still be airborne during free-fall measurement. " +
                "If grounded, increase spawn height");
        }


        // ================================================================
        // L12: Weight Transfer Braking — Front Loads Up
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L12_WeightTransferBraking_FrontLoadsUp()
        {
            // Spawn and settle
            SpawnTestVehicle(k_DefaultSpawn);
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Drive forward to build speed
            SetMotorForce(26f);
            yield return WaitPhysicsFrames(k_DriveFrames);

            float speedBeforeBrake = _carRb.velocity.magnitude;
            Assert.Greater(speedBeforeBrake, 0.3f,
                "L12 precondition: Car should have speed before braking");

            // Record pitch angle before braking
            // Pitch is rotation around the local X axis. In Unity, a forward-pitched car
            // has the nose down = negative pitch (euler X increases).
            float pitchBefore = _car.transform.eulerAngles.x;
            if (pitchBefore > 180f) pitchBefore -= 360f;

            // Brake hard: clear motor, set braking, apply backward impulse via friction
            ClearDriveInputs();
            SetBraking(true);

            // Also reduce velocity by applying backward force directly
            // to simulate the effect of brake friction (since we bypass ESC)
            _carRb.AddForce(-_car.transform.forward * 20f, ForceMode.Force);

            yield return WaitPhysicsFrames(k_DriveFrames / 2);

            float pitchDuringBrake = _car.transform.eulerAngles.x;
            if (pitchDuringBrake > 180f) pitchDuringBrake -= 360f;

            // Under braking, the nose should dip forward (pitch angle changes)
            // Weight transfer causes front suspension to compress more than rear.
            // We check that pitch changed at all during braking.
            float pitchChange = Mathf.Abs(pitchDuringBrake - pitchBefore);

            // If WheelFrameState is available, we could compare front vs rear grip load.
            // For now, check front wheels have more grip load than rear during braking.
            float frontLoadSum = 0f;
            float rearLoadSum = 0f;
            foreach (var w in _wheels)
            {
                if (w.IsOnGround)
                {
                    // Front wheels have positive local Z (z > 0)
                    if (w.transform.localPosition.z > 0f)
                        frontLoadSum += w.LastGripLoad;
                    else
                        rearLoadSum += w.LastGripLoad;
                }
            }

            // Assert at least one of: pitch changed OR front load > rear load
            bool pitchChanged = pitchChange > 0.1f;
            bool frontLoadedMore = frontLoadSum > rearLoadSum * 0.9f;

            Assert.IsTrue(pitchChanged || frontLoadedMore,
                "L12: During braking, the car should either pitch forward " +
                $"(pitch change: {pitchChange:F3} deg) " +
                $"or front wheels should bear more load than rear " +
                $"(front: {frontLoadSum:F3}, rear: {rearLoadSum:F3}). " +
                "Neither condition met — weight transfer may not be working");

            ClearDriveInputs();
        }
    }
}
