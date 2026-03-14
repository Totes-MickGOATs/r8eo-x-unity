using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// Defines a trackside camera position. Place these in the scene
    /// at locations where a static camera should watch the car pass by.
    /// The <see cref="CameraController"/> selects the nearest anchor
    /// when entering Trackside mode.
    /// </summary>
    public class TracksideAnchor : MonoBehaviour
    {
        // ---- Serialized Fields ----

        [Tooltip("Height offset above the anchor transform for the camera eye")]
        [SerializeField] private float _heightOffset = 1.5f;


        // ---- Public Properties ----

        /// <summary>Height offset above the anchor for the camera eye position.</summary>
        public float HeightOffset => _heightOffset;

        /// <summary>World-space camera position including the height offset.</summary>
        public Vector3 CameraPosition => transform.position + Vector3.up * _heightOffset;
    }
}