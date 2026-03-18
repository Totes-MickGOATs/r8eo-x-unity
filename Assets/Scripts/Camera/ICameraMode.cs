using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// Strategy interface for camera mode implementations.
    /// Each mode computes a desired camera pose given the target transform.
    /// </summary>
    public interface ICameraMode
    {
        /// <summary>Compute the desired world-space camera pose for this frame.</summary>
        CameraPose ComputeTargetPose(Transform target);

        /// <summary>Called when this mode becomes active. Use for initialisation.</summary>
        void OnEnter(Transform cam, Transform target);

        /// <summary>Called when this mode is deactivated.</summary>
        void OnExit();
    }
}
