using UnityEngine;

namespace R8EOX.Vehicle.Config
{
    /// <summary>
    /// Wheel inertia and gyroscopic tuning parameters.
    /// Controls the strength of gyroscopic precession and reaction torque
    /// during airborne flight. Game-feel multipliers allow boosting the
    /// physically accurate values for better gameplay.
    /// </summary>
    [CreateAssetMenu(menuName = "R8EOX/Wheel Inertia Config")]
    public class WheelInertiaConfig : ScriptableObject
    {
        [Tooltip("Moment of inertia per wheel (kg*m^2). Typical 1/1 (10x RC): 0.120")]
        [SerializeField] private float _wheelMoI = 0.120f;

        [Tooltip("Game-feel multiplier for gyroscopic effect. 1.0 = physically accurate.")]
        [SerializeField] private float _gyroScale = 3.0f;

        [Tooltip("Game-feel multiplier for reaction torque (pitch control). 1.0 = physically accurate.")]
        [SerializeField] private float _reactionScale = 80.0f;

        /// <summary>Moment of inertia per wheel in kg*m^2.</summary>
        public float WheelMoI => _wheelMoI;

        /// <summary>Game-feel multiplier for gyroscopic precession torque.</summary>
        public float GyroScale => _gyroScale;

        /// <summary>Game-feel multiplier for reaction torque (throttle/brake pitch).</summary>
        public float ReactionScale => _reactionScale;
    }
}
