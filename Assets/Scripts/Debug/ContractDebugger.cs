using UnityEngine;
using R8EOX.Input;
using R8EOX.Vehicle;
using R8EOX.Debug.Validators;

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
        private Rigidbody _rb;
        private RaycastWheel[] _wheels;

        private ObservableContractValidator _observableValidator = new ObservableContractValidator();

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
            _observableValidator.Reset();
        }

        /// <summary>
        /// Validates all input contracts. Public for direct invocation in tests.
        /// </summary>
        public void ValidateInputContracts()
        {
            _inputViolations += InputContractValidator.ValidateInputContracts(_input, _logAllValues);
        }

        /// <summary>
        /// Validates all vehicle contracts. Public for direct invocation in tests.
        /// </summary>
        public void ValidateVehicleContracts()
        {
            _vehicleViolations += VehicleContractValidator.ValidateVehicleContracts(_car, _input, _logAllValues);
        }

        /// <summary>
        /// Validates all wheel contracts. Public for direct invocation in tests.
        /// </summary>
        public void ValidateWheelContracts()
        {
            _wheelViolations += WheelContractValidator.ValidateWheelContracts(_wheels, _logAllValues);
        }

        /// <summary>
        /// Validates observable consistency contracts. Public for direct invocation in tests.
        /// </summary>
        public void ValidateObservableContracts()
        {
            _observableViolations += _observableValidator.Validate(_car, _rb, _input, _logAllValues);
        }


        // ---- Private Helpers ----

        private void AcquireReferences()
        {
            if (_car == null)
                _car = FindFirstObjectByType<RCCar>();

            if (_car != null)
            {
                _input = _car.GetComponent<IVehicleInput>();
                _rb = _car.GetComponent<Rigidbody>();
                _wheels = _car.GetAllWheels();
            }
        }

#endif // UNITY_EDITOR || DEBUG
    }
}
