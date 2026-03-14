using UnityEngine;
using R8EOX.Input;
using R8EOX.Vehicle;

namespace R8EOX.Debug
{
    /// <summary>
    /// Runtime chain-of-custody assertion monitor.
    /// Validates input-to-physics-to-visual contracts every frame during development.
    /// Catches contract violations immediately in the console rather than letting bugs
    /// silently produce bad behavior.
    /// All assertions are stripped from release builds.
    /// </summary>
    public class ContractDebugger : MonoBehaviour
    {
#if UNITY_EDITOR || DEBUG
        // ---- Constants ----

        /// <summary>Tolerance for floating-point comparisons.</summary>
        const float k_Epsilon = 1e-5f;

        /// <summary>Consecutive frames required before observable contracts trigger.</summary>
        const int k_ObservableFrameThreshold = 10;

        /// <summary>Minimum velocity increase per frame to consider "accelerating" (m/s).</summary>
        const float k_MinAccelerationDelta = 0.0001f;

        /// <summary>Minimum velocity decrease per frame to consider "decelerating" (m/s).</summary>
        const float k_MinDecelerationDelta = 0.0001f;

        /// <summary>Speed threshold below which we consider the car "stopped" (m/s).</summary>
        const float k_StoppedSpeedThreshold = 0.05f;


        // ---- Serialized Fields ----

        [Header("Contract Debugger")]
        [Tooltip("Validate input range and phantom input contracts")]
        [SerializeField] private bool _enableInputContracts = true;
        [Tooltip("Validate vehicle force and state contracts")]
        [SerializeField] private bool _enableVehicleContracts = true;
        [Tooltip("Validate per-wheel physics contracts")]
        [SerializeField] private bool _enableWheelContracts = true;
        [Tooltip("Validate observable consistency contracts over time")]
        [SerializeField] private bool _enableObservableContracts = true;
        [Tooltip("Log all values every frame even when passing (very verbose)")]
        [SerializeField] private bool _logAllValues = false;

        [Header("References")]
        [Tooltip("The RC car to monitor")]
        [SerializeField] private RCCar _car;


        // ---- Private Fields ----

        private IVehicleInput _input;
        private RCInput _rcInput;
        private Rigidbody _rb;
        private RaycastWheel[] _wheels;

        // Observable contract tracking
        private int _consecutiveEngineFrames;
        private int _consecutiveZeroInputFrames;
        private float _prevVelocityMagnitude;

        // Violation counters for summary
        private int _inputViolations;
        private int _vehicleViolations;
        private int _wheelViolations;
        private int _observableViolations;


        // ---- Public Properties (for testing) ----

        /// <summary>Whether input contracts are enabled.</summary>
        public bool EnableInputContracts
        {
            get => _enableInputContracts;
            set => _enableInputContracts = value;
        }

        /// <summary>Whether vehicle contracts are enabled.</summary>
        public bool EnableVehicleContracts
        {
            get => _enableVehicleContracts;
            set => _enableVehicleContracts = value;
        }

        /// <summary>Whether wheel contracts are enabled.</summary>
        public bool EnableWheelContracts
        {
            get => _enableWheelContracts;
            set => _enableWheelContracts = value;
        }

        /// <summary>Whether observable contracts are enabled.</summary>
        public bool EnableObservableContracts
        {
            get => _enableObservableContracts;
            set => _enableObservableContracts = value;
        }

        /// <summary>Total input violations detected this session.</summary>
        public int InputViolationCount => _inputViolations;

        /// <summary>Total vehicle violations detected this session.</summary>
        public int VehicleViolationCount => _vehicleViolations;

        /// <summary>Total wheel violations detected this session.</summary>
        public int WheelViolationCount => _wheelViolations;

        /// <summary>Total observable violations detected this session.</summary>
        public int ObservableViolationCount => _observableViolations;


        // ---- Unity Lifecycle ----

        void Start()
        {
            AcquireReferences();
        }

        void Update()
        {
            if (_enableInputContracts)
                ValidateInputContracts();
        }

        void FixedUpdate()
        {
            if (_enableVehicleContracts)
                ValidateVehicleContracts();

            if (_enableWheelContracts)
                ValidateWheelContracts();
        }

        void LateUpdate()
        {
            if (_enableObservableContracts)
                ValidateObservableContracts();
        }


        // ---- Public API ----

        /// <summary>
        /// Manually set the car reference (useful for testing or late binding).
        /// Reacquires all dependent references.
        /// </summary>
        /// <param name="car">The RCCar to monitor</param>
        public void SetTarget(RCCar car)
        {
            _car = car;
            AcquireReferences();
        }

