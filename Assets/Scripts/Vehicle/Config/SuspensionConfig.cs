using UnityEngine;

namespace R8EOX.Vehicle.Config
{
    /// <summary>
    /// ScriptableObject defining suspension parameters.
    /// Create via: Assets → Create → R8EOX → Suspension Config
    /// </summary>
    [CreateAssetMenu(fileName = "NewSuspensionConfig", menuName = "R8EOX/Suspension Config")]
    public class SuspensionConfig : ScriptableObject
    {
        [Header("Spring")]
        [Tooltip("Spring stiffness in N/m")]
        [SerializeField] private float _springStrength = 75f;
        [Tooltip("Damping coefficient")]
        [SerializeField] private float _springDamping = 4.25f;

        [Header("Travel")]
        [Tooltip("Suspension rest distance in metres (×10 scale)")]
        [SerializeField] private float _restDistance = 2.0f;
        [Tooltip("Extra droop extension when airborne in metres (×10 scale)")]
        [SerializeField] private float _overExtend = 0.8f;
        [Tooltip("Bump stop minimum spring length in metres (×10 scale)")]
        [SerializeField] private float _minSpringLen = 0.32f;
        [Tooltip("Maximum suspension force clamp in Newtons")]
        [SerializeField] private float _maxSpringForce = 50f;

        // ---- Public Properties ----

        public float SpringStrength => _springStrength;
        public float SpringDamping => _springDamping;
        public float RestDistance => _restDistance;
        public float OverExtend => _overExtend;
        public float MinSpringLen => _minSpringLen;
        public float MaxSpringForce => _maxSpringForce;
    }
}
