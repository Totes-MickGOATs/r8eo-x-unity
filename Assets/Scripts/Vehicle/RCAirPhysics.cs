using UnityEngine;

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
            // Pitch: throttle -> nose UP, brake -> nose DOWN
            float pitchInput = throttle - brake;
            float pitchForce = pitchInput * _pitchTorque * _pitchSensitivity;
            _rb.AddTorque(-_rb.transform.right * pitchForce);

            // Roll: counter-roll from steering input
            float rollForce = steer * _rollTorque * _rollSensitivity;
            _rb.AddTorque(_rb.transform.forward * rollForce);

            // Gyroscopic stabilization: resist tumbling based on wheel spin
            float avgRpm = GetAvgWheelRpm();
            if (avgRpm > k_MinRpmForGyro)
            {
                float gyroFactor = Mathf.Min(avgRpm / _gyroFullRpm, 1f) * _gyroStrength;
                Vector3 damp = -_rb.angularVelocity * gyroFactor;
                damp.y = 0f; // Allow yaw
                _rb.AddTorque(damp);
            }
        }


        // ---- Private Methods ----

        private float GetAvgWheelRpm()
        {
            if (_wheels == null || _wheels.Length == 0) return 0f;
            float total = 0f;
            foreach (var w in _wheels)
                total += Mathf.Abs(w.WheelRpm);
            return total / _wheels.Length;
        }
    }
}
