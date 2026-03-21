using UnityEngine;
using PhysicsMath = R8EOX.Vehicle.Physics;
using R8EOX.Shared;

namespace R8EOX.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public partial class RCCar : MonoBehaviour
    {
        const float k_DefaultMass = 15.0f, k_DefaultAngularDrag = 0.05f, k_DefaultBounciness = 0.05f;
        const float k_FlipHeightOffset = 14.0f, k_MsToKmh = 3.6f;
        const float k_ReverseSpeedThreshold = 0.25f, k_ForwardSpeedClearThreshold = 0.50f, k_ReverseBrakeMinThreshold = 0.1f;

        public enum MotorPreset { Motor21_5T, Motor17_5T, Motor13_5T, Motor9_5T, Motor5_5T, Motor1_5T, Custom }

        // State
        public float CurrentEngineForce { get; private set; } public float CurrentBrakeForce { get; private set; }
        public float SmoothThrottle     { get; private set; } public float CurrentSteering   { get; private set; }
        public bool  IsAirborne         { get; private set; } public float TumbleFactor       { get; private set; }
        public float TiltAngle          { get; private set; } public bool  ReverseEngaged     { get; private set; }
        public float ForwardSpeed       { get; private set; } public MotorPreset ActiveMotorPreset => _motorPreset;
        public RCAirPhysics AirPhysics  => _airPhysics;       public Drivetrain DrivetrainRef  => _drivetrain;
        public float Mass => _rb != null ? _rb.mass : k_DefaultMass;

        Rigidbody _rb; R8EOX.Input.IVehicleInput _input; RCAirPhysics _airPhysics; Drivetrain _drivetrain;
        WheelManager _wheels = new WheelManager(); TumbleController _tumble;
        AirborneDetector _airDetect = new AirborneDetector(5); bool _flipRequested;
#if UNITY_EDITOR || DEBUG
        float _debugLogTimer;
#endif

        void Awake()
        {
            _rb = GetComponent<Rigidbody>(); _input = GetComponent<R8EOX.Input.RCInput>();
            _airPhysics = GetComponentInChildren<RCAirPhysics>(); _drivetrain = GetComponentInChildren<Drivetrain>();
        }

        void Start()
        {
            ApplyMotorPreset();
            _rb.mass = k_DefaultMass; _rb.centerOfMass = _comGround; _rb.drag = 0f;
            _rb.angularDrag = k_DefaultAngularDrag; _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var physMat = new PhysicMaterial("CarBody") {
                dynamicFriction = 0f, staticFriction = 0f, bounciness = k_DefaultBounciness,
                frictionCombine = PhysicMaterialCombine.Minimum, bounceCombine = PhysicMaterialCombine.Maximum
            };
            foreach (var col in GetComponentsInChildren<Collider>()) col.material = physMat;

            _tumble = new TumbleController(physMat);
            _wheels.Discover(transform);
            _wheels.Configure(gameObject.layer, _drivetrain,
                _frontSpringStrength, _frontSpringDamping, _rearSpringStrength, _rearSpringDamping, _gripCoeff);

            RuntimeLog.Log($"[RCCar] Motor={_motorPreset} engine={_engineForceMax}N max={_maxSpeed}m/s " +
                      $"mass={_rb.mass}kg frontSpring={_frontSpringStrength} rearSpring={_rearSpringStrength} grip={_gripCoeff}");
        }

        void Update()
        {
            if (_input != null && _input.ResetPressed) _flipRequested = true;
            if (_input != null && _input.DebugTogglePressed)
                foreach (var w in _wheels.All) w.ShowDebug = !w.ShowDebug;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_flipRequested) { _flipRequested = false; DoFlip(); }

            IsAirborne = _airDetect.Update(!_wheels.AnyOnGround());
            _tumble.Update(transform, IsAirborne, _tumbleEngageDeg, _tumbleFullDeg,
                _tumbleHysteresisDeg, _enableDynamicPhysicsMaterial, _tumbleBounce, _tumbleFriction);
            TumbleFactor = _tumble.TumbleFactor; TiltAngle = _tumble.TiltAngle;
            _rb.centerOfMass = _comGround;

            float throttleRaw = _input != null ? _input.Throttle : 0f;
            float brakeIn     = _input != null ? _input.Brake    : 0f;
            float steerIn     = _input != null ? _input.Steer    : 0f;
            float rampRate    = throttleRaw > SmoothThrottle ? _throttleRampUp : _throttleRampDown;
            SmoothThrottle = Mathf.MoveTowards(SmoothThrottle, throttleRaw, rampRate * dt);
            ForwardSpeed   = Vector3.Dot(_rb.velocity, transform.forward);

            if (IsAirborne)
            {
                CurrentEngineForce = 0f; CurrentBrakeForce = 0f;
                foreach (var w in _wheels.All) w.MotorForceShare = 0f;
                if (_airPhysics != null) _airPhysics.Apply(dt, SmoothThrottle, brakeIn, steerIn);
            }
            else
            {
                ApplyGroundDrive(SmoothThrottle, brakeIn, ForwardSpeed);
                if (_drivetrain != null) _drivetrain.Distribute(CurrentEngineForce, _wheels.Front, _wheels.Rear);
                CurrentSteering = SteeringRamp.Step(CurrentSteering, dt, steerIn, ForwardSpeed,
                    _steeringMax, _steeringSpeed, _steeringSpeedLimit, _steeringHighSpeedFactor);
            }

            foreach (var w in _wheels.All)
            {
                w.IsBraking = CurrentBrakeForce > 0f && w.IsMotor;
                w.ApplyWheelPhysics(_rb, dt);
                if (w.IsSteer) w.transform.localRotation = Quaternion.Euler(0f, CurrentSteering * Mathf.Rad2Deg, 0f);
            }
#if UNITY_EDITOR || DEBUG
            _debugLogTimer += dt;
            if (_debugLogTimer >= 0.5f) { RuntimeLog.Log($"[esc] throttle={SmoothThrottle:F3} engineForce={CurrentEngineForce:F2}N brake={CurrentBrakeForce:F2}N reverse={ReverseEngaged} airborne={IsAirborne}"); _debugLogTimer = 0f; }
#endif
        }

        // ---- Public API ----

        public float GetSpeedKmh()        => _rb.velocity.magnitude * k_MsToKmh;
        public float GetForwardSpeedKmh() => Vector3.Dot(_rb.velocity, transform.forward) * k_MsToKmh;

        public float GetSlip()
        {
            float slip = 0f; int count = 0;
            foreach (var w in _wheels.All) { if (w.IsMotor) { slip += w.SlipRatio; count++; } }
            return count > 0 ? slip / count : 0f;
        }

        public RaycastWheel[] GetAllWheels()
        { if (_wheels.All.Length == 0) _wheels.Discover(transform); return _wheels.All; }

        public void ApplySuspensionSettings() =>
            _wheels.PushSuspension(_frontSpringStrength, _frontSpringDamping, _rearSpringStrength, _rearSpringDamping);
        public void ApplyTractionSettings() => _wheels.PushGrip(_gripCoeff);

        // ---- Private ----

        void ApplyGroundDrive(float throttleIn, float brakeIn, float fwdSpeed)
        {
            var r = PhysicsMath.ESCMath.ComputeGroundDrive(
                throttleIn, brakeIn, fwdSpeed, ReverseEngaged,
                _engineForceMax, _brakeForce, _reverseForce, _coastDrag, _maxSpeed,
                _rb.velocity.magnitude, k_ReverseSpeedThreshold, k_ForwardSpeedClearThreshold, k_ReverseBrakeMinThreshold);
            CurrentEngineForce = r.EngineForce; CurrentBrakeForce = r.BrakeForce; ReverseEngaged = r.ReverseEngaged;
            if (r.CoastDragForce > 0f) _rb.AddForce(-transform.forward * r.CoastDragForce, ForceMode.Force);
        }

        void DoFlip()
        {
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            transform.position += Vector3.up * k_FlipHeightOffset;
            _rb.velocity = _rb.angularVelocity = Vector3.zero;
        }
    }
}
