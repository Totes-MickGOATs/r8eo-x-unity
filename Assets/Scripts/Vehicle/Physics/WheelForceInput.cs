using UnityEngine;

namespace R8EOX.Vehicle.Physics
{
    /// <summary>
    /// Immutable value type bundling all per-wheel inputs required for one force-computation pass.
    /// Constructed by RaycastWheel each FixedUpdate from serialized config + live Rigidbody state,
    /// then passed to <see cref="WheelForceSolver.Solve"/>.
    /// </summary>
    public readonly struct WheelForceInput
    {
        public readonly float SpringStrength;
        public readonly float SpringDamping;
        public readonly float RestDistance;
        public readonly float MinSpringLen;
        public readonly float MaxSpringForce;
        public readonly float WheelRadius;
        public readonly float GripCoeff;
        public readonly float ZTraction;
        public readonly float ZBrakeTraction;
        public readonly AnimationCurve GripCurve;
        public readonly bool IsMotor;
        public readonly bool IsBraking;
        public readonly float MotorForceShare;
        public readonly float AnchorToContact;
        public readonly Vector3 ContactNormal;
        public readonly Vector3 ContactPoint;
        public readonly Vector3 TireVelocity;
        public readonly Vector3 WheelForward;
        public readonly Vector3 WheelRight;
        public readonly float PrevSpringLen;
        public readonly bool WasGroundedLastFrame;
        public readonly float Dt;
        public readonly float CurrentEngineForce;

        public WheelForceInput(
            float springStrength,
            float springDamping,
            float restDistance,
            float minSpringLen,
            float maxSpringForce,
            float wheelRadius,
            float gripCoeff,
            float zTraction,
            float zBrakeTraction,
            AnimationCurve gripCurve,
            bool isMotor,
            bool isBraking,
            float motorForceShare,
            float anchorToContact,
            Vector3 contactNormal,
            Vector3 contactPoint,
            Vector3 tireVelocity,
            Vector3 wheelForward,
            Vector3 wheelRight,
            float prevSpringLen,
            bool wasGroundedLastFrame,
            float dt,
            float currentEngineForce)
        {
            SpringStrength = springStrength;
            SpringDamping = springDamping;
            RestDistance = restDistance;
            MinSpringLen = minSpringLen;
            MaxSpringForce = maxSpringForce;
            WheelRadius = wheelRadius;
            GripCoeff = gripCoeff;
            ZTraction = zTraction;
            ZBrakeTraction = zBrakeTraction;
            GripCurve = gripCurve;
            IsMotor = isMotor;
            IsBraking = isBraking;
            MotorForceShare = motorForceShare;
            AnchorToContact = anchorToContact;
            ContactNormal = contactNormal;
            ContactPoint = contactPoint;
            TireVelocity = tireVelocity;
            WheelForward = wheelForward;
            WheelRight = wheelRight;
            PrevSpringLen = prevSpringLen;
            WasGroundedLastFrame = wasGroundedLastFrame;
            Dt = dt;
            CurrentEngineForce = currentEngineForce;
        }
    }
}
