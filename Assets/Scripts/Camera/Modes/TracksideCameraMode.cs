using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// Trackside camera mode: fixed world position that rotates to track the car.
    /// Looks up the nearest <see cref="TracksideAnchor"/> in the scene; falls back
    /// to a computed side position when no anchor exists.
    /// Extracted from CameraController.ApplyTracksideMode / ComputeTracksidePosition.
    /// </summary>
    [System.Serializable]
    public class TracksideCameraMode : ICameraMode
    {
        // ---- Config Fields ----

        [Tooltip("Fallback distance when no TracksideAnchor exists in the scene")]
        public float FallbackDistance = 10f;

        [Tooltip("Fallback height when no TracksideAnchor exists in the scene")]
        public float FallbackHeight = 3f;

        [Tooltip("Height offset for the look-at point in metres")]
        public float LookHeight = 0.3f;


        // ---- Cached State ----

        private Vector3 _cachedPosition;
        private bool _positionSet;


        // ---- ICameraMode ----

        public void OnEnter(Transform cam, Transform target)
        {
            // Eagerly resolve anchor position on entry so the first frame is instant.
            if (target != null)
            {
                _cachedPosition = ResolvePosition(target);
                _positionSet = true;
            }
        }

        public void OnExit()
        {
            _positionSet = false;
        }

        /// <summary>
        /// Returns the cached trackside position + a look-at rotation toward the target.
        /// </summary>
        public CameraPose ComputeTargetPose(Transform target)
        {
            if (!_positionSet)
            {
                _cachedPosition = ResolvePosition(target);
                _positionSet = true;
            }

            Vector3 lookAt = target.position + Vector3.up * LookHeight;
            Vector3 dir = (lookAt - _cachedPosition).normalized;
            Quaternion rotation = dir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(dir)
                : Quaternion.identity;

            return new CameraPose(_cachedPosition, rotation);
        }


        // ---- Helpers ----

        private Vector3 ResolvePosition(Transform target)
        {
            TracksideAnchor anchor = FindNearestAnchor(target);
            return anchor != null ? anchor.CameraPosition : ComputeFallback(target);
        }

        private Vector3 ComputeFallback(Transform target)
        {
            Vector3 right = Vector3.Cross(Vector3.up, target.forward).normalized;
            return target.position
                   + right * FallbackDistance
                   + Vector3.up * FallbackHeight;
        }

        private static TracksideAnchor FindNearestAnchor(Transform target)
        {
            TracksideAnchor[] anchors = Object.FindObjectsByType<TracksideAnchor>(
                FindObjectsSortMode.None);

            if (anchors.Length == 0) return null;

            TracksideAnchor nearest = null;
            float nearestSqr = float.MaxValue;

            foreach (TracksideAnchor anchor in anchors)
            {
                float sqr = (anchor.transform.position - target.position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = anchor;
                }
            }

            return nearest;
        }
    }
}
