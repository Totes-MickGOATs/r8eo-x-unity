using UnityEngine;
using UnityEngine.InputSystem;

namespace R8EOX.Camera
{
    /// <summary>
    /// Orbit camera mode: rotates around the target via right stick or mouse drag.
    /// Extracted from CameraController.ApplyOrbitMode / UpdateOrbitInput.
    /// </summary>
    [System.Serializable]
    public class OrbitCameraMode : ICameraMode
    {
        // ---- Constants ----

        const float k_MinPitch = -20f;
        const float k_MaxPitch = 80f;
        const float k_DefaultYaw = 0f;
        const float k_DefaultPitch = 20f;
        const float k_MouseScale = 0.1f;


        // ---- Config Fields ----

        [Tooltip("Distance from the target in metres")]
        public float Distance = 5f;

        [Tooltip("Mouse/stick sensitivity for orbit rotation in degrees per unit")]
        public float Sensitivity = 150f;

        [Tooltip("Height offset for the orbit look-at point in metres")]
        public float LookHeight = 0.5f;

        [Tooltip("Input action for orbit camera look (right stick / mouse delta)")]
        public InputActionReference OrbitAction;

        [Tooltip("Input action for enabling mouse orbit (right mouse button)")]
        public InputActionReference OrbitEnableAction;


        // ---- Orbit State ----

        private float _yaw;
        private float _pitch;


        // ---- ICameraMode ----

        public void OnEnter(Transform cam, Transform target)
        {
            if (cam == null || target == null)
            {
                _yaw = k_DefaultYaw;
                _pitch = k_DefaultPitch;
                return;
            }

            // Derive initial yaw/pitch from the camera's current offset.
            Vector3 offset = cam.position - (target.position + Vector3.up * LookHeight);
            float mag = offset.magnitude;

            _yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            _pitch = mag > 0.001f
                ? Mathf.Asin(Mathf.Clamp(offset.y / mag, -1f, 1f)) * Mathf.Rad2Deg
                : k_DefaultPitch;

            _pitch = Mathf.Clamp(_pitch, k_MinPitch, k_MaxPitch);
        }

        public void OnExit() { }

        public CameraPose ComputeTargetPose(Transform target)
        {
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 lookAt = target.position + Vector3.up * LookHeight;
            Vector3 position = lookAt + rot * (Vector3.back * Distance);

            Vector3 dir = (lookAt - position).normalized;
            Quaternion camRot = dir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(dir)
                : Quaternion.identity;

            return new CameraPose(position, camRot);
        }


        // ---- Input ----

        /// <summary>
        /// Read orbit input and update yaw/pitch state. Call from Update.
        /// </summary>
        public void UpdateInput()
        {
            float yawDelta = 0f;
            float pitchDelta = 0f;

            if (OrbitAction != null && OrbitAction.action != null)
            {
                Vector2 stick = OrbitAction.action.ReadValue<Vector2>();
                yawDelta = stick.x;
                pitchDelta = stick.y;
            }

            bool mouseEnabled = OrbitEnableAction != null
                && OrbitEnableAction.action != null
                && OrbitEnableAction.action.IsPressed();

            if (mouseEnabled && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yawDelta += delta.x * k_MouseScale;
                pitchDelta -= delta.y * k_MouseScale;
            }

            float dt = Time.deltaTime;
            _yaw += yawDelta * Sensitivity * dt;
            _pitch += pitchDelta * Sensitivity * dt;
            _pitch = Mathf.Clamp(_pitch, k_MinPitch, k_MaxPitch);
        }


        // ---- Accessors (for tests) ----

        /// <summary>Current yaw in degrees (wraps freely).</summary>
        public float Yaw => _yaw;

        /// <summary>Current pitch in degrees, clamped to [MinPitch, MaxPitch].</summary>
        public float Pitch => _pitch;

        public float MinPitch => k_MinPitch;
        public float MaxPitch => k_MaxPitch;
    }
}
