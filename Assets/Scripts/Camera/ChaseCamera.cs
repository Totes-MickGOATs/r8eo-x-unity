using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// Legacy chase camera. Use <see cref="CameraController"/> instead.
    /// Kept for backward compatibility with existing scene references.
    /// On Awake, migrates to CameraController if one is not already present.
    /// </summary>
    [System.Obsolete("Use CameraController instead. This class exists only for scene migration.")]
    public class ChaseCamera : MonoBehaviour
    {
        // ---- Serialized Fields ----

        [Tooltip("The transform to follow (typically the RC buggy root)")]
        [SerializeField] private Transform _target;

        [Tooltip("Distance behind the target in metres")]
        [SerializeField] private float _followDistance = 3f;

        [Tooltip("Height above the target in metres")]
        [SerializeField] private float _followHeight = 1.5f;

        [Tooltip("Height offset for the look-at point")]
        [SerializeField] private float _lookHeight = 0.3f;

        [Tooltip("Smoothing speed (higher = snappier)")]
        [SerializeField] private float _smoothSpeed = 5f;


        // ---- Public Properties ----

        /// <summary>The transform this camera follows. Set via inspector or code.</summary>
        public Transform Target { get => _target; set => _target = value; }


        // ---- Unity Lifecycle ----

        void LateUpdate()
        {
            if (_target == null) return;

            float dt = Time.deltaTime;
            Vector3 targetPos = _target.position
                                - _target.forward * _followDistance
                                + Vector3.up * _followHeight;
            transform.position = Vector3.Lerp(
                transform.position, targetPos, _smoothSpeed * dt);
            transform.LookAt(_target.position + Vector3.up * _lookHeight);
        }
    }
}