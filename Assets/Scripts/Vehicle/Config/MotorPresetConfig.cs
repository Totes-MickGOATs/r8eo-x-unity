using UnityEngine;

namespace R8EOX.Vehicle.Config
{
    /// <summary>
    /// ScriptableObject defining a motor preset with engine, braking, and throttle parameters.
    /// Create via: Assets → Create → R8EOX → Motor Preset
    /// </summary>
    [CreateAssetMenu(fileName = "NewMotorPreset", menuName = "R8EOX/Motor Preset")]
    public class MotorPresetConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Human-readable name for this motor (e.g. '17.5T Stock')")]
        [SerializeField] private string _displayName = "Custom Motor";
        [Tooltip("Motor turn rating (lower = faster, e.g. 17.5)")]
        [SerializeField] private float _turnRating = 17.5f;

        [Header("Engine (Newtons)")]
        [Tooltip("Peak driving force in Newtons")]
        [SerializeField] private float _engineForceMax = 18f;
        [Tooltip("Maximum speed in m/s")]
        [SerializeField] private float _maxSpeed = 20f;
        [Tooltip("Braking force in Newtons (~85% of engine)")]
        [SerializeField] private float _brakeForce = 15.3f;
        [Tooltip("Reverse force in Newtons (~55% of forward)")]
        [SerializeField] private float _reverseForce = 9.9f;
        [Tooltip("Drivetrain drag force while coasting in Newtons")]
        [SerializeField] private float _coastDrag = 2.5f;

        [Header("Throttle Response")]
        [Tooltip("Ramp rate from 0 to 1 in units/sec")]
        [SerializeField] private float _throttleRampUp = 4.0f;
        [Tooltip("Ramp rate from 1 to 0 in units/sec")]
        [SerializeField] private float _throttleRampDown = 10f;

        // ---- Public Properties ----

        public string DisplayName => _displayName;
        public float TurnRating => _turnRating;
        public float EngineForceMax => _engineForceMax;
        public float MaxSpeed => _maxSpeed;
        public float BrakeForce => _brakeForce;
        public float ReverseForce => _reverseForce;
        public float CoastDrag => _coastDrag;
        public float ThrottleRampUp => _throttleRampUp;
        public float ThrottleRampDown => _throttleRampDown;
    }
}
