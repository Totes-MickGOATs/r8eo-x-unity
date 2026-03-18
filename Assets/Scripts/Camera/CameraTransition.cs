using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// Manages a smooth lerp/slerp transition between two <see cref="CameraPose"/> values.
    /// Uses Mathf.SmoothStep for an ease-in/ease-out curve.
    /// Extracted from CameraController.BeginTransition / ApplyTransition.
    /// </summary>
    public class CameraTransition
    {
        // ---- Constants ----

        const float k_CompleteThreshold = 0.99f;


        // ---- State ----

        private CameraPose _start;
        private CameraPose _end;
        private float _progress;
        private float _speed;
        private bool _active;


        // ---- Public API ----

        /// <summary>True while a transition is in progress.</summary>
        public bool IsActive => _active && _progress < k_CompleteThreshold;

        /// <summary>Normalised 0–1 progress of the current transition.</summary>
        public float Progress => _progress;

        /// <summary>
        /// Start a new transition from <paramref name="start"/> toward <paramref name="end"/>.
        /// </summary>
        public void Begin(CameraPose start, CameraPose end, float speed)
        {
            _start = start;
            _end = end;
            _speed = speed;
            _progress = 0f;
            _active = true;
        }

        /// <summary>
        /// Advance the transition by one frame and return the interpolated pose.
        /// Call each LateUpdate while <see cref="IsActive"/> is true.
        /// </summary>
        public CameraPose Advance()
        {
            _progress += Time.deltaTime * _speed;
            _progress = Mathf.Clamp01(_progress);

            float t = Mathf.SmoothStep(0f, 1f, _progress);

            Vector3 pos = Vector3.Lerp(_start.Position, _end.Position, t);
            Quaternion rot = Quaternion.Slerp(_start.Rotation, _end.Rotation, t);

            if (_progress >= k_CompleteThreshold)
            {
                _progress = 1f;
                _active = false;
            }

            return new CameraPose(pos, rot);
        }

        /// <summary>
        /// Evaluate the interpolated pose at an arbitrary normalised time without
        /// advancing internal state. Useful for previewing or testing.
        /// </summary>
        public CameraPose Evaluate(float t)
        {
            float smooth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            Vector3 pos = Vector3.Lerp(_start.Position, _end.Position, smooth);
            Quaternion rot = Quaternion.Slerp(_start.Rotation, _end.Rotation, smooth);
            return new CameraPose(pos, rot);
        }
    }
}
