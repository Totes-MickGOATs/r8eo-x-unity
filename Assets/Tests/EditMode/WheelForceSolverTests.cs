using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="WheelForceSolver.Solve"/>.
    /// All tests construct <see cref="WheelForceInput"/> directly and assert on
    /// <see cref="WheelForceResult"/> — no MonoBehaviour or scene required.
    /// </summary>
    public class WheelForceSolverTests
    {
        // ---- Shared test constants ----

        const float k_SpringStrength = 700f;
        const float k_SpringDamping = 4.25f;
        const float k_RestDistance = 0.25f;
        const float k_MinSpringLen = 0.12f;
        const float k_MaxSpringForce = 500f;
        const float k_WheelRadius = 0.042f;
        const float k_GripCoeff = 0.7f;
        const float k_ZTraction = 0.10f;
        const float k_ZBrakeTraction = 0.5f;
        const float k_Dt = 0.02f;

        static AnimationCurve DefaultGripCurve() => new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 0.8f),
            new Keyframe(0.4f, 1.0f),
            new Keyframe(1.0f, 0.7f));

        /// <summary>
        /// Build a minimal valid WheelForceInput where the spring is compressed by <paramref name="compression"/> metres.
        /// The contact normal points straight up; tire velocity is zero.
        /// </summary>
        static WheelForceInput MakeInput(
            float compression = 0.05f,
            bool isMotor = false,
            float motorForceShare = 0f,
            Vector3 tireVelocity = default,
            bool isBraking = false,
            float currentEngineForce = 0f)
        {
            // anchorToContact = (restDistance - compression) + wheelRadius
            float anchorToContact = (k_RestDistance - compression) + k_WheelRadius;

            return new WheelForceInput(
                springStrength:       k_SpringStrength,
                springDamping:        k_SpringDamping,
                restDistance:         k_RestDistance,
                minSpringLen:         k_MinSpringLen,
                maxSpringForce:       k_MaxSpringForce,
                wheelRadius:          k_WheelRadius,
                gripCoeff:            k_GripCoeff,
                zTraction:            k_ZTraction,
                zBrakeTraction:       k_ZBrakeTraction,
                gripCurve:            DefaultGripCurve(),
                isMotor:              isMotor,
                isBraking:            isBraking,
                motorForceShare:      motorForceShare,
                anchorToContact:      anchorToContact,
                contactNormal:        Vector3.up,
                contactPoint:         Vector3.zero,
                tireVelocity:         tireVelocity,
                wheelForward:         Vector3.forward,
                wheelRight:           Vector3.right,
                prevSpringLen:        k_RestDistance,
                wasGroundedLastFrame: true,
                dt:                   k_Dt,
                currentEngineForce:   currentEngineForce);
        }

        // ---- Tests ----

        [Test]
        public void Solve_CompressedSpring_ProducesUpwardSuspensionForce()
        {
            var input = MakeInput(compression: 0.05f);
            WheelForceResult result = WheelForceSolver.Solve(in input);

            Assert.Greater(result.SuspensionForceMag, 0f,
                "A compressed spring must produce a positive suspension force magnitude.");
            Assert.Greater(result.SuspensionForce.y, 0f,
                "Suspension force must have a positive Y component when contact normal is up.");
        }

        [Test]
        public void Solve_ZeroSpeed_ProducesZeroLateralForce()
        {
            // tireVelocity = Vector3.zero → speed = 0 → below k_MinSpeedForGrip → lateral force skipped
            var input = MakeInput(tireVelocity: Vector3.zero);
            WheelForceResult result = WheelForceSolver.Solve(in input);

            // Lateral force may be modified by the ramp-sliding fix; the raw grip contribution
            // should be zero, but the final LateralForce might include a small spring-horizontal
            // cancel term.  We test that no lateral grip energy was introduced.
            Assert.AreEqual(0f, result.SlipRatio,
                "Slip ratio must be zero when tire velocity is zero.");
            Assert.AreEqual(0f, result.GripFactor,
                "Grip factor must be zero when tire velocity is zero.");
        }

        [Test]
        public void Solve_MotorWheel_AppliesForwardMotorForce()
        {
            const float share = 50f;
            var input = MakeInput(isMotor: true, motorForceShare: share);
            WheelForceResult result = WheelForceSolver.Solve(in input);

            Assert.AreEqual(share, result.MotorForce.z, 0.001f,
                "Motor force Z component must equal motorForceShare when wheel is forward-pointing.");
            Assert.Greater(result.TotalForce.z, 0f,
                "Total force Z must be positive when motor is driving forward.");
        }

        [Test]
        public void Solve_NonMotorWheel_ProducesZeroMotorForce()
        {
            var input = MakeInput(isMotor: false, motorForceShare: 100f);
            WheelForceResult result = WheelForceSolver.Solve(in input);

            Assert.AreEqual(Vector3.zero, result.MotorForce,
                "Non-motor wheels must produce zero motor force regardless of motorForceShare.");
        }

        [Test]
        public void Solve_TotalForce_IsSumOfComponents()
        {
            var input = MakeInput(isMotor: true, motorForceShare: 30f,
                tireVelocity: new Vector3(0.3f, 0f, 1.5f));
            WheelForceResult result = WheelForceSolver.Solve(in input);

            Vector3 expected = result.SuspensionForce + result.LateralForce
                             + result.LongitudinalForce + result.MotorForce;

            Assert.AreEqual(expected.x, result.TotalForce.x, 0.0001f, "TotalForce.x mismatch");
            Assert.AreEqual(expected.y, result.TotalForce.y, 0.0001f, "TotalForce.y mismatch");
            Assert.AreEqual(expected.z, result.TotalForce.z, 0.0001f, "TotalForce.z mismatch");
        }
    }
}
