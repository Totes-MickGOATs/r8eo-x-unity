using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Verify lateral, longitudinal, motor, and suspension force directions.
    /// Pure math — no MonoBehaviour instantiation needed.
    /// </summary>
    public class ForceDirectionLateralTests
    {
        const float k_GripCoeff = 0.7f;
        const float k_GripLoad = 5.0f;
        const float k_GripFactor = 0.8f;
        const float k_ZTraction = 0.10f;
        const float k_ZBrakeTraction = 0.5f;
        const float k_StaticFrictionSpeed = 0.5f;
        const float k_StaticFrictionTraction = 5.0f;
        const float k_SpringStrength = 75f;
        const float k_SpringDamping = 4.25f;
        const float k_RestDistance = 0.20f;
        const float k_DefaultDt = 0.008333f;

        [Test]
        public void LateralForce_CarMovingRight_ForcePointsLeft()
        {
            float force = GripMath.ComputeLateralForceMagnitude(
                2.0f, k_GripFactor, k_GripCoeff, k_GripLoad);
            Assert.Less(force, 0f,
                "Lateral force must oppose rightward motion (should be negative/leftward)");
        }

        [Test]
        public void LateralForce_CarMovingStraight_ForceIsZero()
        {
            float force = GripMath.ComputeLateralForceMagnitude(
                0f, k_GripFactor, k_GripCoeff, k_GripLoad);
            Assert.AreEqual(0f, force, 0.0001f);
        }

        [Test]
        public void LongitudinalForce_CarMovingForward_ForcePointsBackward()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(
                5.0f, k_ZTraction, k_GripCoeff, k_GripLoad);
            Assert.Less(force, 0f,
                "Longitudinal friction must oppose forward motion (should be negative)");
        }

        [Test]
        public void LongitudinalForce_CarMovingBackward_ForcePointsForward()
        {
            float force = GripMath.ComputeLongitudinalForceMagnitude(
                -5.0f, k_ZTraction, k_GripCoeff, k_GripLoad);
            Assert.Greater(force, 0f,
                "Longitudinal friction must oppose backward motion (should be positive)");
        }

        [Test]
        public void LongitudinalForce_CarStopped_StaticFrictionEngages()
        {
            float effectiveTraction = GripMath.ComputeEffectiveTraction(
                false, 0.1f, 0f,
                k_ZTraction, k_ZBrakeTraction,
                k_StaticFrictionSpeed, k_StaticFrictionTraction);
            Assert.AreEqual(k_StaticFrictionTraction, effectiveTraction, 0.0001f);
        }

        [Test]
        public void MotorForce_PositiveShare_PushesAlongForward()
        {
            Vector3 wheelForward = Vector3.forward;
            float motorForceShare = 10.0f;
            Vector3 motorForce = wheelForward * motorForceShare;
            Assert.Greater(motorForce.z, 0f);
            Assert.AreEqual(0f, motorForce.x, 0.0001f);
            Assert.AreEqual(0f, motorForce.y, 0.0001f);
        }

        [Test]
        public void SuspensionForce_Compressed_PushesUp()
        {
            float springLen = 0.10f;
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, springLen, k_DefaultDt);
            Assert.Greater(force, 0f);
            Vector3 yForce = Vector3.up * force;
            Assert.Greater(yForce.y, 0f);
        }

        [Test]
        public void SuspensionForce_AtRest_ProducesZeroForce()
        {
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, k_RestDistance, k_RestDistance, k_DefaultDt);
            Assert.AreEqual(0f, force, 0.01f);
        }
    }
}
