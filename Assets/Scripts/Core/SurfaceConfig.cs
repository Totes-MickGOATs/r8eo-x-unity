using UnityEngine;

namespace R8EOX.Core
{
    /// <summary>
    /// ScriptableObject defining a surface type's friction properties.
    /// Create via: Assets → Create → R8EOX → Surface Config
    /// </summary>
    [CreateAssetMenu(fileName = "NewSurface", menuName = "R8EOX/Surface Config")]
    public class SurfaceConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Surface type this config represents")]
        [SerializeField] private SurfaceType _surfaceType = SurfaceType.Dirt;
        [Tooltip("Human-readable name")]
        [SerializeField] private string _displayName = "Dirt";

        [Header("Friction")]
        [Tooltip("Grip coefficient multiplier (1.0 = full grip, 0.5 = half grip)")]
        [SerializeField] private float _gripMultiplier = 1.0f;
        [Tooltip("Rolling resistance factor (higher = more drag)")]
        [SerializeField] private float _rollingResistance = 0.02f;

        [Header("Visual")]
        [Tooltip("Color tint for debug visualization")]
        [SerializeField] private Color _debugColor = new Color(0.6f, 0.4f, 0.2f);

        // ---- Public Properties ----

        public SurfaceType SurfaceType => _surfaceType;
        public string DisplayName => _displayName;
        public float GripMultiplier => _gripMultiplier;
        public float RollingResistance => _rollingResistance;
        public Color DebugColor => _debugColor;
    }
}
