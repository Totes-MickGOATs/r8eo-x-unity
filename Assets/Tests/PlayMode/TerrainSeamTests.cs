using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for terrain anti-snag on seamed ground.
    ///
    /// Covers: contact point smoothness, phantom velocity absence, and
    /// suspension force stability while driving over simulated seam edges.
    ///
    /// Seamed ground simulates terrain triangle edge seams using a row of thin cubes
    /// with alternating height offsets. This reproduces the conditions under which
    /// Physics.Raycast produces discontinuous normals at edges.
    ///
    /// Reference: feat/terrain-anti-snag — SphereCast anti-snag fix + beveled colliders
    /// </summary>
    [TestFixture]
    [Category("AntiSnag")]
    public class TerrainSeamTests : TerrainTestFixture
    {
        // ---- Threshold Constants ----

        /// <summary>
        /// Vertical contact point jump threshold (m/frame).
        /// Measures Y-axis displacement only — the horizontal component grows linearly with speed
        /// and is not an anti-snag signal. Vertical jumps > this threshold indicate terrain snag.
        /// </summary>
        const float k_ContactJumpThreshold = 0.04f;
        /// <summary>Maximum total vertical-jump count across all 4 wheels over 120 frames.</summary>
        const int k_MaxAllowedJumps = 4;
        /// <summary>Maximum velocity magnitude indicating phantom acceleration (m/s).</summary>
        const float k_PhantomVelocityThreshold = 0.15f;
        /// <summary>Maximum frame-over-frame suspension force delta (N). Spikes above this indicate edge snag.</summary>
        const float k_MaxForceDelta = 25f;
        /// <summary>Forward velocity applied to simulate driving over seams (m/s).</summary>
        const float k_DriveVelocity = 5f;


        [TearDown]
        public void TearDown() => TearDownScene();


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
            CarRb.velocity = Vector3.forward * k_DriveVelocity;

            // Sample contact points and count frame-over-frame jumps
            var prevContacts = new Vector3[Wheels.Length];
            for (int w = 0; w < Wheels.Length; w++)
                prevContacts[w] = Wheels[w].ContactPoint;

            int totalJumps = 0;

            for (int frame = 0; frame < k_MeasureFrames; frame++)
            {
                yield return new WaitForFixedUpdate();

                for (int w = 0; w < Wheels.Length; w++)
                {
                    if (!Wheels[w].IsOnGround) continue;

                    // Measure only the vertical (Y) component of the contact point displacement.
                    // The horizontal component grows linearly with forward speed and is not a snag signal.
                    // Vertical jumps > threshold indicate the wheel is snagging on a seam edge.
                    float verticalJump = Mathf.Abs(Wheels[w].ContactPoint.y - prevContacts[w].y);
                    if (verticalJump > k_ContactJumpThreshold)
                        totalJumps++;

                    prevContacts[w] = Wheels[w].ContactPoint;
                }
            }

            Assert.LessOrEqual(totalJumps, k_MaxAllowedJumps,
                $"AntiSnag: Vertical contact point jumps > {k_ContactJumpThreshold}m across all wheels " +
                $"over {k_MeasureFrames} frames should be <= {k_MaxAllowedJumps}. " +
                $"Actual jump count: {totalJumps}. " +
                "SphereCast should smooth vertical contact normal discontinuities at seam edges.");
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
                float vel = CarRb.velocity.magnitude;
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
            CarRb.velocity = Vector3.forward * k_DriveVelocity;

            // Sample suspension forces and compute max frame-over-frame delta
            var prevForces = new float[Wheels.Length];
            for (int w = 0; w < Wheels.Length; w++)
                prevForces[w] = Wheels[w].SuspensionForce;

            float maxForceDelta = 0f;

            for (int frame = 0; frame < k_MeasureFrames; frame++)
            {
                yield return new WaitForFixedUpdate();

                for (int w = 0; w < Wheels.Length; w++)
                {
                    if (!Wheels[w].IsOnGround) continue;

                    float delta = Mathf.Abs(Wheels[w].SuspensionForce - prevForces[w]);
                    if (delta > maxForceDelta)
                        maxForceDelta = delta;

                    prevForces[w] = Wheels[w].SuspensionForce;
                }
            }

            Assert.LessOrEqual(maxForceDelta, k_MaxForceDelta,
                $"AntiSnag: Max suspension force delta should be <= {k_MaxForceDelta}N per frame. " +
                $"Actual max delta: {maxForceDelta:F2}N. " +
                "SphereCast normal averaging should suppress force spikes at triangle edges.");
        }
    }
}
