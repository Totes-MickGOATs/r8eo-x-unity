using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// All inspector-tunable parameters for RCCar in one serialisable object.
    /// Held as a <c>[SerializeField]</c> on RCCar and exposed via read-only
    /// properties on that component. Tuning setters write directly to this struct.
    /// </summary>
    [System.Serializable]
    public sealed class VehicleParams
    {
        // ---- Motor ----

        [Header("Motor")]
        [Tooltip("Select a motor preset or Custom for manual tuning")]
        public MotorPreset MotorPreset = MotorPreset.Motor13_5T;

        // ---- Engine ----

        [Header("Engine")]
        [Tooltip("Peak driving force in Newtons")]
        public float EngineForceMax = 260f;
        [Tooltip("Maximum speed in m/s")]
        public float MaxSpeed = 27f;
        [Tooltip("Braking force in Newtons (~85% of engine)")]
        public float BrakeForce = 221f;
        [Tooltip("Reverse force in Newtons (~55% of forward)")]
        public float ReverseForce = 143f;
        [Tooltip("Drivetrain drag force while coasting in Newtons")]
        public float CoastDrag = 30f;

        // ---- Throttle Response ----

        [Header("Throttle Response")]
        [Tooltip("Ramp rate from 0 to 1 in units/sec")]
        public float ThrottleRampUp = 5.5f;
        [Tooltip("Ramp rate from 1 to 0 in units/sec")]
        public float ThrottleRampDown = 10f;

        // ---- Steering ----

        [Header("Steering")]
        [Tooltip("Max steering angle in radians (~29 deg)")]
        public float SteeringMax = 0.50f;
        [Tooltip("Steering ramp speed in rad/s")]
        public float SteeringSpeed = 7f;
        [Tooltip("Speed in m/s at which steering reduces")]
        public float SteeringSpeedLimit = 8f;
        [Range(0f, 1f)]
        [Tooltip("Fraction of steeringMax kept at high speed")]
        public float SteeringHighSpeedFactor = 0.4f;

        // ---- Suspension ----

        [Header("Suspension — Front")]
        [Tooltip("Front axle spring stiffness in N/m (B6.4 red spring = 4.0 lbs/in = 700 N/m)")]
        public float FrontSpringStrength = 700.0f;
        [Tooltip("Front axle damping coefficient in N·s/m")]
        public float FrontSpringDamping = 41.0f;

        [Header("Suspension — Rear")]
        [Tooltip("Rear axle spring stiffness in N/m (B6.4 gray spring = 2.0 lbs/in = 350 N/m)")]
        public float RearSpringStrength = 350.0f;
        [Tooltip("Rear axle damping coefficient in N·s/m")]
        public float RearSpringDamping = 29.0f;

        // ---- Traction ----

        [Header("Traction")]
        [Range(0f, 1f)]
        [Tooltip("Global grip multiplier (0-1)")]
        public float GripCoeff = 0.7f;

        // ---- Centre of Mass ----

        [Header("Centre of Mass")]
        [Tooltip("Centre of mass offset")]
        public Vector3 ComGround = new Vector3(0f, -0.12f, 0f);

        // ---- Crash Physics ----

        [Header("Crash Physics")]
        [Tooltip("Tilt angle in degrees where tumble blending begins")]
        public float TumbleEngageDeg = 50f;
        [Tooltip("Tilt angle in degrees for full tumble effect")]
        public float TumbleFullDeg = 70f;
        [Range(0f, 1f)]
        [Tooltip("Bounciness coefficient during tumble")]
        public float TumbleBounce = 0.35f;
        [Range(0f, 1f)]
        [Tooltip("Friction coefficient during tumble")]
        public float TumbleFriction = 0.3f;
        [Tooltip("Hysteresis band in degrees to prevent threshold oscillation")]
        public float TumbleHysteresisDeg = 5f;
        [Tooltip("Enable dynamic bounciness/friction blending when tumbling.")]
        public bool EnableDynamicPhysicsMaterial = true;
    }
}