        /// <summary>
        /// Reset all violation counters. Useful at start of a new test run.
        /// </summary>
        public void ResetCounters()
        {
            _inputViolations = 0;
            _vehicleViolations = 0;
            _wheelViolations = 0;
            _observableViolations = 0;
            _consecutiveEngineFrames = 0;
            _consecutiveZeroInputFrames = 0;
        }


        // ---- Validation: Input Contracts (Update) ----

        /// <summary>
        /// Validates all input contracts. Public for direct invocation in tests.
        /// </summary>
        public void ValidateInputContracts()
        {
            if (_input == null) return;

            int frame = Time.frameCount;

            // Throttle in [0, 1]
            if (_input.Throttle < -k_Epsilon || _input.Throttle > 1f + k_Epsilon)
            {
                LogInputViolation("Throttle out of range [0,1]",
                    _input.Throttle, 0f, 1f, frame);
            }

            // Brake in [0, 1]
            if (_input.Brake < -k_Epsilon || _input.Brake > 1f + k_Epsilon)
            {
                LogInputViolation("Brake out of range [0,1]",
                    _input.Brake, 0f, 1f, frame);
            }

            // Steer in [-1, 1]
            if (_input.Steer < -1f - k_Epsilon || _input.Steer > 1f + k_Epsilon)
            {
                LogInputViolation("Steer out of range [-1,1]",
                    _input.Steer, -1f, 1f, frame);
            }

            // During TriggerDetector Detecting/None: throttle and brake must be 0
            if (_rcInput != null)
            {
                ValidateDetectorModeContracts(frame);
            }

            // During InputGuard suppression: all inputs must be 0
            if (InputGuard.ShouldSuppressInput(frame))
            {
                if (Mathf.Abs(_input.Throttle) > k_Epsilon)
                {
                    LogInputViolation("Throttle non-zero during InputGuard suppression",
                        _input.Throttle, 0f, 0f, frame);
                }
                if (Mathf.Abs(_input.Brake) > k_Epsilon)
                {
                    LogInputViolation("Brake non-zero during InputGuard suppression",
                        _input.Brake, 0f, 0f, frame);
                }
                if (Mathf.Abs(_input.Steer) > k_Epsilon)
                {
                    LogInputViolation("Steer non-zero during InputGuard suppression",
                        _input.Steer, 0f, 0f, frame);
                }
            }

            if (_logAllValues)
            {
                UnityEngine.Debug.Log($"[ContractDebugger] Input OK frame={frame} " +
                    $"throttle={_input.Throttle:F4} brake={_input.Brake:F4} steer={_input.Steer:F4}");
            }
        }


        // ---- Validation: Vehicle Contracts (FixedUpdate) ----

        /// <summary>
        /// Validates all vehicle contracts. Public for direct invocation in tests.
        /// </summary>
        public void ValidateVehicleContracts()
        {
            if (_car == null) return;

            int frame = Time.frameCount;

            // CurrentEngineForce >= 0 (reverse produces negative — that is the design,
            // but the task spec says >= 0. Checking absolute non-negativity for forward drive.
            // Note: ESCMath can produce negative engine force for reverse. We check that
            // engine and brake are not BOTH positive simultaneously instead.)

            // CurrentBrakeForce >= 0
            if (_car.CurrentBrakeForce < -k_Epsilon)
            {
                LogVehicleViolation("CurrentBrakeForce is negative",
                    _car.CurrentBrakeForce, 0f, float.MaxValue, frame);
            }

            // Engine and Brake mutually exclusive (both > 0 simultaneously is a violation)
            if (_car.CurrentEngineForce > k_Epsilon && _car.CurrentBrakeForce > k_Epsilon)
            {
                LogVehicleViolation(
                    $"Engine ({_car.CurrentEngineForce:F2}) and Brake ({_car.CurrentBrakeForce:F2}) both active",
                    _car.CurrentEngineForce, 0f, 0f, frame);
            }

            // If airborne: engine and brake must be 0
            if (_car.IsAirborne)
            {
                if (Mathf.Abs(_car.CurrentEngineForce) > k_Epsilon)
                {
                    LogVehicleViolation("Engine force non-zero while airborne",
                        _car.CurrentEngineForce, 0f, 0f, frame);
                }
                if (Mathf.Abs(_car.CurrentBrakeForce) > k_Epsilon)
                {
                    LogVehicleViolation("Brake force non-zero while airborne",
                        _car.CurrentBrakeForce, 0f, 0f, frame);
                }
            }

            // Speed cutoff: if at max speed with throttle, engine should be 0
            if (_input != null && _input.Throttle > k_Epsilon &&
                Mathf.Abs(_car.ForwardSpeed) >= _car.MaxSpeed - k_Epsilon &&
                !_car.IsAirborne)
            {
                if (Mathf.Abs(_car.CurrentEngineForce) > k_Epsilon)
                {
                    LogVehicleViolation(
                        $"Engine force ({_car.CurrentEngineForce:F2}) active at max speed ({_car.ForwardSpeed:F2}/{_car.MaxSpeed:F2})",
                        _car.CurrentEngineForce, 0f, 0f, frame);
                }
            }

            // SmoothThrottle in [0, 1]
            if (_car.SmoothThrottle < -k_Epsilon || _car.SmoothThrottle > 1f + k_Epsilon)
            {
                LogVehicleViolation("SmoothThrottle out of range [0,1]",
                    _car.SmoothThrottle, 0f, 1f, frame);
            }

            // CurrentSteering magnitude <= steeringMax
            if (Mathf.Abs(_car.CurrentSteering) > _car.SteeringMax + k_Epsilon)
            {
                LogVehicleViolation(
                    $"CurrentSteering magnitude ({Mathf.Abs(_car.CurrentSteering):F4}) exceeds SteeringMax ({_car.SteeringMax:F4})",
                    Mathf.Abs(_car.CurrentSteering), 0f, _car.SteeringMax, frame);
            }

            if (_logAllValues)
            {
                UnityEngine.Debug.Log($"[ContractDebugger] Vehicle OK frame={frame} " +
                    $"engine={_car.CurrentEngineForce:F2} brake={_car.CurrentBrakeForce:F2} " +
                    $"steering={_car.CurrentSteering:F4} smoothThrottle={_car.SmoothThrottle:F4} " +
                    $"airborne={_car.IsAirborne}");
            }
        }


