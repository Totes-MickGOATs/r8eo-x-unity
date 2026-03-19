using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// Conformance tests for jump landing scenarios.
    /// Covers: L5 (Jump Landing — Impact Proportional to Height).
    ///
    /// Reference: .ai/knowledge/architecture/audit-physics-conformance.md (Category L)
    /// </summary>
    [TestFixture]
    [Category("Conformance")]
    public class ConformanceLandingTests
    {
        // ---- Physics Constants (from adr-001-physics-model.md) ----

        const float k_Gravity = ConformanceSceneSetup.k_Gravity;

        // ---- Timing Constants ----

        /// <summary>Physics frames to wait for car to settle on ground (2s at 50Hz).</summary>
        const int k_SettleFrames = 120;

        // ---- Spawn Positions ----

        static readonly Vector3 k_LowDropSpawn = new Vector3(0f, 0.5f, 0f);
        static readonly Vector3 k_HighDropSpawn = new Vector3(0f, 1.0f, 0f);

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
            ConformanceSceneSetup.SpawnTestVehicle(
                spawnPosition, out _ground, out _car, out _carRb, out _rcCar, out _wheels);
        }


        // ================================================================
        // L5: Jump Landing — Impact Proportional to Height
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L5_JumpLanding_ImpactProportionalToHeight()
        {
            // --- Drop from low height (0.5m above ground surface) ---
            SpawnTestVehicle(k_LowDropSpawn);

            // Wait for landing — track peak downward velocity before contact
            float lowDropPeakVelocity = 0f;
            for (int i = 0; i < k_SettleFrames; i++)
            {
                float downSpeed = -_carRb.velocity.y;
                if (downSpeed > lowDropPeakVelocity)
                    lowDropPeakVelocity = downSpeed;
                yield return new WaitForFixedUpdate();
            }

            // Clean up first car
            Object.DestroyImmediate(_car);
            Object.DestroyImmediate(_ground);

            // --- Drop from high height (1.0m above ground surface) ---
            SpawnTestVehicle(k_HighDropSpawn);

            float highDropPeakVelocity = 0f;
            for (int i = 0; i < k_SettleFrames; i++)
            {
                float downSpeed = -_carRb.velocity.y;
                if (downSpeed > highDropPeakVelocity)
                    highDropPeakVelocity = downSpeed;
                yield return new WaitForFixedUpdate();
            }

            // Assert: higher drop produces larger impact velocity
            Assert.Greater(highDropPeakVelocity, lowDropPeakVelocity,
                "L5: Higher drop should produce greater impact velocity. " +
                $"Low drop peak: {lowDropPeakVelocity:F3} m/s, high drop peak: {highDropPeakVelocity:F3} m/s");

            // Assert: impact velocity approximately matches v = sqrt(2*g*h) within tolerance
            // Ground surface is at y = 0.05 (top of 0.1m thick cube), car spawns at given y
            // Effective drop height for the high drop:
            // h_eff = spawnY - (ground_surface + suspension_rest) approximately
            // We use a loose 40% tolerance because suspension engagement absorbs some fall
            float expectedHighVelocity = Mathf.Sqrt(2f * k_Gravity * k_HighDropSpawn.y);
            float tolerance = expectedHighVelocity * 0.40f;
            Assert.AreEqual(expectedHighVelocity, highDropPeakVelocity, tolerance,
                "L5: Impact velocity should approximate sqrt(2*g*h). " +
                $"Expected ~{expectedHighVelocity:F3} m/s (+/-{tolerance:F3}), " +
                $"got {highDropPeakVelocity:F3} m/s");
        }
    }
}
