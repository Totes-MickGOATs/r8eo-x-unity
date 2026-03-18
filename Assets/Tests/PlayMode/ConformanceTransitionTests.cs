using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// Conformance tests for ground/air transition and jump landing scenarios.
    /// Covers: L7 (Ground-to-Air), L8 (Air-to-Ground), L5 (Jump Landing).
    ///
    /// Reference: .ai/knowledge/architecture/audit-physics-conformance.md (Category L)
    /// </summary>
    [TestFixture]
    [Category("Conformance")]
    public class ConformanceTransitionTests
    {
        // ---- Physics Constants (from adr-001-physics-model.md) ----

        const float k_Gravity = ConformanceSceneSetup.k_Gravity;

        // ---- Timing Constants ----

        /// <summary>Physics frames to wait for car to settle on ground (2s at 50Hz).</summary>
        const int k_SettleFrames = 120;
        /// <summary>Physics frames for long settling after landing (3s at 50Hz).</summary>
        const int k_LandingSettleFrames = 180;

        // ---- Tolerance Constants ----

        /// <summary>Velocity magnitude below which the car is considered at rest (m/s).</summary>
        const float k_RestVelocityThreshold = 0.05f;
        /// <summary>Maximum velocity discontinuity between frames (m/s).</summary>
        const float k_MaxVelocityDiscontinuity = 1.5f;

        // ---- Spawn Positions ----

        static readonly Vector3 k_DefaultSpawn = new Vector3(0f, 0.5f, 0f);
        static readonly Vector3 k_LowDropSpawn = new Vector3(0f, 0.5f, 0f);
        static readonly Vector3 k_HighDropSpawn = new Vector3(0f, 1.0f, 0f);
        static readonly Vector3 k_LandingDropSpawn = new Vector3(0f, 0.3f, 0f);

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


        // ================================================================
        // L7: Ground-to-Air Transition — Smooth Force Transition
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L7_GroundToAir_SmoothForceTransition()
        {
            // Spawn car, settle, then launch it upward to create a ground-to-air transition
            SpawnTestVehicle(k_DefaultSpawn);
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Give the car upward + forward velocity to simulate driving off an edge
            _carRb.velocity = new Vector3(0f, 3f, 5f);

            // Sample velocity every FixedUpdate through the transition
            Vector3 prevVelocity = _carRb.velocity;
            float maxDiscontinuity = 0f;
            int sampleCount = 0;

            for (int i = 0; i < k_SettleFrames; i++)
            {
                yield return new WaitForFixedUpdate();
                Vector3 currentVelocity = _carRb.velocity;
                float discontinuity = (currentVelocity - prevVelocity).magnitude;

                if (discontinuity > maxDiscontinuity)
                    maxDiscontinuity = discontinuity;

                prevVelocity = currentVelocity;
                sampleCount++;
            }

            // Assert: no velocity discontinuity exceeds threshold between frames
            // Allow for gravity contribution per frame: g * dt ~= 9.81 * 0.02 = 0.196 m/s
            // Plus landing impact, so threshold is generous
            Assert.Less(maxDiscontinuity, k_MaxVelocityDiscontinuity,
                "L7: Velocity should not have discontinuities > " +
                $"{k_MaxVelocityDiscontinuity} m/s during ground-to-air transition. " +
                $"Max discontinuity observed: {maxDiscontinuity:F4} m/s over {sampleCount} frames");
        }


        // ================================================================
        // L8: Air-to-Ground Transition — Damped Landing
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator L8_AirToGround_DampedLanding()
        {
            // Drop car from 0.3m height
            SpawnTestVehicle(k_LandingDropSpawn);

            // Track vertical position peaks after landing to verify damped oscillation
            var verticalPeaks = new List<float>();
            float prevY = _car.transform.position.y;
            float prevPrevY = prevY;
            bool landedOnce = false;

            for (int i = 0; i < k_LandingSettleFrames; i++)
            {
                yield return new WaitForFixedUpdate();
                float currentY = _car.transform.position.y;

                // Detect if we've made first ground contact
                if (!landedOnce)
                {
                    bool anyGrounded = false;
                    foreach (var w in _wheels)
                    {
                        if (w.IsOnGround) { anyGrounded = true; break; }
                    }
                    if (anyGrounded) landedOnce = true;
                }

                // After landing, detect local maxima (bounce peaks)
                if (landedOnce && prevY > prevPrevY && prevY > currentY)
                {
                    verticalPeaks.Add(prevY);
                }

                prevPrevY = prevY;
                prevY = currentY;
            }

            // Assert: car settled within 3 seconds (velocity near zero)
            Assert.Less(_carRb.velocity.magnitude, k_RestVelocityThreshold * 2f,
                "L8: Car should settle within 3 seconds after landing. " +
                $"Velocity: {_carRb.velocity.magnitude:F4} m/s");

            // Assert: if there are bounce peaks, each is lower than the previous (damped)
            if (verticalPeaks.Count >= 2)
            {
                for (int i = 1; i < verticalPeaks.Count; i++)
                {
                    Assert.LessOrEqual(verticalPeaks[i], verticalPeaks[i - 1] + 0.005f,
                        $"L8: Bounce peak {i} ({verticalPeaks[i]:F4}m) should be <= " +
                        $"peak {i - 1} ({verticalPeaks[i - 1]:F4}m) for damped oscillation");
                }
            }
            // If no bounce peaks detected, the suspension absorbed the landing cleanly (also valid)
        }
    }
}
