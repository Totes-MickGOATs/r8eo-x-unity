using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for wheel raycast and ground-contact behavior.
    ///
    /// Verifies:
    /// - Wheel raycasts hit the ground plane, not the car's own colliders
    /// - All four wheels make contact on flat ground
    /// - Grounded wheels report positive grip load
    /// </summary>
    public class VehicleWheelTests
    {
        private readonly VehicleIntegrationHelper _h = new VehicleIntegrationHelper();

        [SetUp]    public void SetUp()    => _h.SetUp();
        [TearDown] public void TearDown() => _h.TearDown();


        [UnityTest]
        public IEnumerator Car_WheelRaycasts_HitGround_NotSelf()
        {
            // Verify wheels detect the ground, not the car's own colliders
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);

            int groundedCount = 0;
            foreach (var w in _h.Wheels)
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
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);

            int groundedCount = 0;
            foreach (var w in _h.Wheels)
            {
                if (w.IsOnGround)
                    groundedCount++;
            }

            Assert.AreEqual(4, groundedCount,
                $"All 4 wheels should be on ground after settling on flat surface. " +
                $"Only {groundedCount} detected. " +
                "Check raycast length, wheel positions, and ground mask");

            // All grounded wheels should have positive grip load
            foreach (var w in _h.Wheels)
            {
                if (w.IsOnGround)
                {
                    Assert.Greater(w.LastGripLoad, 0f,
                        $"Wheel {w.name} is on ground but has zero grip load. " +
                        "Suspension may not be generating spring force");
                }
            }
        }
    }
}
