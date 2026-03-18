using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// Chase camera mode: follows behind and above the target with smooth lerp.
    /// Extracted from CameraController.ApplyChaseMode / ComputeChasePosition.
    /// </summary>
    [System.Serializable]
    public class ChaseCameraMode : ICameraMode
    {
        // ---- Config Fields ----

        [Tooltip("Distance behind the target in metres")]
        public float Distance = 3f;

        [Tooltip("Height above the target in metres")]
        public float Height = 1.5f;

        [Tooltip("Height offset for the look-at point in metres")]
        public float LookHeight = 0.3f;

        [Tooltip("Position smoothing speed (higher = snappier)")]
        public float SmoothSpeed = 5f;


        // ---- Private State ----

        private Transform _cam;


        // ---- ICameraMode ----

        public void OnEnter(Transform cam, Transform target)
        {
            _cam = cam;
        }

        public void OnExit()
        {
            _cam = null;
        }

        /// <summary>
        /// Returns the desired chase position behind the target and a look-at rotation.
        /// Smoothing is applied by CameraController each frame using the SmoothSpeed field.
        /// </summary>
        public CameraPose ComputeTargetPose(Transform target)
        {
            Vector3 desired = ComputeDesiredPosition(target);
            Vector3 lookAt = target.position + Vector3.up * LookHeight;
            Quaternion rotation = ComputeLookRotation(desired, lookAt);
            return new CameraPose(desired, rotation);
        }

        /// <summary>
        /// Apply smooth follow: lerps current camera position toward the desired pose.
        /// Call each LateUpdate when not transitioning.
        /// </summary>
        public void ApplySmooth(Transform cam, Transform target)
        {
            if (cam == null || target == null) return;

            float dt = Time.deltaTime;
            Vector3 desired = ComputeDesiredPosition(target);
            cam.position = Vector3.Lerp(cam.position, desired, SmoothSpeed * dt);

            Vector3 lookAt = target.position + Vector3.up * LookHeight;
            cam.LookAt(lookAt);
        }


        // ---- Helpers ----

        internal Vector3 ComputeDesiredPosition(Transform target)
        {
            return target.position
                   - target.forward * Distance
                   + Vector3.up * Height;
        }

        private static Quaternion ComputeLookRotation(Vector3 from, Vector3 to)
        {
            Vector3 dir = (to - from).normalized;
            if (dir.sqrMagnitude < 0.001f) return Quaternion.identity;
            return Quaternion.LookRotation(dir);
        }
    }
}
