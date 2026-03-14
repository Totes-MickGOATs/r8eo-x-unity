using UnityEngine;
using R8EOX.Vehicle.Config;
using PhysicsMath = R8EOX.Vehicle.Physics;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Physics-first airborne torques for RC buggy.
    /// Two gyroscopic equations produce emergent behavior:
    ///   1. Precession: yaw-pitch coupling from spinning wheels
    ///   2. Reaction: throttle pitches nose up, brake pitches nose down
    /// No direct input-to-torque mapping. All air control comes from wheel physics.
    /// </summary>
    public class RCAirPhysics : MonoBehaviour
    {
        // ---- Constants ----

        const float k_RpmToRadPerSec = Mathf.PI / 30f;


        // ---- Serialized Fields ----

        [Header("Gyroscopic Configuration")]
        [Tooltip("Wheel inertia and scale factors. Create via Assets > Create > R8EOX > Wheel Inertia Config")]
        [SerializeField] private WheelInertiaConfig _inertiaConfig;


        // ---- Fallback Defaults (used when no config asset assigned) ----

        const float k_DefaultWheelMoI = 0.000120f;
        const float k_DefaultGyroScale = 3.0f;
        const float k_DefaultReactionScale = 80.0f;


        // ---- Private Fields ----

        private Rigidbody _rb;
        private RaycastWheel[] _wheels;
        private float[] _prevWheelSpinRates;


        // ---- Properties ----

        /// <summary>Wheel moment of inertia from config or default.</summary>
        private float WheelMoI => _inertiaConfig != null ? _inertiaConfig.WheelMoI : k_DefaultWheelMoI;

        /// <summary>Gyroscopic scale from config or default.</summary>
        private float GyroScale => _inertiaConfig != null ? _inertiaConfig.GyroScale : k_DefaultGyroScale;

        /// <summary>Reaction scale from config or default.</summary>
        private float ReactionScale => _inertiaConfig != null ? _inertiaConfig.ReactionScale : k_DefaultReactionScale;


        // ---- Unity Lifecycle ----

        void Start()
        {
            _rb = GetComponentInParent<Rigidbody>();
            _wheels = GetComponentsInChildren<RaycastWheel>();
            if (_wheels.Length == 0)
                _wheels = transform.parent.GetComponentsInChildren<RaycastWheel>();

            _prevWheelSpinRates = new float[_wheels.Length];
        }


        // ---- Public API ----

        /// <summary>
        /// Apply gyroscopic torques. Called by RCCar when airborne.
        /// Throttle/brake/steer parameters are unused -- all torque comes from
        /// wheel spin physics, not direct input mapping.
        /// </summary>
        public void Apply(float dt, float throttle, float brake, float steer)
        {
            if (_wheels == null || _wheels.Length == 0 || dt <= 0f) return;

            Vector3 bodyOmega = _rb.angularVelocity;
            Vector3 totalGyroTorque = Vector3.zero;
            Vector3 totalReactionTorque = Vector3.zero;
            float wheelMoI = WheelMoI;

            for (int i = 0; i < _wheels.Length; i++)
            {
                Vector3 spinAxis = _wheels[i].transform.right;
                float currentSpinRate = _wheels[i].WheelRpm * k_RpmToRadPerSec;

                // Gyroscopic precession: tau = omega_body x (I * omega_spin * axis)
                totalGyroTorque += PhysicsMath.GyroscopicMath.ComputeGyroscopicTorque(
                    bodyOmega, spinAxis, wheelMoI, currentSpinRate);

                // Reaction torque: tau = -axis * I * d(omega)/dt
                if (_wheels[i].IsMotor)
                {
                    totalReactionTorque += PhysicsMath.GyroscopicMath.ComputeReactionTorque(
                        spinAxis, wheelMoI, currentSpinRate, _prevWheelSpinRates[i], dt);
                }

                _prevWheelSpinRates[i] = currentSpinRate;
            }

            Vector3 totalTorque = (totalGyroTorque * GyroScale)
                                + (totalReactionTorque * ReactionScale);

            _rb.AddTorque(totalTorque);
        }
    }
}