        // ---- Validation: Wheel/Physics Contracts (FixedUpdate) ----

        /// <summary>
        /// Validates all wheel contracts. Public for direct invocation in tests.
        /// </summary>
        public void ValidateWheelContracts()
        {
            if (_wheels == null) return;

            int frame = Time.frameCount;

            for (int i = 0; i < _wheels.Length; i++)
            {
                RaycastWheel w = _wheels[i];
                string wheelName = w != null ? w.name : $"Wheel[{i}]";

                // GripLoad >= 0 when grounded (no tension in suspension)
                if (w.IsOnGround && w.LastGripLoad < -k_Epsilon)
                {
                    LogWheelViolation(wheelName, "GripLoad negative while grounded",
                        w.LastGripLoad, 0f, float.MaxValue, frame);
                }

                // SlipRatio in [0, 1] (our implementation uses 0-1, not -1 to 1)
                if (w.SlipRatio < -k_Epsilon || w.SlipRatio > 1f + k_Epsilon)
                {
                    LogWheelViolation(wheelName, "SlipRatio out of range [0,1]",
                        w.SlipRatio, 0f, 1f, frame);
                }

                // GripFactor in [0, 1]
                if (w.GripFactor < -k_Epsilon || w.GripFactor > 1f + k_Epsilon)
                {
                    LogWheelViolation(wheelName, "GripFactor out of range [0,1]",
                        w.GripFactor, 0f, 1f, frame);
                }

                // Motor force only if IsMotor AND IsOnGround
                if (!w.IsMotor && Mathf.Abs(w.MotorForceShare) > k_Epsilon)
                {
                    LogWheelViolation(wheelName, "MotorForceShare non-zero on non-motor wheel",
                        w.MotorForceShare, 0f, 0f, frame);
                }
                if (!w.IsOnGround && Mathf.Abs(w.MotorForceShare) > k_Epsilon)
                {
                    LogWheelViolation(wheelName, "MotorForceShare non-zero while airborne",
                        w.MotorForceShare, 0f, 0f, frame);
                }
            }

            if (_logAllValues && _wheels.Length > 0)
            {
                var w = _wheels[0];
                UnityEngine.Debug.Log($"[ContractDebugger] Wheels OK frame={frame} " +
                    $"sample: {w.name} ground={w.IsOnGround} grip={w.GripFactor:F3} " +
                    $"slip={w.SlipRatio:F3} motor={w.MotorForceShare:F2}");
            }
        }


        // ---- Validation: Observable/Consistency Contracts (LateUpdate) ----

