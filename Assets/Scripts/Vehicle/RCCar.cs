using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Main RC buggy vehicle controller. Rigidbody on root GameObject.
    /// Orchestrates ground drive, steering, tumble detection, and airborne state.
    /// Ported from rc_car.gd with Godot→Unity coordinate mapping.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RCCar : MonoBehaviour
    {
        // ---- Constants ----

        const int k_AirborneThreshold = 5;
        const float k_DefaultMass = 1.5f;
        const float k_DefaultAngularDrag = 0.05f;
        const float k_DefaultBounciness = 0.05f;
        const float k_FlipHeightOffset = 1.4f;
        const float k_ReverseSpeedThreshold = 0.25f;
        const float k_ForwardSpeedClearThreshold = 0.50f;
        const float k_MsToKmh = 3.6f;


        // ---- Motor Presets ----

        /// <summary>Motor turn rating presets matching real RC motor specifications.</summary>
        public enum MotorPreset
        {
            Motor21_5T, Motor17_5T, Motor13_5T, Motor9_5T, Motor5_5T, Motor1_5T, Custom
        }

        struct MotorData
        {
            public float EngineForceMax;
            public float BrakeForce;
            public float ReverseForce;
            public float CoastDrag;
            public float MaxSpeed;
            public float ThrottleRampUp;

            public MotorData(float engine, float brake, float reverse, float coast, float max, float ramp)
            {
                EngineForceMax = engine;
                BrakeForce = brake;
                ReverseForce = reverse;
                CoastDrag = coast;
                MaxSpeed = max;
                ThrottleRampUp = ramp;
            }
        }

        static readonly MotorData[] k_MotorPresets =
        {
            new MotorData(15.5f, 13.2f,  8.5f, 2.0f, 13f, 3.0f),  // 21.5T
            new MotorData(18.0f, 15.3f,  9.9f, 2.5f, 20f, 4.0f),  // 17.5T
            new MotorData(26.0f, 22.1f, 14.3f, 3.0f, 27f, 5.5f),  // 13.5T
            new MotorData(34.0f, 28.9f, 18.7f, 3.5f, 34f, 7.0f),  // 9.5T
            new MotorData(44.0f, 37.4f, 24.2f, 4.0f, 44f, 9.0f),  // 5.5T
            new MotorData(56.0f, 47.6f, 30.8f, 5.0f, 56f, 12.0f), // 1.5T
        };


        // ---- Serialized Fields ----

        [Header("Motor")]
        [Tooltip("Select a motor preset or Custom for manual tuning")]
        [SerializeField] private MotorPreset _motorPreset = MotorPreset.Motor13_5T;

        [Header("Engine")]
        [Tooltip("Peak driving force in Newtons")]
        [SerializeField] private float _engineForceMax = 26f;
        [Tooltip("Maximum speed in m/s")]
        [SerializeField] private float _maxSpeed = 27f;
        [Tooltip("Braking force in Newtons (~85% of engine)")]
        [SerializeField] private float _brakeForce = 22.1f;
        [Tooltip("Reverse force in Newtons (~55% of forward)")]
        [SerializeField] private float _reverseForce = 14.3f;
        [Tooltip("Drivetrain drag force while coasting in Newtons")]
        [SerializeField] private float _coastDrag = 3.0f;

        [Header("Throttle Response")]
        [Tooltip("Ramp rate from 0 to 1 in units/sec")]
        [SerializeField] private float _throttleRampUp = 5.5f;
        [Tooltip("Ramp rate from 1 to 0 in units/sec")]
        [SerializeField] private float _throttleRampDown = 10f;

        [Header("Steering")]
        [Tooltip("Max steering angle in radians (~29 deg)")]
        [SerializeField] private float _steeringMax = 0.50f;
        [Tooltip("Steering ramp speed in rad/s")]
        [SerializeField] private float _steeringSpeed = 7f;
        [Tooltip("Speed in m/s at which steering reduces")]
        [SerializeField] private float _steeringSpeedLimit = 8f;
        [Tooltip("Fraction of steeringMax kept at high speed")]
        [SerializeField] private float _steeringHighSpeedFactor = 0.4f;

        [Header("Suspension")]
        [Tooltip("Spring stiffness in N/m (distributed across all wheels)")]
        [SerializeField] private float _springStrength = 75f;
        [Tooltip("Damping coefficient")]
        [SerializeField] private float _springDamping = 4.25f;

        [Header("Traction")]
        [Tooltip("Global grip multiplier (0-1)")]
        [SerializeField] private float _gripCoeff = 0.7f;

        [Header("Centre of Mass")]
        [Tooltip("Centre of mass offset when grounded")]
        [SerializeField] private Vector3 _comGround = new Vector3(0f, -0.20f, 0f);
        [Tooltip("Centre of mass offset when airborne (lower = anti-flip)")]
        [SerializeField] private Vector3 _comAir = new Vector3(0f, -1.2f, 0f);

        [Header("Crash Physics")]
        [Tooltip("Tilt angle in degrees where tumble blending begins")]
        [SerializeField] private float _tumbleEngageDeg = 50f;
        [Tooltip("Tilt angle in degrees for full tumble effect")]
        [SerializeField] private float _tumbleFullDeg = 70f;
        [Tooltip("Bounciness coefficient during tumble")]
        [SerializeField] private float _tumbleBounce = 0.35f;
        [Tooltip("Friction coefficient during tumble")]
        [SerializeField] private float _tumbleFriction = 0.3f;
        [Tooltip("Hysteresis band in degrees to prevent threshold oscillation")]
        [SerializeField] private float _tumbleHysteresisDeg = 5f;


        // ---- Public Properties ----

        public float CurrentEngineForce { get; private set; }
        public float CurrentBrakeForce { get; private set; }
        public float SmoothThrottle { get; private set; }
        public float CurrentSteering { get; private set; }
        public bool IsAirborne { get; private set; }
        public float TumbleFactor { get; private set; }
        public float TiltAngle { get; private set; }
        public bool ReverseEngaged { get; private set; }
        public float ForwardSpeed { get; private set; }
        public MotorPreset ActiveMotorPreset => _motorPreset;


        // ---- Private Fields ----

        private Rigidbody _rb;
        private R8EOX.Input.RCInput _input;
        private RCAirPhysics _airPhysics;
        private Drivetrain _drivetrain;
        private RaycastWheel[] _allWheels;
        private RaycastWheel[] _frontWheels;
        private RaycastWheel[] _rearWheels;
        private PhysicMaterial _physMat;
        private Collider[] _colliders;
        private bool _wasTumbling;
        private int _airborneFrames;
        private bool _flipRequested;


        // ---- Unity Lifecycle ----

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _input = GetComponent<R8EOX.Input.RCInput>();
            _airPhysics = GetComponentInChildren<RCAirPhysics>();
            _drivetrain = GetComponentInChildren<Drivetrain>();
        }

        void Start()
        {
            ApplyMotorPreset();
            ConfigureRigidbody();
            CreatePhysicsMaterial();
            DiscoverWheels();
            ConfigureWheels();

            Debug.Log($"[RCCar] Motor={_motorPreset} engine={_engineForceMax}N max={_maxSpeed}m/s " +
                      $"mass={_rb.mass}kg spring={_springStrength} damp={_springDamping} grip={_gripCoeff}");
        }

        void Update()
        {
            if (_input != null && _input.ResetPressed)
                _flipRequested = true;

            if (_input != null && _input.DebugTogglePressed)
            {
                foreach (var w in _allWheels)
                    w.ShowDebug = !w.ShowDebug;
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            if (_flipRequested)
            {
                _flipRequested = false;
                DoFlip();
            }

            IsAirborne = CheckAirborne();
            ComputeTumbleFactor();
            _rb.centerOfMass = IsAirborne ? _comAir : _comGround;
            UpdatePhysicsMaterial();

            float throttleRaw = _input != null ? _input.Throttle : 0f;
            float brakeIn = _input != null ? _input.Brake : 0f;
            float steerIn = _input != null ? _input.Steer : 0f;

            float rampRate = throttleRaw > SmoothThrottle ? _throttleRampUp : _throttleRampDown;
            SmoothThrottle = Mathf.MoveTowards(SmoothThrottle, throttleRaw, rampRate * dt);
            ForwardSpeed = Vector3.Dot(_rb.velocity, transform.forward);

            if (IsAirborne)
            {
                CurrentEngineForce = 0f;
                CurrentBrakeForce = 0f;
                foreach (var w in _allWheels)
                    w.MotorForceShare = 0f;
                CurrentSteering = Mathf.MoveTowards(CurrentSteering, 0f, _steeringSpeed * dt);
                if (_airPhysics != null)
                    _airPhysics.Apply(dt, SmoothThrottle, brakeIn, steerIn);
            }
            else
            {
                ApplyGroundDrive(SmoothThrottle, brakeIn, ForwardSpeed);
                if (_drivetrain != null)
                    _drivetrain.Distribute(CurrentEngineForce, _frontWheels, _rearWheels);
                ApplySteering(dt, steerIn, ForwardSpeed);
            }

            foreach (var w in _allWheels)
            {
                w.IsBraking = CurrentBrakeForce > 0f && w.IsMotor;
                w.ApplyWheelPhysics(_rb, dt);

                if (w.IsSteer)
                    w.transform.localRotation = Quaternion.Euler(0f, CurrentSteering * Mathf.Rad2Deg, 0f);
            }
        }


        // ---- Public API ----

        /// <summary>Returns total speed in km/h.</summary>
        public float GetSpeedKmh() => _rb.velocity.magnitude * k_MsToKmh;

        /// <summary>Returns signed forward speed in km/h.</summary>
        public float GetForwardSpeedKmh() => Vector3.Dot(_rb.velocity, transform.forward) * k_MsToKmh;

        /// <summary>Returns average slip ratio across motor wheels (0=grip, 1=slide).</summary>
        public float GetSlip()
        {
            float slip = 0f;
            int count = 0;
            foreach (var w in _allWheels)
            {
                if (w.IsMotor)
                {
                    slip += w.SlipRatio;
                    count++;
                }
            }
            return count > 0 ? slip / count : 0f;
        }

        /// <summary>Returns all wheel components for telemetry display.</summary>
        public RaycastWheel[] GetAllWheels() => _allWheels;

        /// <summary>Pushes current suspension settings to all wheels.</summary>
        public void ApplySuspensionSettings()
        {
            foreach (var w in _allWheels)
            {
                w.SpringStrength = _springStrength;
                w.SpringDamping = _springDamping;
            }
        }

        /// <summary>Pushes current traction settings to all wheels.</summary>
        public void ApplyTractionSettings()
        {
            foreach (var w in _allWheels)
                w.GripCoeff = _gripCoeff;
        }


        // ---- Private Methods ----

        private void ApplyMotorPreset()
        {
            if (_motorPreset == MotorPreset.Custom) return;
            int idx = (int)_motorPreset;
            if (idx < 0 || idx >= k_MotorPresets.Length) return;
            var p = k_MotorPresets[idx];
            _engineForceMax = p.EngineForceMax;
            _brakeForce = p.BrakeForce;
            _reverseForce = p.ReverseForce;
            _coastDrag = p.CoastDrag;
            _maxSpeed = p.MaxSpeed;
            _throttleRampUp = p.ThrottleRampUp;
        }

        private void ConfigureRigidbody()
        {
            _rb.mass = k_DefaultMass;
            _rb.centerOfMass = _comGround;
            _rb.drag = 0f;
            _rb.angularDrag = k_DefaultAngularDrag;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void CreatePhysicsMaterial()
        {
            _physMat = new PhysicMaterial("CarBody")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = k_DefaultBounciness,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Maximum
            };

            _colliders = GetComponentsInChildren<Collider>();
            foreach (var col in _colliders)
                col.material = _physMat;
        }

        private void DiscoverWheels()
        {
            var allList = new System.Collections.Generic.List<RaycastWheel>();
            var frontList = new System.Collections.Generic.List<RaycastWheel>();
            var rearList = new System.Collections.Generic.List<RaycastWheel>();

            foreach (var w in GetComponentsInChildren<RaycastWheel>())
            {
                allList.Add(w);
                if (w.transform.localPosition.z > 0f)
                    frontList.Add(w);
                else
                    rearList.Add(w);
            }

            _allWheels = allList.ToArray();
            _frontWheels = frontList.ToArray();
            _rearWheels = rearList.ToArray();
        }

        private void ConfigureWheels()
        {
            if (_drivetrain != null)
                _drivetrain.UpdateLayout(_frontWheels, _rearWheels);

            ApplySuspensionSettings();
            ApplyTractionSettings();

            int carLayer = gameObject.layer;
            foreach (var w in _allWheels)
            {
                w.GroundMask = ~(1 << carLayer);
                w.ShowDebug = false;
            }
        }

        private void ApplyGroundDrive(float throttleIn, float brakeIn, float fwdSpeed)
        {
            if (throttleIn > 0f || fwdSpeed > k_ForwardSpeedClearThreshold)
                ReverseEngaged = false;
            else if (brakeIn > 0f && fwdSpeed < k_ReverseSpeedThreshold)
                ReverseEngaged = true;

            if (throttleIn > 0f)
            {
                CurrentEngineForce = _rb.velocity.magnitude >= _maxSpeed ? 0f : throttleIn * _engineForceMax;
                CurrentBrakeForce = 0f;
            }
            else if (brakeIn > 0f)
            {
                if (ReverseEngaged)
                {
                    CurrentEngineForce = -brakeIn * _reverseForce;
                    CurrentBrakeForce = 0f;
                }
                else
                {
                    CurrentEngineForce = 0f;
                    CurrentBrakeForce = brakeIn * _brakeForce;
                }
            }
            else
            {
                ReverseEngaged = false;
                CurrentEngineForce = 0f;
                CurrentBrakeForce = _coastDrag;
            }
        }

        private void ApplySteering(float dt, float steerIn, float fwdSpeed)
        {
            float spd = Mathf.Abs(fwdSpeed);
            float t = Mathf.Clamp01(spd / _steeringSpeedLimit);
            float effectiveMax = Mathf.Lerp(_steeringMax, _steeringMax * _steeringHighSpeedFactor, t);
            float steerSign = fwdSpeed < -k_ReverseSpeedThreshold ? -1f : 1f;
            float target = steerIn * effectiveMax * steerSign;
            CurrentSteering = Mathf.MoveTowards(CurrentSteering, target, _steeringSpeed * dt);
        }

        private void ComputeTumbleFactor()
        {
            TiltAngle = Physics.TumbleMath.ComputeTiltAngle(transform.up);
            TumbleFactor = Physics.TumbleMath.ComputeTumbleFactor(
                TiltAngle, IsAirborne, _wasTumbling,
                _tumbleEngageDeg, _tumbleFullDeg, _tumbleHysteresisDeg);
            _wasTumbling = TumbleFactor > 0f;
        }

        private bool CheckAirborne()
        {
            bool offGround = true;
            foreach (var w in _allWheels)
            {
                if (w.IsOnGround)
                {
                    offGround = false;
                    break;
                }
            }

            _airborneFrames = offGround
                ? Mathf.Min(_airborneFrames + 1, k_AirborneThreshold)
                : 0;

            return _airborneFrames >= k_AirborneThreshold;
        }

        private void UpdatePhysicsMaterial()
        {
            if (_physMat == null) return;
            _physMat.bounciness = Mathf.Lerp(k_DefaultBounciness, _tumbleBounce, TumbleFactor);
            _physMat.dynamicFriction = Mathf.Lerp(0f, _tumbleFriction, TumbleFactor);
            _physMat.staticFriction = Mathf.Lerp(0f, _tumbleFriction, TumbleFactor);
        }

        private void DoFlip()
        {
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            transform.position += Vector3.up * k_FlipHeightOffset;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }
}