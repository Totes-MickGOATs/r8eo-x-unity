using UnityEngine;

namespace R8EOX.Vehicle
{
    /// <summary>
    /// Inspector-serializable configuration for a single <see cref="RaycastWheel"/>.
    /// Holds only the design-time values that are set in the Unity Inspector.
    /// Runtime state (SpringStrength, GripCoeff, etc.) is kept on RaycastWheel directly.
    /// </summary>
    [System.Serializable]
    public class WheelConfig
    {
        [Header("Suspension")]
        public float restDistance   = 0.25f;
        public float overExtend     = 0.24f;
        public float maxSpringForce = 500f;
        public float minSpringLen   = 0.12f;

        [Header("Wheel")]
        public float wheelRadius = 0.420f;

        [Header("Motor/Steer")]
        public bool isMotor;
        public bool isSteer;

        [Header("Traction")]
        public float zTraction      = 0.10f;
        public float zBrakeTraction = 0.5f;
        public AnimationCurve gripCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.15f, 0.8f),
            new Keyframe(0.4f, 1.0f), new Keyframe(1.0f, 0.7f));

        [Header("Ground Detection")]
        public LayerMask groundMask = ~0;

        [Header("Debug")]
        public bool showDebug;
    }
}
