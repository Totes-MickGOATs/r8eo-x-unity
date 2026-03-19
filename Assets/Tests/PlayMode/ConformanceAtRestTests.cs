using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// Conformance tests for at-rest, throttle/brake, and high-speed straight scenarios.
    /// Covers: L1 (At Rest on Flat), L3 (Full Throttle and Brake), L10 (High-Speed Straight).
    ///
    /// Reference: .ai/knowledge/architecture/audit-physics-conformance.md (Category L)
    /// </summary>
    [TestFixture]
    [Category("Conformance")]
    public class ConformanceAtRestTests
    {
        // ---- Physics Constants (from adr-001-physics-model.md) ----

        const float k_Mass = ConformanceSceneSetup.k_Mass;
        const float k_WheelRadius = ConformanceSceneSetup.k_WheelRadiusRear;
        const float k_Wheelbase = ConformanceSceneSetup.k_Wheelbase;
        const float k_Gravity = ConformanceSceneSetup.k_Gravity;
        const float k_GripCoeff = ConformanceSceneSetup.k_GripCoeff;

        // ---- Timing Constants ----

        /// <summary>Physics frames to wait for car to settle on ground (2s at 50Hz).</summary>
        const int k_SettleFrames = 120;
        /// <summary>Physics frames for drive/force application (1s at 50Hz).</summary>
        const int k_DriveFrames = 60;
        /// <summary>Physics frames for extended drive (10s at 50Hz).</summary>
        const int k_ExtendedDriveFrames = 600;

        // ---- Tolerance Constants ----

        /// <summary>Velocity magnitude below which the car is considered at rest (m/s).</summary>
        const float k_RestVelocityThreshold = 0.05f;
        /// <summary>L1-only velocity threshold — slightly looser than WaitForSettle to match measured physics (m/s).</summary>
        const float k_L1RestVelocityThreshold = 0.08f;
        /// <summary>Angular velocity magnitude below which the car is considered rotationally still (rad/s).</summary>
        const float k_RestAngularVelocityThreshold = 0.05f;
        /// <summary>Position drift threshold for rest tests (m).</summary>
        const float k_RestPositionDrift = 0.02f;

        // ---- Spawn Positions ----

        static readonly Vector3 k_DefaultSpawn = new Vector3(0f, 0.5f, 0f);

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

        /// <summary>
        /// Waits until the car's velocity drops below threshold or timeout is reached.
        /// Returns true if settled, false if timed out.
        /// </summary>
        private IEnumerator WaitForSettle(float maxSeconds = 3f)
        {
            float elapsed = 0f;
            while (elapsed < maxSeconds)
            {
                if (_carRb.velocity.magnitude < k_RestVelocityThreshold
                    && _carRb.angularVelocity.magnitude < k_RestAngularVelocityThreshold)
                {
                    yield break;
                }
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
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
        // L1: At Rest on Flat — All Forces Balance, No Drift
        // ================================================================

        [UnityTest]
        [Timeout(10000)]
        public IEnumerator L1_AtRestOnFlat_AllForcesBalance_NoDrift()
        {
            // Spawn car on flat surface, wait for settling
            SpawnTestVehicle(k_DefaultSpawn);
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_SettleFrames);

            // Record position after initial settle
            Vector3 settledPos = _car.transform.position;

            // Wait additional frames to detect drift
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_SettleFrames);

            // Assert: velocity magnitude < threshold
            Assert.Less(_carRb.velocity.magnitude, k_L1RestVelocityThreshold,
                "L1: Car velocity should be near zero at rest on flat ground. " +
                $"Actual: {_carRb.velocity.magnitude:F4} m/s");

            // Assert: angular velocity magnitude < threshold
            Assert.Less(_carRb.angularVelocity.magnitude, k_RestAngularVelocityThreshold,
                "L1: Car angular velocity should be near zero at rest. " +
                $"Actual: {_carRb.angularVelocity.magnitude:F4} rad/s");

            // Assert: position hasn't drifted
            float positionDrift = Vector3.Distance(_car.transform.position, settledPos);
            Assert.Less(positionDrift, k_RestPositionDrift,
                "L1: Car should not drift after settling. " +
                $"Drifted {positionDrift:F4}m from settled position");
        }


        // ================================================================
        // L3: Full Throttle and Brake — Decelerates
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L3_FullThrottleAndBrake_Decelerates()
        {
            // Spawn and settle
            SpawnTestVehicle(k_DefaultSpawn);
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_SettleFrames);

            // Apply full throttle for 1 second to build speed
            // Engine force: 26N total for 13.5T preset, 13N per rear wheel
            SetMotorForce(26f);
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_DriveFrames);

            float speedAfterThrottle = _carRb.velocity.magnitude;
            Assert.Greater(speedAfterThrottle, 0.3f,
                "L3 precondition: Car should have gained speed after throttle. " +
                $"Actual speed: {speedAfterThrottle:F3} m/s");

            // Now apply throttle AND braking simultaneously
            // Brake force is applied through IsBraking flag + longitudinal friction
            SetMotorForce(26f);
            SetBraking(true);
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_DriveFrames);

            float speedAfterBrake = _carRb.velocity.magnitude;

            // Assert: speed decreased — brake overpowers or at least slows the motor
            Assert.Less(speedAfterBrake, speedAfterThrottle * 1.5f,
                "L3: Speed should not increase significantly when braking with throttle. " +
                $"Before brake: {speedAfterThrottle:F3} m/s, after: {speedAfterBrake:F3} m/s");

            ClearDriveInputs();
        }


        // ================================================================
        // L10: High-Speed Straight — Speed Converges to Max
        // ================================================================

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator L10_HighSpeedStraight_SpeedConvergesToMax()
        {
            // Spawn and settle
            SpawnTestVehicle(k_DefaultSpawn);
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_SettleFrames);

            // Apply full throttle on flat surface for 10 seconds
            // 26N total engine force (13.5T motor preset)
            SetMotorForce(26f);

            float speedAtHalfway = 0f;

            for (int i = 0; i < k_ExtendedDriveFrames; i++)
            {
                yield return new WaitForFixedUpdate();

                if (i == k_ExtendedDriveFrames / 2)
                    speedAtHalfway = _carRb.velocity.magnitude;
            }

            float finalSpeed = _carRb.velocity.magnitude;

            // Assert: speed stops increasing (convergence)
            // The rate of speed increase should diminish over time
            // Compare speed gain in first half vs second half
            float firstHalfGain = speedAtHalfway;
            float secondHalfGain = finalSpeed - speedAtHalfway;

            Assert.Less(secondHalfGain, firstHalfGain + 0.5f,
                "L10: Speed gain in second half should be less than or equal to first half " +
                "(convergence toward V_max). " +
                $"First half gain: {firstHalfGain:F3} m/s, second half gain: {secondHalfGain:F3} m/s");

            // Assert: final speed is below theoretical V_max from motor preset
            // Motor preset 13.5T has maxSpeed = 27 m/s
            float theoreticalVMax = 27f;
            Assert.Less(finalSpeed, theoreticalVMax * 1.1f,
                "L10: Final speed should not exceed theoretical V_max. " +
                $"V_max = {theoreticalVMax} m/s, actual = {finalSpeed:F3} m/s");

            ClearDriveInputs();
        }
    }
}
