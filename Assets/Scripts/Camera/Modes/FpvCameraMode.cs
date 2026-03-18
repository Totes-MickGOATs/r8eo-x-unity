using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// First-person-view camera mode: fixed to the car body at a local offset.
    /// Extracted from CameraController.ApplyFpvMode.
    /// </summary>
    [System.Serializable]
    public class FpvCameraMode : ICameraMode
    {
        // ---- Config Fields ----

        [Tooltip("Local position offset on the car body")]
        public Vector3 LocalOffset = new Vector3(0f, 0.15f, 0.3f);

        [Tooltip("Local rotation offset in euler angles")]
        public Vector3 LocalRotationEuler = Vector3.zero;


        // ---- ICameraMode ----

        public void OnEnter(Transform cam, Transform target) { }

        public void OnExit() { }

        /// <summary>
        /// Returns the world-space pose matching the car body's local offset/rotation.
        /// </summary>
        public CameraPose ComputeTargetPose(Transform target)
        {
            Vector3 position = target.TransformPoint(LocalOffset);
            Quaternion rotation = target.rotation * Quaternion.Euler(LocalRotationEuler);
            return new CameraPose(position, rotation);
        }
    }
}
