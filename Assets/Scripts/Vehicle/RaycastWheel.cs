using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Per-wheel physics: suspension spring, lateral grip, longitudinal friction, motor force.
    /// Attach as MonoBehaviour on each wheel pivot GameObject (child of RCCar root).
    /// Each wheel pivot should have a child "WheelVisual" with the tire mesh.
    /// </summary>
    public class RaycastWheel : MonoBehaviour
    {
        // ---- Constants ----

        const float k_DebugScale = 0.024f;
        const float k_MinSpeedForGrip = 0.1f;
        const float k_StaticFrictionSpeed = 0.5f;
        const float k_StaticFrictionTraction = 5.0f;
        const float k_DroopSpeed = 20f;
        const float k_RpmConversion = 60f / (2f * Mathf.PI);


        // ---- Serialized Fields ----

        [Header("Suspension")]
        [Tooltip("Suspension rest distance in metres")]
        [SerializeField] private float _restDistance = 0.20f;
        [Tooltip("Extra droop extension when airborne in metres")]
        [SerializeField] private float _overExtend = 0.08f;
        [Tooltip("Maximum suspension force clamp in Newtons")]
        [SerializeField] private float _maxSpringForce = 50f;
        [Tooltip("Bump stop minimum spring length in metres")]
        [SerializeField] private float _minSpringLen = 0.032f;

        [Header("Wheel")]
        [Tooltip("Tire radius in metres (1/10th scale)")]
        [SerializeField] private float _wheelRadius = 0.166f;

        [Header("Motor/Steer")]
        [Tooltip("Whether this wheel receives motor force")]
        [SerializeField] private bool _isMotor;
        [Tooltip("Whether this wheel steers")]
        [SerializeField] private bool _isSteer;

        [Header("Traction")]
        [Tooltip("Longitudinal slip traction factor")]
        [SerializeField] private float _zTraction = 0.10f;
        [Tooltip("Braking friction boost factor")]
        [SerializeField] private float _zBrakeTraction = 0.5f;
        [Tooltip("Grip curve mapping slip ratio to grip factor")]
        [SerializeField] private AnimationCurve _gripCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 0.8f),
            new Keyframe(0.4f, 1.0f),
            new Keyframe(1.0f, 0.7f)
        );

        [Header("Ground Detection")]
        [Tooltip("Layer mask for ground raycast")]
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Debug")]
        [Tooltip("Show force debug arrows in Scene view")]
        [SerializeField] private bool _showDebug;


        // ---- Public Properties (set by RCCar) ----

        public float SpringStrength { get; set; } = 75f;
        public float SpringDamping { get; set; } = 4.25f;
        public float GripCoeff { get; set; } = 0.7f;
        public LayerMask GroundMask { get => _groundMask; set => _groundMask = value; }
        public bool ShowDebug { get => _showDebug; set => _showDebug = value; }
        public bool IsMotor { get => _isMotor; set => _isMotor = value; }
        public bool IsSteer { get => _isSteer; set => _isSteer = value; }
        public bool IsBraking { get; set; }
        public float MotorForceShare { get; set; }


        // ---- Public Properties (read by telemetry) ----

        public float GripFactor { get; private set; }
        public float SlipRatio { get; private set; }
        public bool IsOnGround { get; private set; }
        public float WheelRpm { get; private set; }
        public float LastSpringLen { get; private set; }
        public float LastGripLoad { get; private set; }


        // ---- Private Fields ----

        private Transform _wheelVisual;
        private Transform _hubVisual;
        private RCCar _cachedCar;
        private float _prevSpringLen;
        private float _rayLen;

        // Per-frame physics state
        private Vector3 _contactPoint;
        private Vector3 _contactNormal;
        private float _springLen;
        private float _springForce;
        private float _suspensionForce;
        private float _gripLoad;
        private Vector3 _tireVelocity;
        private float _speed;
        private float _fSpeed;
        private Vector3 _yForce;
        private Vector3 _xForce;
        private Vector3 _zForce;
        private Vector3 _motorForce;


        // ---- Unity Lifecycle ----

        void Awake()
        {
            _rayLen = Physics.SuspensionMath.ComputeRayLength(_restDistance, _overExtend, _wheelRadius);
            _prevSpringLen = _restDistance;

            Transform wm = transform.Find("WheelVisual");
            if (wm != null) _wheelVisual = wm;
            Transform hm = transform.Find("HubVisual");
            if (hm != null) _hubVisual = hm;
        }


        // ---- Public API ----

        /// <summary>
        /// Main physics entry point. Called by RCCar each FixedUpdate.
        /// Computes suspension, grip, friction, and motor forces, then applies
        /// the composite force to the car Rigidbody at the ground contact point.
        /// </summary>
        public void ApplyWheelPhysics(Rigidbody carRb, float dt)
        {
            if (_cachedCar == null)
                _cachedCar = carRb.GetComponent<RCCar>();

            _rayLen = Physics.SuspensionMath.ComputeRayLength(
                _restDistance, _overExtend, _wheelRadius);

            Ray ray = new Ray(transform.position, -transform.up);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, _rayLen, _groundMask))
            {
                HandleAirborne(dt);
                return;
            }

            if (hit.normal.y < 0f)
            {
                IsOnGround = false;
                _prevSpringLen = _restDistance + _overExtend;
                return;
            }

            IsOnGround = true;
            _contactPoint = hit.point;
            _contactNormal = hit.normal;

            ComputeSuspension(carRb, dt);
            ComputeLateralForce();
            ComputeLongitudinalForce(carRb);
            ComputeMotorForce();

            Vector3 totalForce = _yForce + _xForce + _zForce + _motorForce;
            carRb.AddForceAtPosition(totalForce, _contactPoint);

            UpdateVisuals(dt);

            WheelRpm = Physics.GripMath.ComputeWheelRpm(_fSpeed, _wheelRadius);

            if (_showDebug)
                DrawDebug();
        }


        // ---- Private Methods ----

        private void HandleAirborne(float dt)
        {
            GripFactor = 0f;
            SlipRatio = 0f;
            IsOnGround = false;
            _prevSpringLen = _restDistance + _overExtend;

            float droopTarget = -(_restDistance + _overExtend);
            if (_wheelVisual != null)
                _wheelVisual.localPosition = new Vector3(0f,
                    Mathf.MoveTowards(_wheelVisual.localPosition.y, droopTarget, k_DroopSpeed * dt), 0f);
            if (_hubVisual != null)
                _hubVisual.localPosition = new Vector3(0f,
                    Mathf.MoveTowards(_hubVisual.localPosition.y, droopTarget, k_DroopSpeed * dt), 0f);
        }

        private void ComputeSuspension(Rigidbody carRb, float dt)
        {
            float anchorToContact = Vector3.Distance(transform.position, _contactPoint);
            _springLen = Physics.SuspensionMath.ComputeSpringLength(anchorToContact, _wheelRadius, _minSpringLen);

            _springForce = SpringStrength * (_restDistance - _springLen);
            _suspensionForce = Physics.SuspensionMath.ComputeSuspensionForceWithDamping(
                SpringStrength, SpringDamping, _restDistance, _springLen, _prevSpringLen, dt);
            _prevSpringLen = _springLen;

            _yForce = _contactNormal * _suspensionForce;

            _tireVelocity = carRb.GetPointVelocity(_contactPoint);
            _speed = _tireVelocity.magnitude;
            _fSpeed = Vector3.Dot(transform.forward, _tireVelocity);

            _gripLoad = Physics.SuspensionMath.ComputeGripLoad(
                SpringStrength, _restDistance, _springLen, _maxSpringForce);

            LastSpringLen = _springLen;
            LastGripLoad = _gripLoad;
        }

        private void ComputeLateralForce()
        {
            Vector3 steerSideDir = transform.right;
            float lateralVel = Vector3.Dot(steerSideDir, _tireVelocity);

            if (_speed < k_MinSpeedForGrip || _gripCurve == null)
            {
                GripFactor = 0f;
                SlipRatio = 0f;
                _xForce = Vector3.zero;
                return;
            }

            SlipRatio = Physics.GripMath.ComputeSlipRatio(lateralVel, _speed);
            GripFactor = _gripCurve.Evaluate(SlipRatio);

            float lateralForceMag = Physics.GripMath.ComputeLateralForceMagnitude(
                lateralVel, GripFactor, GripCoeff, _gripLoad);
            _xForce = steerSideDir * lateralForceMag;
        }

        private void ComputeLongitudinalForce(Rigidbody carRb)
        {
            float engineForce = _cachedCar != null ? _cachedCar.CurrentEngineForce : 0f;
            float effectiveZTraction = Physics.GripMath.ComputeEffectiveTraction(
                IsBraking, _fSpeed, engineForce,
                _zTraction, _zBrakeTraction,
                k_StaticFrictionSpeed, k_StaticFrictionTraction);

            float longForceMag = Physics.GripMath.ComputeLongitudinalForceMagnitude(
                _fSpeed, effectiveZTraction, GripCoeff, _gripLoad);
            _zForce = carRb.transform.forward * longForceMag;

            // Ramp sliding fix: cancel the spring's horizontal component when stopped
            if (Mathf.Abs(_fSpeed) < k_StaticFrictionSpeed)
            {
                _xForce.x -= _contactNormal.x * _suspensionForce;
                _zForce.z -= _contactNormal.z * _suspensionForce;
            }
        }

        private void ComputeMotorForce()
        {
            _motorForce = Vector3.zero;
            if (_isMotor && MotorForceShare != 0f)
                _motorForce = transform.forward * MotorForceShare;
        }

        private void UpdateVisuals(float dt)
        {
            float spinAngle = _fSpeed / _wheelRadius * dt * Mathf.Rad2Deg;

            if (_wheelVisual != null)
            {
                _wheelVisual.localPosition = new Vector3(0f, -_springLen, 0f);
                _wheelVisual.Rotate(transform.right, spinAngle, Space.World);
            }
            if (_hubVisual != null)
            {
                _hubVisual.localPosition = new Vector3(0f, -_springLen, 0f);
                _hubVisual.Rotate(transform.right, spinAngle, Space.World);
            }
        }

        private void DrawDebug()
        {
            if (!IsOnGround) return;

            Debug.DrawLine(transform.position, _contactPoint, Color.white);

            if (_yForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(_contactPoint, _yForce * k_DebugScale, Color.yellow);
            if (_xForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(_contactPoint, _xForce * k_DebugScale, Color.red);
            if (_zForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(_contactPoint, _zForce * k_DebugScale, Color.green);
            if (_motorForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(_contactPoint, _motorForce * k_DebugScale, Color.cyan);
        }
    }
}