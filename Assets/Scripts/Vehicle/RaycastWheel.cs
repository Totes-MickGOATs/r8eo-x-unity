using UnityEngine;
using PhysicsMath = R8EOX.Vehicle.Physics;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Per-wheel physics: suspension spring, lateral grip, longitudinal friction, motor force.
    /// Attach as MonoBehaviour on each wheel pivot GameObject (child of RCCar root).
    /// Each wheel pivot should have a child "WheelVisual" with the tire mesh.
    /// Force computation is delegated to <see cref="PhysicsMath.WheelForceSolver.Solve"/>.
    /// </summary>
    public class RaycastWheel : MonoBehaviour
    {
        // ---- Constants ----

        const float k_DebugScale = 0.024f;
        const float k_DroopSpeed = 200f;
        // k_SphereCastRadius: sphere radius for SphereCast ground detection (~150mm tire contact patch, anti-snag).
        private const float k_SphereCastRadius = 0.15f;
        /// <summary>Public accessor for the SphereCast radius. Used by tests to validate the constant.</summary>
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
            new Keyframe(0f, 0f), new Keyframe(0.15f, 0.8f),
            new Keyframe(0.4f, 1.0f), new Keyframe(1.0f, 0.7f));
        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [Header("Debug")]
        [SerializeField] private bool _showDebug;

        // ---- Public Properties ----

        public float SpringStrength { get; set; } = 750.0f;
        public float SpringDamping  { get; set; } = 42.5f;
        public float GripCoeff      { get; set; } = 0.7f;
        public LayerMask GroundMask { get => _groundMask;  set => _groundMask  = value; }
        public bool ShowDebug       { get => _showDebug;   set => _showDebug   = value; }
        public bool IsMotor         { get => _isMotor;     set => _isMotor     = value; }
        public bool IsSteer         { get => _isSteer;     set => _isSteer     = value; }
        public bool IsBraking       { get; set; }
        public float MotorForceShare { get; set; }
        public float RestDistance   { get => _restDistance;  set => _restDistance  = value; }
        public float ZTraction      { get => _zTraction;     set => _zTraction     = value; }
        public float ZBrakeTraction { get => _zBrakeTraction; set => _zBrakeTraction = value; }

        // ---- Public Properties (read by telemetry / diagnostics) ----

        public float GripFactor    { get; private set; }
        public float SlipRatio     { get; private set; }
        public bool IsOnGround     { get; private set; }
        public float WheelRpm      { get; private set; }
        public float LastSpringLen { get; private set; }
        public float LastGripLoad  { get; private set; }
        public Vector3 ContactPoint  => _contactPoint;
        public Vector3 ContactNormal => _contactNormal;
        /// <summary>Suspension force magnitude (N) from the most recent frame.</summary>
        public float SuspensionForce => _lastResult.SuspensionForceMag;
        public Vector3 TireVelocity  => _tireVelocity;

        // ---- Private Fields ----

#if UNITY_EDITOR || DEBUG
        float _debugLogTimer;
#endif
        private Transform _wheelVisual;
        private Transform _hubVisual;
        private RCCar _cachedCar;
        private float _prevSpringLen, _rayLen;
        private bool _wasOnGround;
        private Vector3 _contactPoint;
        private Vector3 _contactNormal;
        private Vector3 _tireVelocity;
        private PhysicsMath.WheelForceResult _lastResult;

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
        /// Main physics entry point (called by RCCar each FixedUpdate).
        /// Builds <see cref="PhysicsMath.WheelForceInput"/>, calls <see cref="PhysicsMath.WheelForceSolver.Solve"/>,
        /// then applies the composite force to the Rigidbody at the contact point.
        /// </summary>
        public void ApplyWheelPhysics(Rigidbody carRb, float dt)
        {
            if (_cachedCar == null) _cachedCar = carRb.GetComponent<RCCar>();
            _rayLen = PhysicsMath.SuspensionMath.ComputeRayLength(
                _restDistance, _overExtend, _wheelRadius);

            // SphereCast averages contact normals over the tire contact patch (anti-snag).
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

            _tireVelocity = carRb.GetPointVelocity(_contactPoint);
            float engineForce = _cachedCar != null ? _cachedCar.CurrentEngineForce : 0f;

            var input = new PhysicsMath.WheelForceInput(
                SpringStrength, SpringDamping, _restDistance, _minSpringLen, _maxSpringForce,
                _wheelRadius, GripCoeff, _zTraction, _zBrakeTraction, _gripCurve,
                _isMotor, IsBraking, MotorForceShare,
                Vector3.Distance(transform.position, _contactPoint),
                _contactNormal, _contactPoint, _tireVelocity,
                transform.forward, transform.right,
                _prevSpringLen, wasGroundedLastFrame, dt, engineForce);

            _lastResult   = PhysicsMath.WheelForceSolver.Solve(in input);
            _prevSpringLen = _lastResult.SpringLen;
            GripFactor    = _lastResult.GripFactor;
            SlipRatio     = _lastResult.SlipRatio;
            LastSpringLen = _lastResult.SpringLen;
            LastGripLoad  = _lastResult.GripLoad;

            carRb.AddForceAtPosition(_lastResult.TotalForce, _contactPoint);
            UpdateVisuals(dt);
            WheelRpm     = PhysicsMath.GripMath.ComputeWheelRpm(_lastResult.ForwardSpeed, _wheelRadius);
            _wasOnGround = IsOnGround;

#if UNITY_EDITOR || DEBUG
            _debugLogTimer += dt;
            if (_debugLogTimer >= 0.5f)
            {
                Debug.Log($"[suspension] wheel={name} springLen={_lastResult.SpringLen:F4}m suspForce={_lastResult.SuspensionForceMag:F2}N gripLoad={_lastResult.GripLoad:F3}");
                Debug.Log($"[grip] wheel={name} slip={SlipRatio:F4} gripFactor={GripFactor:F3} lat={_lastResult.LateralForce.magnitude:F2}N long={_lastResult.LongitudinalForce.magnitude:F2}N motor={_lastResult.MotorForce.magnitude:F2}N");
                _debugLogTimer = 0f;
            }
#endif

            if (_showDebug)
                DrawDebug();
        }

        // ---- Private Methods ----

        private void UpdateVisuals(float dt)
        {
            float spinAngle = _lastResult.ForwardSpeed / _wheelRadius * dt * Mathf.Rad2Deg;

            if (_wheelVisual != null)
            {
                _wheelVisual.localPosition = new Vector3(0f, -_lastResult.SpringLen, 0f);
                _wheelVisual.Rotate(transform.right, spinAngle, Space.World);
            }
            if (_hubVisual != null)
            {
                _hubVisual.localPosition = new Vector3(0f, -_lastResult.SpringLen, 0f);
                _hubVisual.Rotate(transform.right, spinAngle, Space.World);
            }
        }

        private void DrawDebug()
        {
            if (!IsOnGround) return;

            Debug.DrawLine(transform.position, _contactPoint, Color.white);

            if (_lastResult.SuspensionForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(_contactPoint, _lastResult.SuspensionForce * k_DebugScale, Color.yellow);
            if (_lastResult.LateralForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(_contactPoint, _lastResult.LateralForce * k_DebugScale, Color.red);
            if (_lastResult.LongitudinalForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(_contactPoint, _lastResult.LongitudinalForce * k_DebugScale, Color.green);
            if (_lastResult.MotorForce.sqrMagnitude > 0.0001f)
                Debug.DrawRay(_contactPoint, _lastResult.MotorForce * k_DebugScale, Color.cyan);
        }
    }
}
