using UnityEngine;
using R8EOX.Core;

namespace R8EOX.Track
{
    /// <summary>
    /// Marks a trigger volume as a surface zone. When a wheel enters this zone,
    /// its grip coefficient is multiplied by the surface's grip multiplier.
    /// Attach to a GameObject with a Collider set to isTrigger=true.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SurfaceZone : MonoBehaviour
    {
        [Tooltip("Surface configuration for this zone")]
        [SerializeField] private SurfaceConfig _surfaceConfig;

        // ---- Public Properties ----

        /// <summary>The surface configuration assigned to this zone.</summary>
        public SurfaceConfig Surface => _surfaceConfig;

        /// <summary>Grip multiplier for this surface (1.0 = default, less = slippery).</summary>
        public float GripMultiplier => _surfaceConfig != null ? _surfaceConfig.GripMultiplier : 1f;

        /// <summary>The surface type enum for identification.</summary>
        public SurfaceType SurfaceType => _surfaceConfig != null ? _surfaceConfig.SurfaceType : SurfaceType.Dirt;


        // ---- Unity Lifecycle ----

        void Awake()
        {
            var col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                Debug.LogWarning($"[SurfaceZone] Collider on '{name}' must be a trigger. Setting isTrigger=true.");
                col.isTrigger = true;
            }

            if (_surfaceConfig == null)
                Debug.LogError($"[SurfaceZone] No SurfaceConfig assigned on '{name}'.");
        }
    }
}
