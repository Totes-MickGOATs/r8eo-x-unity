using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for vehicle settlement, suspension, and zero-input behavior.
    ///
    /// Verifies:
    /// - Car settles to rest on flat ground
    /// - No phantom forces cause lateral drift with no input
    /// - Reverse ESC does not engage at startup
    /// - Suspension spring lengths reach expected rest distances
    /// </summary>
    public class VehicleSettlingTests
    {
        private readonly VehicleIntegrationHelper _h = new VehicleIntegrationHelper();

        [SetUp]    public void SetUp()    => _h.SetUp();
        [TearDown] public void TearDown() => _h.TearDown();


        [UnityTest]
        public IEnumerator Car_OnFlatGround_SettlesToRest()
        {
            // Spawn car 0.5m above flat ground
            // Run 120 physics frames
            // Assert velocity is near zero and car is above ground
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);

            Assert.Less(_h.CarRb.velocity.magnitude, 0.5f,
                "Car should settle to near-rest after 1 second on flat ground");

            float carY = _h.Car.transform.position.y;
            Assert.Greater(carY, 0f,
                "Car should be above the ground, not clipped through");
            Assert.Less(carY, 1.0f,
                "Car should have fallen from spawn height and settled");
        }

        [UnityTest]
        public IEnumerator Car_SuspensionSettles_NearRestDistance()
        {
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);

            foreach (var w in _h.Wheels)
            {
                if (w.IsOnGround)
                {
                    bool isFront = w.transform.localPosition.z > 0f;
                    float expectedRest = isFront
                        ? VehicleIntegrationHelper.k_FrontRestLen
                        : VehicleIntegrationHelper.k_RearRestLen;
                    string axle = isFront ? "front" : "rear";
                    Assert.AreEqual(expectedRest, w.LastSpringLen, VehicleIntegrationHelper.k_RestTolerance,
                        $"Wheel {w.name} ({axle}) spring length {w.LastSpringLen:F3}m should be near " +
                        $"rest distance {expectedRest}m (+/-{VehicleIntegrationHelper.k_RestTolerance}m). " +
                        "If too compressed or extended, suspension tuning or mass may be off");
                }
            }
        }

        [UnityTest]
        public IEnumerator Car_NoInput_DoesNotReverse()
        {
            // Critical bug test: with no input at all (null RCInput),
            // the car should NOT engage reverse. This was a reported startup bug.
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);

            Assert.IsFalse(_h.RcCar.ReverseEngaged,
                "Reverse should NOT engage with zero input at startup. " +
                "If it does, the reverse ESC state machine has a bug (e.g., " +
                "interpreting zero brake as a brake press when stopped)");

            Assert.AreEqual(0f, _h.RcCar.CurrentEngineForce, 0.01f,
                "Engine force should be zero with no input");
        }

        [UnityTest]
        public IEnumerator Car_NoInput_StaysNearOrigin()
        {
            // With no input the car should settle and remain still.
            // If it drifts, there's a phantom force bug.
            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);

            Vector3 posAfterSettle = _h.Car.transform.position;

            yield return VehicleIntegrationHelper.WaitPhysicsFrames(VehicleIntegrationHelper.k_SettleFrames);

            Vector3 posAfterWait = _h.Car.transform.position;
            float lateralDrift = new Vector2(
                posAfterWait.x - posAfterSettle.x,
                posAfterWait.z - posAfterSettle.z).magnitude;

            Assert.Less(lateralDrift, 0.1f,
                "Car should not drift laterally with no input. " +
                "If it drifts, there may be phantom forces from suspension " +
                "normal projection or asymmetric grip");
        }
    }
}
