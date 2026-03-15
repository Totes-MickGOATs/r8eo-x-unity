using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for terrain anti-snag: contact smoothness, no phantom velocity,
    /// and suspension stability on seamed ground.
    ///
    /// Seamed ground simulates terrain triangle edge seams using a row of thin cubes
    /// with alternating height offsets. This reproduces the conditions under which
    /// Physics.Raycast produces discontinuous normals at edges.
    ///
    /// Reference: feat/terrain-anti-snag — SphereCast anti-snag fix + beveled colliders
    /// </summary>
    [TestFixture]
    [Category("AntiSnag")]
    public class TerrainSnagTests
    {
        // ---- Timing Constants ----

        /// <summary>Physics frames for settling (1s at 50Hz).</summary>
        const int k_SettleFrames = 60;
        /// <summary>Physics frames for measurement window (2.4s at 50Hz).</summary>
        const int k_MeasureFrames = 120;

        // ---- Threshold Constants ----

        /// <summary>Contact point jump threshold (m/frame). Jumps larger than this indicate snag.</summary>
        const float k_ContactJumpThreshold = 0.04f;
        /// <summary>Maximum total jump count across all 4 wheels over 120 frames.</summary>
        const int k_MaxAllowedJumps = 2;
        /// <summary>Maximum velocity magnitude indicating phantom acceleration (m/s).</summary>
        const float k_PhantomVelocityThreshold = 0.1f;
        /// <summary>Maximum frame-over-frame suspension force delta (N). Spikes above this indicate edge snag.</summary>
        const float k_MaxForceDelta = 25f;
        /// <summary>Forward velocity applied to simulate driving over seams (m/s).</summary>
        const float k_DriveVelocity = 5f;

        // ---- Seam Geometry Constants ----

        /// <summary>Number of seam slabs in the row.</summary>
        const int k_SeamSlabCount = 10;
        /// <summary>Width of each slab (m).</summary>
        const float k_SlabWidth = 2f;
        /// <summary>Length of each slab (m).</summary>
        const float k_SlabLength = 10f;
        /// <summary>Thickness of each slab (m) — thin to expose triangle edges.</summary>
        const float k_SlabThickness = 0.1f;
        /// <summary>Height offset applied to alternating slabs (m).</summary>
        const float k_SeamOffset = 0.008f;

        // ---- Test Fixtures ----

        private List<GameObject> _seamedGround;
        private GameObject _flatGround;
        private GameObject _car;
        private Rigidbody _carRb;
        private R8EOX.Vehicle.RaycastWheel[] _wheels;


        // ---- Setup / Teardown ----

        [TearDown]
        public void TearDown()
        {
            if (_car != null) Object.DestroyImmediate(_car);
            if (_flatGround != null) Object.DestroyImmediate(_flatGround);
            if (_seamedGround != null)
            {
                foreach (var slab in _seamedGround)
                    if (slab != null) Object.DestroyImmediate(slab);
                _seamedGround = null;
            }
            _car = null;
            _flatGround = null;
            _carRb = null;
            _wheels = null;
        }


        // ---- Ground Factories ----

        /// <summary>
        /// Creates a row of 10 thin cubes with alternating height offsets to simulate
        /// terrain triangle edge seams. The vehicle drives along Z axis over the seams.
        /// </summary>
        private List<GameObject> CreateSeamedGround()
        {
            var slabs = new List<GameObject>();
            float totalWidth = k_SeamSlabCount * k_SlabWidth;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < k_SeamSlabCount; i++)
            {
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = $"SeamSlab_{i}";

                float slabCenterX = startX + i * k_SlabWidth + k_SlabWidth * 0.5f;
                float heightOffset = (i % 2 == 0) ? 0f : k_SeamOffset;

                slab.transform.position = new Vector3(
                    slabCenterX,
                    heightOffset - k_SlabThickness * 0.5f,
                    0f);
                slab.transform.localScale = new Vector3(k_SlabWidth, k_SlabThickness, k_SlabLength);
                slab.layer = ConformanceSceneSetup.k_GroundLayer;
                slabs.Add(slab);
            }

            return slabs;
        }

        /// <summary>
        /// Spawns the test vehicle on seamed ground.
        /// Vehicle is placed above y=0 (the base surface level).
        /// </summary>
        private void SpawnOnSeamedGround()
        {
            _seamedGround = CreateSeamedGround();
            // Spawn above seam surface — seams alternate between y=0 and y=k_SeamOffset
            _car = ConformanceSceneSetup.CreateTestVehicle(new Vector3(0f, 0.5f, 0f));
            _carRb = _car.GetComponent<Rigidbody>();
            _wheels = _car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>();
        }

        /// <summary>
        /// Spawns the test vehicle on a single flat ground cube (regression baseline).
        /// </summary>
        private void SpawnOnFlatGround()
        {
            _flatGround = ConformanceSceneSetup.CreateGround();
            _car = ConformanceSceneSetup.CreateTestVehicle(new Vector3(0f, 0.5f, 0f));
            _carRb = _car.GetComponent<Rigidbody>();
            _wheels = _car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>();
        }

        /// <summary>Yields the given number of FixedUpdate frames.</summary>
        private static IEnumerator WaitPhysicsFrames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new WaitForFixedUpdate();
        }


        // ================================================================
        // Test 1: Contact point jumps are bounded on seamed ground
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator Car_DrivingOverSeams_ContactPointsSmooth()
        {
            SpawnOnSeamedGround();

            // Settle on ground first
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Apply forward velocity to drive over seams
            _carRb.velocity = Vector3.forward * k_DriveVelocity;

            // Sample contact points and count frame-over-frame jumps
            var prevContacts = new Vector3[_wheels.Length];
            for (int w = 0; w < _wheels.Length; w++)
                prevContacts[w] = _wheels[w].ContactPoint;

            int totalJumps = 0;

            for (int frame = 0; frame < k_MeasureFrames; frame++)
            {
                yield return new WaitForFixedUpdate();

                for (int w = 0; w < _wheels.Length; w++)
                {
                    if (!_wheels[w].IsOnGround) continue;

                    float jump = Vector3.Distance(_wheels[w].ContactPoint, prevContacts[w]);
                    if (jump > k_ContactJumpThreshold)
                        totalJumps++;

                    prevContacts[w] = _wheels[w].ContactPoint;
                }
            }

            Assert.LessOrEqual(totalJumps, k_MaxAllowedJumps,
                $"AntiSnag: Contact point jumps > {k_ContactJumpThreshold}m across all wheels " +
                $"over {k_MeasureFrames} frames should be <= {k_MaxAllowedJumps}. " +
                $"Actual jump count: {totalJumps}. " +
                "SphereCast should smooth normals over triangle edges.");
        }


        // ================================================================
        // Test 2: No phantom velocity gain on seamed ground at rest
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator Car_DrivingOverSeams_NoPhantomVelocityGain()
        {
            SpawnOnSeamedGround();

            // Settle to rest — zero inputs, wait for car to settle
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Measure velocity over the next 120 frames — should stay near zero
            float maxVelocity = 0f;

            for (int frame = 0; frame < k_MeasureFrames; frame++)
            {
                yield return new WaitForFixedUpdate();
                float vel = _carRb.velocity.magnitude;
                if (vel > maxVelocity)
                    maxVelocity = vel;
            }

            Assert.LessOrEqual(maxVelocity, k_PhantomVelocityThreshold,
                $"AntiSnag: At-rest car on seamed ground should not exceed {k_PhantomVelocityThreshold} m/s. " +
                $"Max velocity observed: {maxVelocity:F4} m/s. " +
                "Sharp BoxCollider edges catching seam lips cause phantom acceleration.");
        }


        // ================================================================
        // Test 3: Suspension force deltas bounded while driving over seams
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator Car_DrivingOverSeams_SuspensionForceStable()
        {
            SpawnOnSeamedGround();

            // Settle on ground first
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Apply forward velocity to drive over seams
            _carRb.velocity = Vector3.forward * k_DriveVelocity;

            // Sample suspension forces and compute max frame-over-frame delta
            var prevForces = new float[_wheels.Length];
            for (int w = 0; w < _wheels.Length; w++)
                prevForces[w] = _wheels[w].SuspensionForce;

            float maxForceDelta = 0f;

            for (int frame = 0; frame < k_MeasureFrames; frame++)
            {
                yield return new WaitForFixedUpdate();

                for (int w = 0; w < _wheels.Length; w++)
                {
                    if (!_wheels[w].IsOnGround) continue;

                    float delta = Mathf.Abs(_wheels[w].SuspensionForce - prevForces[w]);
                    if (delta > maxForceDelta)
                        maxForceDelta = delta;

                    prevForces[w] = _wheels[w].SuspensionForce;
                }
            }

            Assert.LessOrEqual(maxForceDelta, k_MaxForceDelta,
                $"AntiSnag: Max suspension force delta should be <= {k_MaxForceDelta}N per frame. " +
                $"Actual max delta: {maxForceDelta:F2}N. " +
                "SphereCast normal averaging should suppress force spikes at triangle edges.");
        }


        // ================================================================
        // Test 4 (Regression): Flat ground physics unchanged
        // ================================================================

        [UnityTest]
        [Timeout(15000)]
        public IEnumerator Car_FlatGround_PhysicsUnchanged()
        {
            SpawnOnFlatGround();

            // Settle
            yield return WaitPhysicsFrames(k_SettleFrames);

            // Check spring length is near rest distance (within tolerance)
            float avgSpringLen = 0f;
            int groundedWheels = 0;
            foreach (var w in _wheels)
            {
                if (w.IsOnGround)
                {
                    avgSpringLen += w.LastSpringLen;
                    groundedWheels++;
                }
            }

            Assert.Greater(groundedWheels, 0, "Regression: At least one wheel must be on flat ground after settling.");

            avgSpringLen /= groundedWheels;
            float restDistance = 0.20f; // ConformanceSceneSetup wheel default
            Assert.AreEqual(restDistance, avgSpringLen, 0.03f,
                $"Regression: Spring length after settling should be near rest distance {restDistance}m. " +
                $"Actual avg: {avgSpringLen:F4}m. SphereCast must not alter settled spring length.");

            // Apply motor force to a rear wheel to verify car moves forward (not backward)
            var rearWheels = new List<R8EOX.Vehicle.RaycastWheel>();
            foreach (var w in _wheels)
                if (w.IsMotor) rearWheels.Add(w);

            float forcePerWheel = rearWheels.Count > 0 ? 26f / rearWheels.Count : 0f;
            foreach (var w in rearWheels)
                w.MotorForceShare = forcePerWheel;

            Vector3 startPos = _car.transform.position;
            yield return WaitPhysicsFrames(k_SettleFrames);

            float forwardDelta = _car.transform.position.z - startPos.z;
            Assert.Greater(forwardDelta, 0.01f,
                $"Regression: Car should move forward under motor force. " +
                $"Forward delta: {forwardDelta:F4}m. SphereCast must not break motor drive.");

            // Verify no backward drift
            Assert.Greater(forwardDelta, -0.01f,
                $"Regression: Car must not drift backward. Delta: {forwardDelta:F4}m.");

            // Clear motor
            foreach (var w in rearWheels)
                w.MotorForceShare = 0f;
        }
    }
}
