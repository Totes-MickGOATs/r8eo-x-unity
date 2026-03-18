using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Pure static solver that consolidates suspension, lateral grip, longitudinal friction,
    /// and motor force computation that previously lived as four private methods in RaycastWheel.
    /// No MonoBehaviour dependency — fully unit-testable without a running Unity scene.
    /// </summary>
    public static class WheelForceSolver
    {
        /// <summary>Below this speed the lateral grip calculation is skipped (avoids divide-near-zero).</summary>
        const float k_MinSpeedForGrip = 0.1f;
        /// <summary>Forward-speed threshold below which static friction is applied.</summary>
        const float k_StaticFrictionSpeed = 0.5f;
        /// <summary>Traction multiplier for the static-friction (ramp-hold) regime.</summary>
        const float k_StaticFrictionTraction = 5.0f;

        /// <summary>
        /// Compute all wheel forces for one FixedUpdate step.
        /// </summary>
        /// <param name="input">Fully-populated <see cref="WheelForceInput"/> for this wheel this frame.</param>
        /// <returns>
        /// <see cref="WheelForceResult"/> containing force vectors and diagnostic scalars.
        /// Caller is responsible for adding <see cref="WheelForceResult.TotalForce"/> to the Rigidbody.
        /// </returns>
        public static WheelForceResult Solve(in WheelForceInput input)
        {
            var result = new WheelForceResult();

            // ---- Suspension ----
            result.SpringLen = SuspensionMath.ComputeSpringLength(
                input.AnchorToContact, input.WheelRadius, input.MinSpringLen);

            float effectivePrev = SuspensionMath.SanitizePrevSpringLenForLanding(
                result.SpringLen, input.PrevSpringLen, input.WasGroundedLastFrame);

            result.SuspensionForceMag = SuspensionMath.ComputeSuspensionForceWithDamping(
                input.SpringStrength, input.SpringDamping, input.RestDistance,
                result.SpringLen, effectivePrev, input.Dt);

            result.SuspensionForce = input.ContactNormal * result.SuspensionForceMag;

            Vector3 tireVel = input.TireVelocity;
            result.Speed = tireVel.magnitude;
            result.ForwardSpeed = Vector3.Dot(input.WheelForward, tireVel);

            result.GripLoad = SuspensionMath.ComputeGripLoadFromSuspensionForce(
                result.SuspensionForceMag, input.MaxSpringForce);

            // ---- Lateral grip ----
            float lateralVel = Vector3.Dot(input.WheelRight, tireVel);
            if (result.Speed >= k_MinSpeedForGrip && input.GripCurve != null)
            {
                result.SlipRatio = GripMath.ComputeSlipRatio(lateralVel, result.Speed);
                result.GripFactor = input.GripCurve.Evaluate(result.SlipRatio);
                float latForceMag = GripMath.ComputeLateralForceMagnitude(
                    lateralVel, result.GripFactor, input.GripCoeff, result.GripLoad);
                result.LateralForce = input.WheelRight * latForceMag;
            }

            // ---- Longitudinal friction ----
            float effectiveZTraction = GripMath.ComputeEffectiveTraction(
                input.IsBraking, result.ForwardSpeed, input.CurrentEngineForce,
                input.ZTraction, input.ZBrakeTraction,
                k_StaticFrictionSpeed, k_StaticFrictionTraction);

            float longForceMag = GripMath.ComputeLongitudinalForceMagnitude(
                result.ForwardSpeed, effectiveZTraction, input.GripCoeff, result.GripLoad);
            result.LongitudinalForce = input.WheelForward * longForceMag;

            // Ramp sliding fix: cancel the spring's horizontal component when stopped.
            // Use proper vector subtraction so this works regardless of car rotation.
            if (Mathf.Abs(result.ForwardSpeed) < k_StaticFrictionSpeed)
            {
                Vector3 springHoriz = new Vector3(result.SuspensionForce.x, 0f, result.SuspensionForce.z);
                result.LateralForce -= springHoriz;
            }

            // ---- Motor ----
            if (input.IsMotor && input.MotorForceShare != 0f)
                result.MotorForce = input.WheelForward * input.MotorForceShare;

            result.TotalForce = result.SuspensionForce + result.LateralForce
                              + result.LongitudinalForce + result.MotorForce;

            return result;
        }
    }
}
