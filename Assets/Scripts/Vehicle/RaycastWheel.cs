using UnityEngine;
using PhysicsMath = R8EOX.Vehicle.Physics;

namespace R8EOX.Vehicle
{
    /// <summary>Per-wheel physics: suspension, grip, friction, motor force. Attach on wheel pivot (child of RCCar).</summary>
    public class RaycastWheel : MonoBehaviour
    {
        // ---- Constants ----
        const float k_MinSpeedForGrip = 0.1f;
        const float k_StaticFrictionSpeed = 0.5f;
        const float k_StaticFrictionTraction = 5.0f;
        // SphereCast radius: averages contact normals over the tire patch (anti-snag, ~150 mm).
        private const float k_SphereCastRadius = 0.15f;
        /// <summary>Public accessor used by tests to validate the sphere-cast radius constant.</summary>
        public static float SphereCastRadius => k_SphereCastRadius;
        // ---- Serialized Fields ----

        [Header("Suspension")]
        [SerializeField] private float _restDistance = 0.25f;
        [SerializeField] private float _overExtend = 0.24f;
        [SerializeField] private float _maxSpringForce = 500f;
        [SerializeField] private float _minSpringLen = 0.12f;
        [Header("Wheel")]
        [SerializeField] private float _wheelRadius = 0.420f;
        [Header("Motor/Steer")]
        [SerializeField] private bool _isMotor;
        [SerializeField] private bool _isSteer;
        [Header("Traction")]
        [SerializeField] private float _zTraction = 0.10f;
        [SerializeField] private float _zBrakeTraction = 0.5f;
        [SerializeField] private AnimationCurve _gripCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 0.8f),
            new Keyframe(0.4f, 1.0f),
            new Keyframe(1.0f, 0.7f)
        );
        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [Header("Debug")]
        [SerializeField] private bool _showDebug;

        // ---- Public Properties ----

        public float SpringStrength { get; set; } = 750.0f;
        public float SpringDamping { get; set; } = 42.5f;
        public float GripCoeff { get; set; } = 0.7f;
        public LayerMask GroundMask { get => _groundMask; set => _groundMask = value; }
        public bool ShowDebug { get => _showDebug; set => _showDebug = value; }
        public bool IsMotor { get => _isMotor; set => _isMotor = value; }
        public bool IsSteer { get => _isSteer; set => _isSteer = value; }
        public bool IsBraking { get; set; }
        public float MotorForceShare { get; set; }
        public float RestDistance { get => _restDistance; set => _restDistance = value; }
        public float ZTraction { get => _zTraction; set => _zTraction = value; }
        public float ZBrakeTraction { get => _zBrakeTraction; set => _zBrakeTraction = value; }

        public float GripFactor { get; private set; }
        public float SlipRatio { get; private set; }
        public bool IsOnGround { get; private set; }
        public float WheelRpm { get; private set; }
        public float LastSpringLen { get; private set; }
        public float LastGripLoad { get; private set; }
        public Vector3 ContactPoint => _contactPoint;
        public Vector3 ContactNormal => _contactNormal;
        public float SuspensionForce => _suspensionForce;
        public Vector3 TireVelocity => _tireVelocity;

        // ---- Private Fields ----

        private Transform _wheelVisual, _hubVisual;
        private RCCar _cachedCar;
        private float _prevSpringLen, _rayLen;
        private bool _wasOnGround;
        private Vector3 _contactPoint, _contactNormal, _tireVelocity;
        private Vector3 _yForce, _xForce, _zForce, _motorForce;
        private float _springLen, _springForce, _suspensionForce, _gripLoad;
        private float _speed, _fSpeed;

        // ---- Unity Lifecycle ----

        void Awake()
        {
            _rayLen = PhysicsMath.SuspensionMath.ComputeRayLength(_restDistance, _overExtend, _wheelRadius);
            _prevSpringLen = _restDistance;
            Transform wm = transform.Find("WheelVisual");
            if (wm != null) _wheelVisual = wm;
            Transform hm = transform.Find("HubVisual");
            if (hm != null) _hubVisual = hm;
        }

        // ---- Public API ----

        /// <summary>
        /// Main physics entry point called by RCCar each FixedUpdate.
        /// Computes suspension, grip, friction, and motor forces and applies them to the Rigidbody.
        /// </summary>
        public void ApplyWheelPhysics(Rigidbody carRb, float dt)
        {
            if (_cachedCar == null) _cachedCar = carRb.GetComponent<RCCar>();
            _rayLen = PhysicsMath.SuspensionMath.ComputeRayLength(_restDistance, _overExtend, _wheelRadius);

            Vector3 rayOrigin = transform.position + transform.up * k_SphereCastRadius;
            RaycastHit hit;
            if (!UnityEngine.Physics.SphereCast(new Ray(rayOrigin, -transform.up),
                    k_SphereCastRadius, out hit, _rayLen, _groundMask))
            {
                GripFactor = 0f; SlipRatio = 0f; IsOnGround = false; _wasOnGround = false;
                _prevSpringLen = _restDistance + _overExtend;
                WheelVisuals.ApplyDroop(_wheelVisual, _hubVisual, _restDistance, _overExtend, dt);
                return;
            }
            if (hit.normal.y < 0f)
            {
                IsOnGround = false; _wasOnGround = false;
                _prevSpringLen = _restDistance + _overExtend;
                return;
            }

            bool wasGroundedLastFrame = _wasOnGround;
            IsOnGround = true;
            _contactPoint = hit.point;
            _contactNormal = hit.normal;

            ComputeSuspension(carRb, dt, wasGroundedLastFrame);
            ComputeLateralForce();
            ComputeLongitudinalForce(carRb);
            ComputeMotorForce();

            carRb.AddForceAtPosition(_yForce + _xForce + _zForce + _motorForce, _contactPoint);
            WheelVisuals.UpdateGrounded(_wheelVisual, _hubVisual, transform, _springLen, _fSpeed, _wheelRadius, dt);
            WheelRpm = PhysicsMath.GripMath.ComputeWheelRpm(_fSpeed, _wheelRadius);
            _wasOnGround = IsOnGround;

            if (_showDebug)
                WheelVisuals.DrawForces(transform, _contactPoint, _yForce, _xForce, _zForce, _motorForce);
        }

        // ---- Private Methods ----

        private void ComputeSuspension(Rigidbody carRb, float dt, bool wasGroundedLastFrame)
        {
            float anchorToContact = Vector3.Distance(transform.position, _contactPoint);
            _springLen = PhysicsMath.SuspensionMath.ComputeSpringLength(anchorToContact, _wheelRadius, _minSpringLen);
            _springForce = SpringStrength * (_restDistance - _springLen);
            float effectivePrev = PhysicsMath.SuspensionMath.SanitizePrevSpringLenForLanding(
                _springLen, _prevSpringLen, wasGroundedLastFrame);
            _suspensionForce = PhysicsMath.SuspensionMath.ComputeSuspensionForceWithDamping(
                SpringStrength, SpringDamping, _restDistance, _springLen, effectivePrev, dt);
            _prevSpringLen = _springLen;
            _yForce = _contactNormal * _suspensionForce;
            _tireVelocity = carRb.GetPointVelocity(_contactPoint);
            _speed = _tireVelocity.magnitude;
            _fSpeed = Vector3.Dot(transform.forward, _tireVelocity);
            _gripLoad = PhysicsMath.SuspensionMath.ComputeGripLoadFromSuspensionForce(_suspensionForce, _maxSpringForce);
            LastSpringLen = _springLen;
            LastGripLoad = _gripLoad;
        }

        private void ComputeLateralForce()
        {
            Vector3 steerSideDir = transform.right;
            float lateralVel = Vector3.Dot(steerSideDir, _tireVelocity);
            if (_speed < k_MinSpeedForGrip || _gripCurve == null)
            {
                GripFactor = 0f; SlipRatio = 0f; _xForce = Vector3.zero;
                return;
            }
            SlipRatio = PhysicsMath.GripMath.ComputeSlipRatio(lateralVel, _speed);
            GripFactor = _gripCurve.Evaluate(SlipRatio);
            _xForce = steerSideDir * PhysicsMath.GripMath.ComputeLateralForceMagnitude(
                lateralVel, GripFactor, GripCoeff, _gripLoad);
        }

        private void ComputeLongitudinalForce(Rigidbody carRb)
        {
            float engineForce = _cachedCar != null ? _cachedCar.CurrentEngineForce : 0f;
            float effectiveZTraction = PhysicsMath.GripMath.ComputeEffectiveTraction(
                IsBraking, _fSpeed, engineForce,
                _zTraction, _zBrakeTraction,
                k_StaticFrictionSpeed, k_StaticFrictionTraction);
            _zForce = transform.forward * PhysicsMath.GripMath.ComputeLongitudinalForceMagnitude(
                _fSpeed, effectiveZTraction, GripCoeff, _gripLoad);
            // Ramp-sliding fix: cancel the spring's horizontal component when stopped.
            if (Mathf.Abs(_fSpeed) < k_StaticFrictionSpeed)
            {
                Vector3 sh = _contactNormal * _suspensionForce;
                _xForce -= new Vector3(sh.x, 0f, sh.z);
            }
        }

        private void ComputeMotorForce()
        {
            _motorForce = (_isMotor && MotorForceShare != 0f)
                ? transform.forward * MotorForceShare
                : Vector3.zero;
        }
    }
}
