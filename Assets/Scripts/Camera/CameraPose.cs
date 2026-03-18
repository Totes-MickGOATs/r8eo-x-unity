using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// Immutable world-space camera pose: position and orientation.
    /// Returned by <see cref="ICameraMode.ComputeTargetPose"/> and consumed
    /// by <see cref="CameraTransition"/> and <see cref="CameraController"/>.
    /// </summary>
    public struct CameraPose
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public CameraPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
