using UnityEngine;
using PhysicsMath = R8EOX.Vehicle.Physics;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Airborne pitch/roll/gyro torques for RC buggy.
    /// Throttle creates pitch (nose up/down), steering creates roll,
    /// and spinning wheels provide gyroscopic stabilization.
    /// </summary>
    public class RCAirPhysics : MonoBehaviour
    {
        // ---- Constants ----

        const float k_MinRpmForGyro = 10f;


        // ---- Serialized Fields ----

        [Header("Pitch (throttle/brake in air)")]
        [Tooltip("Pitch torque magnitude in N*m")]
        [SerializeField] private float _pitchTorque = 40f;
        [Tooltip("Pitch sensitivity multiplier")]
        [SerializeField] private float _pitchSensitivity = 1f;

        [Header("Roll (steering in air)")]
        [Tooltip("Roll torque magnitude in N*m")]
        [SerializeField] private float _rollTorque = 12.8f;
        [Tooltip("Roll sensitivity multiplier")]
        [SerializeField] private float _rollSensitivity = 0.6f;

        [Header("Gyroscopic Stabilization")]
        [Tooltip("Damps angular velocity based on wheel spin speed. 0 = off.")]
        [SerializeField] private float _gyroStrength = 4f;
        [Tooltip("Wheel RPM at which full gyro effect is reached")]
        [SerializeField] private float _gyroFullRpm = 125f;


        // ---- Public Properties (read/write for tuning) ----

        /// <summary>Pitch torque magnitude in N*m.</summary>
        public float PitchTorque { get => _pitchTorque; set => _pitchTorque = value; }
        /// <summary>Pitch sensitivity multiplier.</summary>
        public float PitchSensitivity { get => _pitchSensitivity; set => _pitchSensitivity = value; }
        /// <summary>Roll torque magnitude in N*m.</summary>
        public float RollTorque { get => _rollTorque; set => _rollTorque = value; }
        /// <summary>Roll sensitivity multiplier.</summary>
        public float RollSensitivity { get => _rollSensitivity; set => _rollSensitivity = value; }
        /// <summary>Gyroscopic stabilization strength.</summary>
        public float GyroStrength { get => _gyroStrength; set => _gyroStrength = value; }
        /// <summary>Wheel RPM at which full gyro effect is reached.</summary>
        public float GyroFullRpm { get => _gyroFullRpm; set => _gyroFullRpm = value; }


        // ---- Private Fields ----

        private Rigidbody _rb;
        private RaycastWheel[] _wheels;


        // ---- Unity Lifecycle ----

        void Start()
        {
            _rb = GetComponentInParent<Rigidbody>();
            _wheels = GetComponentsInChildren<RaycastWheel>();
            if (_wheels.Length == 0)
                _wheels = transform.parent.GetComponentsInChildren<RaycastWheel>();
        }


        // ---- Public API ----

        /// <summary>
        /// Apply air control torques. Called by RCCar when airborne.
        /// </summary>
        public void Apply(float dt, float throttle, float brake, float steer)
        {
            float pitchForce = PhysicsMath.AirPhysicsMath.ComputePitchTorque(
                throttle, brake, _pitchTorque, _pitchSensitivity);
            _rb.AddTorque(-_rb.transform.right * pitchForce);

            float rollForce = PhysicsMath.AirPhysicsMath.ComputeRollTorque(
                steer, _rollTorque, _rollSensitivity);
            _rb.AddTorque(_rb.transform.forward * rollForce);

            float[] wheelRpms = GetWheelRpmArray();
            float avgRpm = PhysicsMath.AirPhysicsMath.ComputeAverageAbsRpm(wheelRpms);
            float gyroFactor = PhysicsMath.AirPhysicsMath.ComputeGyroDampingFactor(
                avgRpm, _gyroStrength, _gyroFullRpm);

            if (gyroFactor > 0f)
            {
                Vector3 damp = -_rb.angularVelocity * gyroFactor;
                damp.y = 0f; // Allow yaw
                _rb.AddTorque(damp);
            }
        }


        // ---- Private Methods ----

        private float[] GetWheelRpmArray()
        {
            if (_wheels == null || _wheels.Length == 0) return System.Array.Empty<float>();
            float[] rpms = new float[_wheels.Length];
            for (int i = 0; i < _wheels.Length; i++)
                rpms[i] = _wheels[i].WheelRpm;
            return rpms;
        }
    }
}
