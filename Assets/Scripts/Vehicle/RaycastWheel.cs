using UnityEngine;
using PhysicsMath = R8EOX.Vehicle.Physics;
using R8EOX.Shared;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Per-wheel physics: suspension spring, lateral grip, longitudinal friction, motor force.
    /// Attach as MonoBehaviour on each wheel pivot GameObject (child of RCCar root).
    /// Inspector configuration is grouped in <see cref="WheelConfig"/>.
    /// Force computation is delegated to <see cref="PhysicsMath.WheelForceSolver.Solve"/>.
    /// </summary>
    public class RaycastWheel : MonoBehaviour
    {
        // SphereCast radius (~150mm tire contact patch, anti-snag).
        private const float k_SphereCastRadius = 0.15f;
        /// <summary>SphereCast radius accessor used by tests.</summary>
        public static float SphereCastRadius => k_SphereCastRadius;

        [SerializeField] WheelConfig _config = new WheelConfig();

        // ---- Runtime properties (overwritten by WheelManager at runtime) ----

        public float SpringStrength  { get; set; } = 750.0f;
        public float SpringDamping   { get; set; } = 42.5f;
        public float GripCoeff       { get; set; } = 0.7f;
        public bool  IsBraking       { get; set; }
        public float MotorForceShare { get; set; }

        // ---- Config pass-throughs ----

        public LayerMask GroundMask { get => _config.groundMask;   set => _config.groundMask   = value; }
        public bool ShowDebug       { get => _config.showDebug;    set => _config.showDebug    = value; }
        public bool IsMotor         { get => _config.isMotor;      set => _config.isMotor      = value; }
        public bool IsSteer         { get => _config.isSteer;      set => _config.isSteer      = value; }
        public float RestDistance   { get => _config.restDistance; set => _config.restDistance = value; }
        public float ZTraction      { get => _config.zTraction;    set => _config.zTraction    = value; }
        public float ZBrakeTraction { get => _config.zBrakeTraction; set => _config.zBrakeTraction = value; }

        // ---- Telemetry ----

        public float   GripFactor    { get; private set; }
        public float   SlipRatio     { get; private set; }
        public bool    IsOnGround    { get; private set; }
        public float   WheelRpm      { get; private set; }
        public float   LastSpringLen { get; private set; }
        public float   LastGripLoad  { get; private set; }
        public Vector3 ContactPoint  => _contactPoint;
        public Vector3 ContactNormal => _contactNormal;
        public float   SuspensionForce => _lastResult.SuspensionForceMag;
        public Vector3 TireVelocity  => _tireVelocity;

        // ---- Private state ----

        Transform _wheelVisual, _hubVisual;
        RCCar  _cachedCar;
        float  _prevSpringLen, _rayLen;
        bool   _wasOnGround;
        Vector3 _contactPoint, _contactNormal, _tireVelocity;
        PhysicsMath.WheelForceResult _lastResult;
#if UNITY_EDITOR || DEBUG
        float _debugLogTimer;
#endif

        void Awake()
        {
            _rayLen        = PhysicsMath.SuspensionMath.ComputeRayLength(
                                 _config.restDistance, _config.overExtend, _config.wheelRadius);
            _prevSpringLen = _config.restDistance;
            Transform wm   = transform.Find("WheelVisual"); if (wm != null) _wheelVisual = wm;
            Transform hm   = transform.Find("HubVisual");   if (hm != null) _hubVisual   = hm;
        }

        /// <summary>Main physics entry point — called by RCCar each FixedUpdate.</summary>
        public void ApplyWheelPhysics(Rigidbody carRb, float dt)
        {
            if (_cachedCar == null) _cachedCar = carRb.GetComponent<RCCar>();

            Vector3 rayOrigin = transform.position + transform.up * k_SphereCastRadius;
            RaycastHit hit;
            if (!UnityEngine.Physics.SphereCast(new Ray(rayOrigin, -transform.up),
                    k_SphereCastRadius, out hit, _rayLen, _config.groundMask))
            {
                GripFactor = 0f; SlipRatio = 0f; IsOnGround = false; _wasOnGround = false;
                _prevSpringLen = _config.restDistance + _config.overExtend;
                WheelVisuals.ApplyDroop(_wheelVisual, _hubVisual, _config.restDistance, _config.overExtend, dt);
                return;
            }
            if (hit.normal.y < 0f)
            {
                IsOnGround = false; _wasOnGround = false;
                _prevSpringLen = _config.restDistance + _config.overExtend;
                return;
            }

            bool wasGroundedLastFrame = _wasOnGround;
            IsOnGround = true; _contactPoint = hit.point; _contactNormal = hit.normal;
            _tireVelocity = carRb.GetPointVelocity(_contactPoint);
            float engineForce = _cachedCar != null ? _cachedCar.CurrentEngineForce : 0f;

            var input = new PhysicsMath.WheelForceInput(
                SpringStrength, SpringDamping,
                _config.restDistance, _config.minSpringLen, _config.maxSpringForce,
                _config.wheelRadius, GripCoeff, _config.zTraction, _config.zBrakeTraction,
                _config.gripCurve, _config.isMotor, IsBraking, MotorForceShare,
                Vector3.Distance(transform.position, _contactPoint),
                _contactNormal, _contactPoint, _tireVelocity,
                transform.forward, transform.right,
                _prevSpringLen, wasGroundedLastFrame, dt, engineForce);

            _lastResult    = PhysicsMath.WheelForceSolver.Solve(in input);
            _prevSpringLen = _lastResult.SpringLen;
            GripFactor     = _lastResult.GripFactor; SlipRatio  = _lastResult.SlipRatio;
            LastSpringLen  = _lastResult.SpringLen;  LastGripLoad = _lastResult.GripLoad;
            carRb.AddForceAtPosition(_lastResult.TotalForce, _contactPoint);
            WheelVisuals.UpdateGrounded(_wheelVisual, _hubVisual, transform,
                _lastResult.SpringLen, _lastResult.ForwardSpeed, _config.wheelRadius, dt);
            WheelRpm     = PhysicsMath.GripMath.ComputeWheelRpm(_lastResult.ForwardSpeed, _config.wheelRadius);
            _wasOnGround = IsOnGround;
#if UNITY_EDITOR || DEBUG
            _debugLogTimer += dt;
            if (_debugLogTimer >= 0.5f) {
                RuntimeLog.Log($"[suspension] wheel={name} springLen={_lastResult.SpringLen:F4}m suspForce={_lastResult.SuspensionForceMag:F2}N gripLoad={_lastResult.GripLoad:F3}");
                RuntimeLog.Log($"[grip] wheel={name} slip={SlipRatio:F4} gripFactor={GripFactor:F3} lat={_lastResult.LateralForce.magnitude:F2}N long={_lastResult.LongitudinalForce.magnitude:F2}N motor={_lastResult.MotorForce.magnitude:F2}N");
                _debugLogTimer = 0f; }
#endif
            if (_config.showDebug) WheelVisuals.DrawForces(transform, _contactPoint,
                _lastResult.SuspensionForce, _lastResult.LateralForce,
                _lastResult.LongitudinalForce, _lastResult.MotorForce);
        }
    }
}
