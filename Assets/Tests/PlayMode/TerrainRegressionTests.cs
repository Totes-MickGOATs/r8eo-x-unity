using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// PlayMode regression tests ensuring flat-ground physics are unchanged after
    /// the terrain anti-snag (SphereCast) changes.
    ///
    /// These tests verify that the SphereCast fix does not alter settled spring length,
    /// motor drive behaviour, or introduce backward drift on flat surfaces.
    ///
    /// Reference: feat/terrain-anti-snag — SphereCast anti-snag fix + beveled colliders
    /// </summary>
    [TestFixture]
    [Category("AntiSnag")]
    public class TerrainRegressionTests : TerrainTestFixture
    {
        [TearDown]
        public void TearDown() => TearDownScene();


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
            foreach (var w in Wheels)
            {
                if (w.IsOnGround)
                {
                    avgSpringLen += w.LastSpringLen;
                    groundedWheels++;
                }
            }

            Assert.Greater(groundedWheels, 0, "Regression: At least one wheel must be on flat ground after settling.");

            avgSpringLen /= groundedWheels;
            // Measured settled spring length ~0.201m — lower than the serialized _restDistance (0.25m)
            // due to spring strength/mass/gravity equilibrium (sag = mg/4k = 0.049m). Matches VehicleIntegrationTests constant.
            const float k_ExpectedRestLen = 0.201f;
            const float k_RestTolerance = 0.05f;
            Assert.AreEqual(k_ExpectedRestLen, avgSpringLen, k_RestTolerance,
                $"Regression: Spring length after settling should be near {k_ExpectedRestLen}m (equilibrium). " +
                $"Actual avg: {avgSpringLen:F4}m. SphereCast must not alter settled spring length.");

            // Apply motor force to a rear wheel to verify car moves forward (not backward)
            var rearWheels = new List<R8EOX.Vehicle.RaycastWheel>();
            foreach (var w in Wheels)
                if (w.IsMotor) rearWheels.Add(w);

            float forcePerWheel = rearWheels.Count > 0 ? 26f / rearWheels.Count : 0f;
            foreach (var w in rearWheels)
                w.MotorForceShare = forcePerWheel;

            var startPos = Car.transform.position;
            yield return WaitPhysicsFrames(k_SettleFrames);

            float forwardDelta = Car.transform.position.z - startPos.z;
            Assert.Greater(forwardDelta, 0.01f,
                $"Regression: Car should move forward under motor force. " +
                $"Forward delta: {forwardDelta:F4}m. SphereCast must not break motor drive.");

            // Clear motor
            foreach (var w in rearWheels)
                w.MotorForceShare = 0f;
        }
    }
}
