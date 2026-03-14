using UnityEngine;

namespace R8EOX.Vehicle.Config
{
    /// <summary>
    /// ScriptableObject defining traction and grip parameters.
    /// Create via: Assets → Create → R8EOX → Traction Config
    /// </summary>
    [CreateAssetMenu(fileName = "NewTractionConfig", menuName = "R8EOX/Traction Config")]
    public class TractionConfig : ScriptableObject
    {
        [Header("Grip")]
        [Tooltip("Base grip coefficient (0-1)")]
        [SerializeField] private float _gripCoeff = 0.7f;
        [Tooltip("Grip curve mapping slip ratio to grip factor")]
        [SerializeField] private AnimationCurve _gripCurve = new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.15f, 0.8f),
            new Keyframe(0.4f, 1.0f),
            new Keyframe(1.0f, 0.7f)
        );

        [Header("Longitudinal Friction")]
        [Tooltip("Normal longitudinal slip traction factor")]
        [SerializeField] private float _zTraction = 0.10f;
        [Tooltip("Braking friction boost factor")]
        [SerializeField] private float _zBrakeTraction = 0.5f;

        // ---- Public Properties ----

        public float GripCoeff => _gripCoeff;
        public AnimationCurve GripCurve => _gripCurve;
        public float ZTraction => _zTraction;
        public float ZBrakeTraction => _zBrakeTraction;
    }
}
