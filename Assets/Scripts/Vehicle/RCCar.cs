using UnityEngine;
using PhysicsMath = R8EOX.Vehicle.Physics;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Main RC buggy vehicle controller. Rigidbody on root GameObject.
    /// Orchestrates ground drive, steering, tumble detection, and airborne state.
    /// All tunable parameters live in <see cref="VehicleParams"/> (_p).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RCCar : MonoBehaviour
    {
        const float k_DefaultMass                = 15.0f;
        const float k_DefaultAngularDrag         = 0.05f;
        const float k_FlipHeightOffset           = 14.0f;
        const float k_ReverseSpeedThreshold      = 0.25f;
        const float k_ForwardSpeedClearThreshold = 0.50f;
        const float k_ReverseBrakeMinThreshold   = 0.1f;
        const float k_MsToKmh                    = 3.6f;

        [SerializeField] private VehicleParams _p = new VehicleParams();

        // Runtime state
        public float       CurrentEngineForce { get; private set; }
        public float       CurrentBrakeForce  { get; private set; }
        public float       SmoothThrottle     { get; private set; }
        public bool        IsAirborne         { get; private set; }
        public bool        ReverseEngaged     { get; private set; }
        public float       ForwardSpeed       { get; private set; }
        public float       CurrentSteering    => _steeringRamp.CurrentSteering;
        public float       TumbleFactor       => _tumbleController.TumbleFactor;
        public float       TiltAngle          => _tumbleController.TiltAngle;
        public MotorPreset ActiveMotorPreset  => _p.MotorPreset;

        // Tuning read-only surface (delegates to VehicleParams)
        public float EngineForceMax          => _p.EngineForceMax;
        public float MaxSpeed                => _p.MaxSpeed;
        public float BrakeForce              => _p.BrakeForce;
        public float ReverseForce            => _p.ReverseForce;
        public float CoastDrag               => _p.CoastDrag;
        public float ThrottleRampUp          => _p.ThrottleRampUp;
        public float ThrottleRampDown        => _p.ThrottleRampDown;
        public float SteeringMax             => _p.SteeringMax;
        public float SteeringSpeed           => _p.SteeringSpeed;
        public float SteeringSpeedLimit      => _p.SteeringSpeedLimit;
        public float SteeringHighSpeedFactor => _p.SteeringHighSpeedFactor;
        public float FrontSpringStrength     => _p.FrontSpringStrength;
        public float FrontSpringDamping      => _p.FrontSpringDamping;
        public float RearSpringStrength      => _p.RearSpringStrength;
        public float RearSpringDamping       => _p.RearSpringDamping;
        public float GripCoeff              => _p.GripCoeff;
        public float ComGroundY             => _p.ComGround.y;
        public float TumbleEngageDeg        => _p.TumbleEngageDeg;
        public float TumbleFullDeg          => _p.TumbleFullDeg;
        public float TumbleBounce           => _p.TumbleBounce;
        public float TumbleFriction         => _p.TumbleFriction;
        public RCAirPhysics AirPhysics      => _airPhysics;
        public Drivetrain   DrivetrainRef   => _drivetrain;
        public float        Mass            => _rb != null ? _rb.mass : k_DefaultMass;

        private Rigidbody                 _rb;
        private R8EOX.Input.IVehicleInput _input;
        private RCAirPhysics              _airPhysics;
        private Drivetrain                _drivetrain;
        private readonly WheelManager     _wheels           = new WheelManager();
        private readonly AirborneDetector _airborneDetector = new AirborneDetector();
        private readonly TumbleController _tumbleController = new TumbleController();
        private readonly SteeringRamp     _steeringRamp     = new SteeringRamp();
        private bool  _flipRequested;
#if UNITY_EDITOR || DEBUG
        private float _debugLogTimer;
#endif

        void Awake()
        {
            _rb         = GetComponent<Rigidbody>();
            _input      = GetComponent<R8EOX.Input.RCInput>();
            _airPhysics = GetComponentInChildren<RCAirPhysics>();
            _drivetrain = GetComponentInChildren<Drivetrain>();
        }

        void Start()
        {
            ApplyMotorPreset();
            ConfigureRigidbody();
            _tumbleController.Initialise(transform);
            _wheels.Discover(transform);
            _wheels.Configure(_drivetrain, gameObject.layer,
                _p.FrontSpringStrength, _p.FrontSpringDamping,
                _p.RearSpringStrength,  _p.RearSpringDamping,
                _p.GripCoeff);
            Debug.Log($"[RCCar] Motor={_p.MotorPreset} engine={_p.EngineForceMax}N max={_p.MaxSpeed}m/s " +
                      $"mass={_rb.mass}kg frontSpring={_p.FrontSpringStrength} rearSpring={_p.RearSpringStrength} grip={_p.GripCoeff}");
        }

        void Update()
        {
            if (_input != null && _input.ResetPressed)         _flipRequested = true;
            if (_input != null && _input.DebugTogglePressed)
                foreach (var w in _wheels.All) w.ShowDebug = !w.ShowDebug;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_flipRequested) { _flipRequested = false; DoFlip(); }

            bool anyOnGround = false;
            foreach (var w in _wheels.All) if (w.IsOnGround) { anyOnGround = true; break; }

            IsAirborne = _airborneDetector.Update(anyOnGround);
            _tumbleController.Update(transform, IsAirborne,
                _p.TumbleEngageDeg, _p.TumbleFullDeg, _p.TumbleHysteresisDeg,
                _p.TumbleBounce, _p.TumbleFriction, _p.EnableDynamicPhysicsMaterial);
            _rb.centerOfMass = _p.ComGround;

            float throttleRaw = _input != null ? _input.Throttle : 0f;
            float brakeIn     = _input != null ? _input.Brake    : 0f;
            float steerIn     = _input != null ? _input.Steer    : 0f;

            float rampRate = throttleRaw > SmoothThrottle ? _p.ThrottleRampUp : _p.ThrottleRampDown;
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
                if (_drivetrain != null)
                    _drivetrain.Distribute(CurrentEngineForce, _wheels.Front, _wheels.Rear);
                _steeringRamp.Update(dt, steerIn, ForwardSpeed,
                    _p.SteeringMax, _p.SteeringSpeed, _p.SteeringSpeedLimit, _p.SteeringHighSpeedFactor);
            }

            foreach (var w in _wheels.All)
            {
                w.IsBraking = CurrentBrakeForce > 0f && w.IsMotor;
                w.ApplyWheelPhysics(_rb, dt);
                if (w.IsSteer)
                    w.transform.localRotation = Quaternion.Euler(0f, CurrentSteering * Mathf.Rad2Deg, 0f);
            }
