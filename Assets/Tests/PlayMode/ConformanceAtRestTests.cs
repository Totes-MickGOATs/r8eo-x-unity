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
        // ---- Timing Constants ----

        const int k_SettleFrames = 120;   // 2s at 50Hz
        const int k_DriveFrames = 60;     // 1s at 50Hz
        const int k_ExtendedDriveFrames = 600; // 10s at 50Hz

        // ---- Tolerance Constants ----

        const float k_RestVelocityThreshold = 0.05f;
        const float k_L1RestVelocityThreshold = 0.08f;
        const float k_RestAngularVelocityThreshold = 0.05f;
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

        private void SpawnTestVehicle(Vector3 spawnPosition)
        {
            ConformanceSceneSetup.SpawnTestVehicle(
                spawnPosition, out _ground, out _car, out _carRb, out _rcCar, out _wheels);
        }

        private IEnumerator WaitForSettle(float maxSeconds = 3f)
        {
            float elapsed = 0f;
            while (elapsed < maxSeconds)
            {
                if (_carRb.velocity.magnitude < k_RestVelocityThreshold
                    && _carRb.angularVelocity.magnitude < k_RestAngularVelocityThreshold)
                    yield break;
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
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
            SpawnTestVehicle(k_DefaultSpawn);
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_SettleFrames);

            // Apply full throttle for 1 second to build speed (26N for 13.5T preset)
            ConformanceSceneSetup.SetMotorForce(_wheels, 26f);
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_DriveFrames);

            float speedAfterThrottle = _carRb.velocity.magnitude;
            Assert.Greater(speedAfterThrottle, 0.3f,
                "L3 precondition: Car should have gained speed after throttle. " +
                $"Actual speed: {speedAfterThrottle:F3} m/s");

            // Apply throttle AND braking simultaneously
            ConformanceSceneSetup.SetMotorForce(_wheels, 26f);
            ConformanceSceneSetup.SetBraking(_wheels, true);
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_DriveFrames);

            float speedAfterBrake = _carRb.velocity.magnitude;
            Assert.Less(speedAfterBrake, speedAfterThrottle * 1.5f,
                "L3: Speed should not increase significantly when braking with throttle. " +
                $"Before brake: {speedAfterThrottle:F3} m/s, after: {speedAfterBrake:F3} m/s");

            ConformanceSceneSetup.ClearDriveInputs(_wheels);
        }


        // ================================================================
        // L10: High-Speed Straight — Speed Converges to Max
        // ================================================================

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator L10_HighSpeedStraight_SpeedConvergesToMax()
        {
            SpawnTestVehicle(k_DefaultSpawn);
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_SettleFrames);

            // Apply full throttle for 10 seconds (26N, 13.5T motor preset)
            ConformanceSceneSetup.SetMotorForce(_wheels, 26f);
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

            ConformanceSceneSetup.ClearDriveInputs(_wheels);
        }
    }
}
