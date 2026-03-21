using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// Static helpers for CameraController — mode-to-strategy mapping and pose application.
    /// Extracted to keep CameraController.cs under 150 lines.
    /// </summary>
    public static class CameraControllerHelpers
    {
        /// <summary>Apply a CameraPose to a Transform in one call.</summary>
        public static void ApplyPose(Transform transform, CameraPose pose)
        {
            transform.position = pose.Position;
            transform.rotation = pose.Rotation;
        }
    }
}
