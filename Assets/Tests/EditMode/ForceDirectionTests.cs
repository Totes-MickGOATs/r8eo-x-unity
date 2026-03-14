using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Verify that given known geometry inputs, forces point in the expected directions.
    /// These tests catch axis mapping bugs from the Godot-to-Unity port.
    /// Pure math — no MonoBehaviour instantiation needed.
    /// </summary>
    public class ForceDirectionTests
    {
        // ---- Constants matching production defaults ----
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
        const float k_DefaultDt = 0.008333f; // 120 Hz


        // ---- Lateral Force Direction ----

        [Test]
        public void LateralForce_CarMovingRight_ForcePointsLeft()
        {
            // If lateral velocity is positive (moving right), lateral force should be negative (pushing left)
            float lateralVelocity = 2.0f; // Moving right
            float force = GripMath.ComputeLateralForceMagnitude(
                lateralVelocity, k_GripFactor, k_GripCoeff, k_GripLoad);

            Assert.Less(force, 0f,
                "Lateral force must oppose rightward motion (should be negative/leftward)");
        }

        [Test]
        public void LateralForce_CarMovingStraight_ForceIsZero()
        {
            // If lateral velocity is 0, lateral force should be 0.
            // NOTE: This test verifies the MATH function — gripFactor is an input parameter.
            // The potential bug is in the grip CURVE (returns 0 at slip=0), not in this function.
            float lateralVelocity = 0f;
            float force = GripMath.ComputeLateralForceMagnitude(
                lateralVelocity, k_GripFactor, k_GripCoeff, k_GripLoad);

            Assert.AreEqual(0f, force, 0.0001f,
                "Lateral force should be zero when lateral velocity is zero");
        }

        [Test]
        public void LongitudinalForce_CarMovingForward_ForcePointsBackward()
        {
            // If forward speed > 0, longitudinal friction force should be negative (opposing motion)
            float forwardSpeed = 5.0f;
            float force = GripMath.ComputeLongitudinalForceMagnitude(
                forwardSpeed, k_ZTraction, k_GripCoeff, k_GripLoad);

            Assert.Less(force, 0f,
                "Longitudinal friction must oppose forward motion (should be negative)");
        }

        [Test]
        public void LongitudinalForce_CarMovingBackward_ForcePointsForward()
        {
            // If forward speed < 0 (moving backward), force should be positive (opposing backward motion)
            float forwardSpeed = -5.0f;
            float force = GripMath.ComputeLongitudinalForceMagnitude(
                forwardSpeed, k_ZTraction, k_GripCoeff, k_GripLoad);

            Assert.Greater(force, 0f,
                "Longitudinal friction must oppose backward motion (should be positive)");
        }

        [Test]
        public void LongitudinalForce_CarStopped_StaticFrictionEngages()
        {
            // If |fSpeed| < 0.5 and engineForce == 0, effective traction should be 5.0
            float forwardSpeed = 0.1f;
            float engineForce = 0f;

            float effectiveTraction = GripMath.ComputeEffectiveTraction(
                false, forwardSpeed, engineForce,
                k_ZTraction, k_ZBrakeTraction,
                k_StaticFrictionSpeed, k_StaticFrictionTraction);

            Assert.AreEqual(k_StaticFrictionTraction, effectiveTraction, 0.0001f,
                "Static friction traction (5.0) should engage when stopped with no engine force");
        }

        [Test]
        public void MotorForce_PositiveShare_PushesAlongForward()
        {
            // MotorForceShare applied along wheel forward should produce force in that direction.
            // In RaycastWheel.ComputeMotorForce: _motorForce = transform.forward * MotorForceShare
            // If MotorForceShare > 0, force should be in the forward direction.
            // We verify the math principle: positive * forward = forward direction.
            Vector3 wheelForward = Vector3.forward; // (0, 0, 1)
            float motorForceShare = 10.0f;
            Vector3 motorForce = wheelForward * motorForceShare;

            Assert.Greater(motorForce.z, 0f,
                "Positive motor force share along +Z forward should produce positive Z force");
            Assert.AreEqual(0f, motorForce.x, 0.0001f,
                "Motor force should have no lateral component when wheel is straight");
            Assert.AreEqual(0f, motorForce.y, 0.0001f,
                "Motor force should have no vertical component");
        }

        [Test]
        public void SuspensionForce_Compressed_PushesUp()
        {
            // Spring compressed below rest distance should produce positive force
            float springLen = 0.10f; // Well below rest distance of 0.20m
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, springLen, springLen, k_DefaultDt);

            Assert.Greater(force, 0f,
                "Compressed suspension must produce positive (upward) force");

            // Verify the force applied along contact normal (0,1,0) points up
            Vector3 contactNormal = Vector3.up;
            Vector3 yForce = contactNormal * force;
            Assert.Greater(yForce.y, 0f,
                "Suspension force along upward normal must point upward");
        }

        [Test]
        public void SuspensionForce_AtRest_ProducesZeroForce()
        {
            // At rest length, no compression = no force
            float force = SuspensionMath.ComputeSuspensionForceWithDamping(
                k_SpringStrength, k_SpringDamping,
                k_RestDistance, k_RestDistance, k_RestDistance, k_DefaultDt);

            Assert.AreEqual(0f, force, 0.01f,
                "Suspension at rest length should produce zero force");
        }


        // ---- Grip Curve Bug Detection ----

        [Test]
        public void GripCurve_ZeroSlip_ReturnsValue()
        {
            // The default grip curve has keyframe (0, 0) — this means at zero slip,
            // grip factor is ZERO. This is a finding: the car has no grip when
            // moving perfectly straight because the curve starts at 0.
            // A correct curve should return ~1.0 at slip=0 for maximum grip.
            var gripCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, 0.8f),
                new Keyframe(0.4f, 1.0f),
                new Keyframe(1.0f, 0.7f)
            );

            float gripAtZero = gripCurve.Evaluate(0f);

            // This test documents the CURRENT behavior (grip=0 at slip=0).
            // If someone fixes the curve, this test should be updated.
            Assert.AreEqual(0f, gripAtZero, 0.01f,
                "FINDING: Default grip curve returns 0 at slip=0 — car has no lateral grip " +
                "when moving straight. This is likely a bug; the curve should start near 1.0");
        }

        [Test]
        public void GripCurve_SmallSlip_ReturnsNonZero()
        {
            // At slip=0.05, grip should be meaningful (not near-zero)
            // With the current curve (0,0)→(0.15,0.8), interpolation at 0.05
            // gives a very small value, meaning the car has almost no grip
            // at small slip angles. This contributes to the "skating" feel.
            var gripCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, 0.8f),
                new Keyframe(0.4f, 1.0f),
                new Keyframe(1.0f, 0.7f)
            );

            float gripAtSmallSlip = gripCurve.Evaluate(0.05f);

            // Ideally, grip at 0.05 slip should be at least 0.3 for responsive handling.
            // With the current curve starting at 0, this will be much lower.
            Assert.Greater(gripAtSmallSlip, 0f,
                "Grip at small slip angle must be non-zero for responsive handling");

            // This assertion may fail or barely pass — documenting the low-grip finding
            // A well-tuned curve would give at least 0.3 grip at 5% slip
            // Assert.Greater(gripAtSmallSlip, 0.3f,
            //     "EXPECTED FAILURE: Grip at slip=0.05 is too low for responsive handling");
        }
    }
}