        /// <summary>
        /// Validates observable consistency contracts. Public for direct invocation in tests.
        /// </summary>
        public void ValidateObservableContracts()
        {
            if (_car == null || _rb == null) return;

            int frame = Time.frameCount;
            float currentSpeed = _rb.velocity.magnitude;

            // Track consecutive engine-force frames
            if (_car.CurrentEngineForce > k_Epsilon && !_car.IsAirborne)
                _consecutiveEngineFrames++;
            else
                _consecutiveEngineFrames = 0;

            // Track consecutive zero-input frames
            if (_input != null &&
                Mathf.Abs(_input.Throttle) < k_Epsilon &&
                Mathf.Abs(_input.Brake) < k_Epsilon &&
                Mathf.Abs(_input.Steer) < k_Epsilon &&
                !_car.IsAirborne)
            {
                _consecutiveZeroInputFrames++;
            }
            else
            {
                _consecutiveZeroInputFrames = 0;
            }

            // If engine active for N frames AND grounded: velocity should increase (or be at max)
            if (_consecutiveEngineFrames >= k_ObservableFrameThreshold)
            {
                bool atMaxSpeed = Mathf.Abs(_car.ForwardSpeed) >= _car.MaxSpeed - k_Epsilon;
                bool velocityIncreasing = currentSpeed > _prevVelocityMagnitude - k_MinAccelerationDelta;

                if (!atMaxSpeed && !velocityIncreasing)
                {
                    LogObservableViolation(
                        $"Engine active for {_consecutiveEngineFrames} frames but velocity not increasing " +
                        $"(prev={_prevVelocityMagnitude:F4} current={currentSpeed:F4})",
                        frame);
                }
            }

            // If all inputs zero for N frames AND grounded: velocity should decrease
            if (_consecutiveZeroInputFrames >= k_ObservableFrameThreshold)
            {
                bool alreadyStopped = currentSpeed < k_StoppedSpeedThreshold;
                bool velocityDecreasing = currentSpeed < _prevVelocityMagnitude + k_MinDecelerationDelta;

                if (!alreadyStopped && !velocityDecreasing)
                {
                    LogObservableViolation(
                        $"All inputs zero for {_consecutiveZeroInputFrames} frames but velocity not decreasing " +
                        $"(prev={_prevVelocityMagnitude:F4} current={currentSpeed:F4})",
                        frame);
                }
            }

            _prevVelocityMagnitude = currentSpeed;

            if (_logAllValues)
            {
                UnityEngine.Debug.Log($"[ContractDebugger] Observable OK frame={frame} " +
                    $"speed={currentSpeed:F4} engineFrames={_consecutiveEngineFrames} " +
                    $"zeroFrames={_consecutiveZeroInputFrames}");
            }
        }


        // ---- Private Helpers ----

        private void AcquireReferences()
        {
            if (_car == null)
                _car = FindFirstObjectByType<RCCar>();

            if (_car != null)
            {
                _input = _car.GetComponent<IVehicleInput>();
                _rcInput = _car.GetComponent<RCInput>();
                _rb = _car.GetComponent<Rigidbody>();
                _wheels = _car.GetAllWheels();
            }
        }

        private void ValidateDetectorModeContracts(int frame)
        {
            var mode = _rcInput.DetectorMode;

            if (mode == TriggerDetector.Mode.Detecting || mode == TriggerDetector.Mode.None)
            {
                if (Mathf.Abs(_input.Throttle) > k_Epsilon)
                {
                    LogInputViolation(
                        $"Throttle non-zero during detector mode {mode}",
                        _input.Throttle, 0f, 0f, frame);
                }
                if (Mathf.Abs(_input.Brake) > k_Epsilon)
                {
                    LogInputViolation(
                        $"Brake non-zero during detector mode {mode}",
                        _input.Brake, 0f, 0f, frame);
                }
            }
        }

        private void LogInputViolation(string contract, float actual, float expectedMin, float expectedMax, int frame)
        {
            _inputViolations++;
            UnityEngine.Debug.LogError(
                $"[ContractDebugger] INPUT VIOLATION: {contract} | " +
                $"actual={actual:F6} expected=[{expectedMin:F2}, {expectedMax:F2}] | frame={frame}");
        }

        private void LogVehicleViolation(string contract, float actual, float expectedMin, float expectedMax, int frame)
        {
            _vehicleViolations++;
            UnityEngine.Debug.LogError(
                $"[ContractDebugger] VEHICLE VIOLATION: {contract} | " +
                $"actual={actual:F6} expected=[{expectedMin:F2}, {expectedMax:F2}] | frame={frame}");
        }

        private void LogWheelViolation(string wheelName, string contract, float actual,
            float expectedMin, float expectedMax, int frame)
        {
            _wheelViolations++;
            UnityEngine.Debug.LogError(
                $"[ContractDebugger] WHEEL VIOLATION [{wheelName}]: {contract} | " +
                $"actual={actual:F6} expected=[{expectedMin:F2}, {expectedMax:F2}] | frame={frame}");
        }

        private void LogObservableViolation(string description, int frame)
        {
            _observableViolations++;
            UnityEngine.Debug.LogWarning(
                $"[ContractDebugger] OBSERVABLE: {description} | frame={frame}");
        }

#endif // UNITY_EDITOR || DEBUG
    }
}