#if UNITY_EDITOR || DEBUG
            _debugLogTimer += dt;
            if (_debugLogTimer >= 0.5f)
            {
                Debug.Log($"[esc] throttle={SmoothThrottle:F3} engineForce={CurrentEngineForce:F2}N " +
                          $"brake={CurrentBrakeForce:F2}N reverse={ReverseEngaged} airborne={IsAirborne}");
                _debugLogTimer = 0f;
            }
#endif
        }

        // ---- Public API ----

        public float GetSpeedKmh()        => _rb.velocity.magnitude * k_MsToKmh;
        public float GetForwardSpeedKmh() => Vector3.Dot(_rb.velocity, transform.forward) * k_MsToKmh;

        public float GetSlip()
        {
            float slip = 0f; int count = 0;
            foreach (var w in _wheels.All) if (w.IsMotor) { slip += w.SlipRatio; count++; }
            return count > 0 ? slip / count : 0f;
        }

        /// <summary>Returns all wheel components. Lazy-discovers if not yet initialised.</summary>
        public RaycastWheel[] GetAllWheels()
        {
            if (_wheels.All == null) _wheels.Discover(transform);
            return _wheels.All;
        }

        public void ApplySuspensionSettings() =>
            _wheels.ApplySuspension(_p.FrontSpringStrength, _p.FrontSpringDamping,
                                    _p.RearSpringStrength,  _p.RearSpringDamping);

        public void ApplyTractionSettings() => _wheels.ApplyTraction(_p.GripCoeff);

        // ---- Tuning Setters ----

        public void SetMotorParams(float engineForce, float maxSpeed, float brakeForce,
            float reverseForce, float coastDrag)
        {
            _p.MotorPreset    = MotorPreset.Custom;
            _p.EngineForceMax = engineForce;  _p.MaxSpeed     = maxSpeed;
            _p.BrakeForce     = brakeForce;   _p.ReverseForce = reverseForce;
            _p.CoastDrag      = coastDrag;
        }

        public void SetThrottleResponse(float rampUp, float rampDown)
        { _p.ThrottleRampUp = rampUp; _p.ThrottleRampDown = rampDown; }

        public void SetSteeringParams(float max, float speed, float speedLimit, float highSpeedFactor)
        { _p.SteeringMax = max; _p.SteeringSpeed = speed; _p.SteeringSpeedLimit = speedLimit; _p.SteeringHighSpeedFactor = highSpeedFactor; }

        public void SetSuspension(float springStrength, float damping)
        {
            _p.FrontSpringStrength = _p.RearSpringStrength = springStrength;
            _p.FrontSpringDamping  = _p.RearSpringDamping  = damping;
            if (_wheels.All != null) ApplySuspensionSettings();
        }

        public void SetAxleSuspension(float frontK, float frontDamp, float rearK, float rearDamp)
        {
            _p.FrontSpringStrength = frontK;  _p.FrontSpringDamping = frontDamp;
            _p.RearSpringStrength  = rearK;   _p.RearSpringDamping  = rearDamp;
            if (_wheels.All != null) ApplySuspensionSettings();
        }

        public void SetTraction(float gripCoeff)
        { _p.GripCoeff = gripCoeff; if (_wheels.All != null) ApplyTractionSettings(); }

        public void SetCrashParams(float engageDeg, float fullDeg, float bounce, float friction)
        { _p.TumbleEngageDeg = engageDeg; _p.TumbleFullDeg = fullDeg; _p.TumbleBounce = bounce; _p.TumbleFriction = friction; }

        public void SetCentreOfMass(float groundY) => _p.ComGround = new Vector3(0f, groundY, 0f);
        public void SetMass(float mass)             { if (_rb != null) _rb.mass = mass; }

        public void SelectMotorPreset(MotorPreset preset)
        { _p.MotorPreset = preset; ApplyMotorPreset(); }

        // ---- Private Helpers ----

        private void ApplyMotorPreset()
        {
            if (!MotorPresetRegistry.TryGet(_p.MotorPreset, out var d)) return;
            _p.EngineForceMax = d.EngineForceMax; _p.BrakeForce    = d.BrakeForce;
            _p.ReverseForce   = d.ReverseForce;   _p.CoastDrag     = d.CoastDrag;
            _p.MaxSpeed       = d.MaxSpeed;        _p.ThrottleRampUp = d.ThrottleRampUp;
        }

        private void ConfigureRigidbody()
        {
            _rb.mass                   = k_DefaultMass;    _rb.drag         = 0f;
            _rb.centerOfMass           = _p.ComGround;     _rb.angularDrag  = k_DefaultAngularDrag;
            _rb.interpolation          = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void ApplyGroundDrive(float throttleIn, float brakeIn, float fwdSpeed)
        {
            var r = PhysicsMath.ESCMath.ComputeGroundDrive(
                throttleIn, brakeIn, fwdSpeed, ReverseEngaged,
                _p.EngineForceMax, _p.BrakeForce, _p.ReverseForce,
                _p.CoastDrag, _p.MaxSpeed, _rb.velocity.magnitude,
                k_ReverseSpeedThreshold, k_ForwardSpeedClearThreshold, k_ReverseBrakeMinThreshold);

            CurrentEngineForce = r.EngineForce;
            CurrentBrakeForce  = r.BrakeForce;
            ReverseEngaged     = r.ReverseEngaged;
            if (r.CoastDragForce > 0f)
                _rb.AddForce(-transform.forward * r.CoastDragForce, ForceMode.Force);
        }

        private void DoFlip()
        {
            Vector3 e          = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, e.y, 0f);
            transform.position += Vector3.up * k_FlipHeightOffset;
            _rb.velocity = _rb.angularVelocity = Vector3.zero;
        }
    }
}
