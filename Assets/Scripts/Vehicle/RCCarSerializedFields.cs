using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Partial class holding RCCar's Inspector-serialized configuration, all public
    /// accessor properties, the runtime tuning setter API, and motor preset application.
    /// Split from <see cref="RCCar"/> to keep the orchestrator file under 150 lines.
    /// </summary>
    public partial class RCCar
    {
        [Header("Motor")]
        [Tooltip("Motor preset selecting predefined engine parameters")]
        [SerializeField] MotorPreset _motorPreset = MotorPreset.Motor13_5T;

        [Header("Engine")]
        [Tooltip("Maximum engine force applied at the wheels (Newtons)")]
        [SerializeField] float _engineForceMax = 260f;
        [Tooltip("Maximum forward speed (m/s)")]
        [SerializeField] float _maxSpeed = 27f;
        [Tooltip("Braking force applied when brake input is active (Newtons)")]
        [SerializeField] float _brakeForce = 221f;
        [Tooltip("Reverse drive force (Newtons)")]
        [SerializeField] float _reverseForce = 143f;
        [Tooltip("Coast drag force applied when no throttle is given (Newtons)")]
        [SerializeField] float _coastDrag = 30f;

        [Header("Throttle")]
        [Tooltip("Rate at which throttle ramps up (units/sec)")]
        [SerializeField] float _throttleRampUp = 5.5f;
        [Tooltip("Rate at which throttle ramps down when released (units/sec)")]
        [SerializeField] float _throttleRampDown = 10f;

        [Header("Steering")]
        [Tooltip("Maximum steering angle in radians")]
        [SerializeField] float _steeringMax = 0.50f;
        [Tooltip("Steering interpolation speed (units/sec)")]
        [SerializeField] float _steeringSpeed = 7f;
        [Tooltip("Speed threshold above which high-speed steering factor activates (m/s)")]
        [SerializeField] float _steeringSpeedLimit = 8f;
        [Range(0f, 1f)]
        [Tooltip("Steering angle multiplier applied at high speed (0=no steering, 1=full steering)")]
        [SerializeField] float _steeringHighSpeedFactor = 0.4f;

        [Header("Suspension Front")]
        [Tooltip("Front axle spring strength (N/m)")]
        [SerializeField] float _frontSpringStrength = 700.0f;
        [Tooltip("Front axle spring damping coefficient (N·s/m)")]
        [SerializeField] float _frontSpringDamping = 41.0f;

        [Header("Suspension Rear")]
        [Tooltip("Rear axle spring strength (N/m)")]
        [SerializeField] float _rearSpringStrength = 350.0f;
        [Tooltip("Rear axle spring damping coefficient (N·s/m)")]
        [SerializeField] float _rearSpringDamping = 29.0f;

        [Header("Traction")]
        [Range(0f, 1f)]
        [Tooltip("Grip coefficient multiplier applied to all wheels (0=no grip, 1=full grip)")]
        [SerializeField] float _gripCoeff = 0.7f;

        [Header("CoM")]
        [Tooltip("Centre of mass offset from the Rigidbody origin (world-space Y is most critical)")]
        [SerializeField] Vector3 _comGround = new Vector3(0f, -0.12f, 0f);

        [Header("Crash")]
        [Tooltip("Tilt angle (degrees) at which tumble mode begins to engage")]
        [SerializeField] float _tumbleEngageDeg = 50f;
        [Tooltip("Tilt angle (degrees) at which tumble mode is fully active")]
        [SerializeField] float _tumbleFullDeg = 70f;
        [Tooltip("Hysteresis band (degrees) to prevent tumble state oscillation")]
        [SerializeField] float _tumbleHysteresisDeg = 5f;
        [Range(0f, 1f)]
        [Tooltip("Bounciness of the physics material while tumbling (0=no bounce, 1=full bounce)")]
        [SerializeField] float _tumbleBounce = 0.35f;
        [Range(0f, 1f)]
        [Tooltip("Friction of the physics material while tumbling (0=frictionless, 1=full friction)")]
        [SerializeField] float _tumbleFriction = 0.3f;
        [Tooltip("When true, switches to a low-friction physics material during tumble")]
        [SerializeField] bool _enableDynamicPhysicsMaterial = true;

        // ---- Tuning accessors ----

        public float EngineForceMax => _engineForceMax; public float MaxSpeed    => _maxSpeed;
        public float BrakeForce     => _brakeForce;     public float ReverseForce => _reverseForce;
        public float CoastDrag      => _coastDrag;      public float ThrottleRampUp => _throttleRampUp;
        public float ThrottleRampDown => _throttleRampDown; public float SteeringMax => _steeringMax;
        public float SteeringSpeed    => _steeringSpeed;    public float SteeringSpeedLimit => _steeringSpeedLimit;
        public float SteeringHighSpeedFactor => _steeringHighSpeedFactor;
        public float FrontSpringStrength => _frontSpringStrength; public float FrontSpringDamping => _frontSpringDamping;
        public float RearSpringStrength  => _rearSpringStrength;  public float RearSpringDamping  => _rearSpringDamping;
        public float GripCoeff => _gripCoeff; public float ComGroundY => _comGround.y;
        public float TumbleEngageDeg => _tumbleEngageDeg; public float TumbleFullDeg  => _tumbleFullDeg;
        public float TumbleBounce    => _tumbleBounce;    public float TumbleFriction => _tumbleFriction;

        // ---- Tuning setter API ----

        public void SetMotorParams(float engineForce, float maxSpeed, float brakeForce, float reverseForce, float coastDrag)
        { _motorPreset = MotorPreset.Custom; _engineForceMax = engineForce; _maxSpeed = maxSpeed; _brakeForce = brakeForce; _reverseForce = reverseForce; _coastDrag = coastDrag; }
        public void SetThrottleResponse(float rampUp, float rampDown) { _throttleRampUp = rampUp; _throttleRampDown = rampDown; }
        public void SetSteeringParams(float max, float speed, float speedLimit, float highSpeedFactor)
        { _steeringMax = max; _steeringSpeed = speed; _steeringSpeedLimit = speedLimit; _steeringHighSpeedFactor = highSpeedFactor; }
        public void SetSuspension(float springStrength, float damping)
        { _frontSpringStrength = _rearSpringStrength = springStrength; _frontSpringDamping = _rearSpringDamping = damping; if (_wheels.All.Length > 0) ApplySuspensionSettings(); }
        public void SetAxleSuspension(float frontK, float frontDamp, float rearK, float rearDamp)
        { _frontSpringStrength = frontK; _frontSpringDamping = frontDamp; _rearSpringStrength = rearK; _rearSpringDamping = rearDamp; if (_wheels.All.Length > 0) ApplySuspensionSettings(); }
        public void SetTraction(float gripCoeff) { _gripCoeff = gripCoeff; if (_wheels.All.Length > 0) ApplyTractionSettings(); }
        public void SetCrashParams(float engageDeg, float fullDeg, float bounce, float friction)
        { _tumbleEngageDeg = engageDeg; _tumbleFullDeg = fullDeg; _tumbleBounce = bounce; _tumbleFriction = friction; }
        public void SetCentreOfMass(float groundY) => _comGround = new Vector3(0f, groundY, 0f);
        public void SetMass(float mass) { if (_rb != null) _rb.mass = mass; }
        public void SelectMotorPreset(MotorPreset preset) { _motorPreset = preset; ApplyMotorPreset(); }

        void ApplyMotorPreset()
        {
            var p = MotorPresetRegistry.Get(_motorPreset); if (p == null) return;
            var d = p.Value;
            _engineForceMax = d.EngineForceMax; _brakeForce = d.BrakeForce; _reverseForce = d.ReverseForce;
            _coastDrag = d.CoastDrag; _maxSpeed = d.MaxSpeed; _throttleRampUp = d.ThrottleRampUp;
        }
    }
}
