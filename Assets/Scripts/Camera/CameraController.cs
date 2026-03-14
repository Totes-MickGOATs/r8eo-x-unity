using UnityEngine;

namespace R8EOX.Camera
{
    /// <summary>
    /// Multi-mode camera controller for the RC buggy.
    /// Supports Chase, Orbit, FPV, and Trackside modes with
    /// smooth transitions between them.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        // ---- Constants ----

        const float k_MinOrbitPitch = -20f;
        const float k_MaxOrbitPitch = 80f;
        const float k_DefaultOrbitYaw = 0f;
        const float k_DefaultOrbitPitch = 20f;
        const float k_TransitionCompleteThreshold = 0.99f;
        const float k_FpvForwardOffset = 0.1f;


        // ---- Serialized Fields ----

        [Header("Target")]
        [Tooltip("The transform to follow (typically the RC buggy root)")]
        [SerializeField] private Transform _target;

        [Header("Mode")]
        [Tooltip("Key used to cycle through camera modes")]
        [SerializeField] private KeyCode _cycleModeKey = KeyCode.C;

        [Tooltip("Speed of the smooth transition between modes")]
        [SerializeField] private float _transitionSpeed = 5f;

        [Header("Chase Mode")]
        [Tooltip("Distance behind the target in metres")]
        [SerializeField] private float _chaseDistance = 3f;

        [Tooltip("Height above the target in metres")]
        [SerializeField] private float _chaseHeight = 1.5f;

        [Tooltip("Height offset for the look-at point in metres")]
        [SerializeField] private float _chaseLookHeight = 0.3f;

        [Tooltip("Position smoothing speed (higher = snappier)")]
        [SerializeField] private float _chaseSmoothSpeed = 5f;

        [Header("Orbit Mode")]
        [Tooltip("Distance from the target in metres")]
        [SerializeField] private float _orbitDistance = 5f;

        [Tooltip("Mouse/stick sensitivity for orbit rotation in degrees per unit")]
        [SerializeField] private float _orbitSensitivity = 150f;

        [Tooltip("Height offset for the orbit look-at point in metres")]
        [SerializeField] private float _orbitLookHeight = 0.5f;

        [Header("FPV Mode")]
        [Tooltip("Local position offset for the FPV camera on the car body")]
        [SerializeField] private Vector3 _fpvLocalOffset = new Vector3(0f, 0.15f, 0.3f);

        [Tooltip("Local rotation offset in euler angles for the FPV camera")]
        [SerializeField] private Vector3 _fpvLocalRotation = Vector3.zero;

        [Header("Trackside Mode")]
        [Tooltip("Fallback distance when no TracksideAnchor exists in the scene")]
        [SerializeField] private float _tracksideFallbackDistance = 10f;

        [Tooltip("Fallback height when no TracksideAnchor exists in the scene")]
        [SerializeField] private float _tracksideFallbackHeight = 3f;

        [Tooltip("Height offset for the trackside look-at point in metres")]
        [SerializeField] private float _tracksideLookHeight = 0.3f;


        // ---- Public Properties ----

        /// <summary>The transform this camera follows. Set via inspector or code.</summary>
        public Transform Target { get => _target; set => _target = value; }

        /// <summary>The currently active camera mode.</summary>
        public CameraMode CurrentMode => _currentMode;


        // ---- Private Fields ----

        private CameraMode _currentMode = CameraMode.Chase;
        private float _orbitYaw;
        private float _orbitPitch;
        private Vector3 _transitionStartPos;
        private Quaternion _transitionStartRot;
        private float _transitionProgress = 1f;
        private Vector3 _tracksidePosition;
        private bool _tracksidePositionSet;


        // ---- Unity Lifecycle ----

        void Start()
        {
            _orbitYaw = k_DefaultOrbitYaw;
            _orbitPitch = k_DefaultOrbitPitch;
        }

        void Update()
        {
            if (Input.GetKeyDown(_cycleModeKey))
            {
                CycleMode();
            }

            if (_currentMode == CameraMode.Orbit)
            {
                UpdateOrbitInput();
            }
        }

        void LateUpdate()
        {
            if (_target == null) return;

            if (IsTransitioning())
            {
                ApplyTransition();
                return;
            }

            ApplyCurrentMode();
        }


        // ---- Public Methods ----

        /// <summary>
        /// Switch to a specific camera mode with a smooth transition.
        /// </summary>
        public void SetMode(CameraMode mode)
        {
            if (mode == _currentMode) return;

            BeginTransition();
            _currentMode = mode;
            OnModeEntered(mode);
        }

        /// <summary>
        /// Cycle to the next camera mode in sequence.
        /// </summary>
        public void CycleMode()
        {
            int next = ((int)_currentMode + 1) % 4;
            SetMode((CameraMode)next);
        }


        // ---- Private Methods: Mode Logic ----

        private void ApplyCurrentMode()
        {
            switch (_currentMode)
            {
                case CameraMode.Chase:
                    ApplyChaseMode();
                    break;
                case CameraMode.Orbit:
                    ApplyOrbitMode();
                    break;
                case CameraMode.Fpv:
                    ApplyFpvMode();
                    break;
                case CameraMode.Trackside:
                    ApplyTracksideMode();
                    break;
            }
        }

        private void ApplyChaseMode()
        {
            float dt = Time.deltaTime;
            Vector3 desiredPos = ComputeChasePosition();

            transform.position = Vector3.Lerp(
                transform.position, desiredPos, _chaseSmoothSpeed * dt);

            Vector3 lookTarget = _target.position + Vector3.up * _chaseLookHeight;
            transform.LookAt(lookTarget);
        }

        private Vector3 ComputeChasePosition()
        {
            return _target.position
                   - _target.forward * _chaseDistance
                   + Vector3.up * _chaseHeight;
        }

        private void ApplyOrbitMode()
        {
            Quaternion rotation = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            Vector3 offset = rotation * (Vector3.back * _orbitDistance);
            Vector3 lookTarget = _target.position + Vector3.up * _orbitLookHeight;

            transform.position = lookTarget + offset;
            transform.LookAt(lookTarget);
        }

        private void UpdateOrbitInput()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            bool rightMouseHeld = Input.GetMouseButton(1);
            float rightStickX = GetAxisSafe("RightStickHorizontal");
            float rightStickY = GetAxisSafe("RightStickVertical");

            float yawInput = rightStickX;
            float pitchInput = rightStickY;

            if (rightMouseHeld)
            {
                yawInput += mouseX;
                pitchInput -= mouseY;
            }

            float dt = Time.deltaTime;
            _orbitYaw += yawInput * _orbitSensitivity * dt;
            _orbitPitch += pitchInput * _orbitSensitivity * dt;
            _orbitPitch = Mathf.Clamp(_orbitPitch, k_MinOrbitPitch, k_MaxOrbitPitch);
        }

        private void ApplyFpvMode()
        {
            transform.position = _target.TransformPoint(_fpvLocalOffset);
            transform.rotation = _target.rotation
                                 * Quaternion.Euler(_fpvLocalRotation);
        }

        private void ApplyTracksideMode()
        {
            if (!_tracksidePositionSet)
            {
                _tracksidePosition = ComputeTracksidePosition();
                _tracksidePositionSet = true;
            }

            transform.position = _tracksidePosition;
            Vector3 lookTarget = _target.position
                                 + Vector3.up * _tracksideLookHeight;
            transform.LookAt(lookTarget);
        }

        private Vector3 ComputeTracksidePosition()
        {
            TracksideAnchor anchor = FindNearestAnchor();
            if (anchor != null)
            {
                return anchor.CameraPosition;
            }

            return ComputeTracksideFallback();
        }

        private Vector3 ComputeTracksideFallback()
        {
            Vector3 right = Vector3.Cross(Vector3.up, _target.forward).normalized;
            return _target.position
                   + right * _tracksideFallbackDistance
                   + Vector3.up * _tracksideFallbackHeight;
        }

        private TracksideAnchor FindNearestAnchor()
        {
            TracksideAnchor[] anchors = Object.FindObjectsByType<TracksideAnchor>(
                FindObjectsSortMode.None);

            if (anchors.Length == 0) return null;

            TracksideAnchor nearest = null;
            float nearestSqr = float.MaxValue;

            foreach (TracksideAnchor anchor in anchors)
            {
                float sqr = (anchor.transform.position - _target.position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = anchor;
                }
            }

            return nearest;
        }


        // ---- Private Methods: Transitions ----

        private void BeginTransition()
        {
            _transitionStartPos = transform.position;
            _transitionStartRot = transform.rotation;
            _transitionProgress = 0f;
        }

        private bool IsTransitioning()
        {
            return _transitionProgress < k_TransitionCompleteThreshold;
        }

        private void ApplyTransition()
        {
            _transitionProgress += Time.deltaTime * _transitionSpeed;
            _transitionProgress = Mathf.Clamp01(_transitionProgress);

            float t = Mathf.SmoothStep(0f, 1f, _transitionProgress);

            ComputeTargetPoseForMode(
                out Vector3 targetPos, out Quaternion targetRot);

            transform.position = Vector3.Lerp(_transitionStartPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(_transitionStartRot, targetRot, t);

            if (_transitionProgress >= k_TransitionCompleteThreshold)
            {
                _transitionProgress = 1f;
            }
        }

        private void ComputeTargetPoseForMode(
            out Vector3 position, out Quaternion rotation)
        {
            switch (_currentMode)
            {
                case CameraMode.Chase:
                    position = ComputeChasePosition();
                    rotation = ComputeLookRotation(
                        position, _target.position + Vector3.up * _chaseLookHeight);
                    break;
                case CameraMode.Orbit:
                    Quaternion orbitRot = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
                    Vector3 lookPt = _target.position + Vector3.up * _orbitLookHeight;
                    position = lookPt + orbitRot * (Vector3.back * _orbitDistance);
                    rotation = ComputeLookRotation(position, lookPt);
                    break;
                case CameraMode.Fpv:
                    position = _target.TransformPoint(_fpvLocalOffset);
                    rotation = _target.rotation * Quaternion.Euler(_fpvLocalRotation);
                    break;
                case CameraMode.Trackside:
                    position = _tracksidePositionSet
                        ? _tracksidePosition
                        : ComputeTracksidePosition();
                    rotation = ComputeLookRotation(
                        position,
                        _target.position + Vector3.up * _tracksideLookHeight);
                    break;
                default:
                    position = transform.position;
                    rotation = transform.rotation;
                    break;
            }
        }

        private Quaternion ComputeLookRotation(
            Vector3 from, Vector3 to)
        {
            Vector3 dir = (to - from).normalized;
            if (dir.sqrMagnitude < 0.001f) return transform.rotation;
            return Quaternion.LookRotation(dir);
        }

        private void OnModeEntered(CameraMode mode)
        {
            if (mode == CameraMode.Orbit)
            {
                InitOrbitFromCurrentView();
            }
            else if (mode == CameraMode.Trackside)
            {
                _tracksidePosition = ComputeTracksidePosition();
                _tracksidePositionSet = true;
            }
        }

        private void InitOrbitFromCurrentView()
        {
            if (_target == null) return;

            Vector3 offset = transform.position
                             - (_target.position + Vector3.up * _orbitLookHeight);

            _orbitYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            _orbitPitch = Mathf.Asin(
                Mathf.Clamp(offset.y / offset.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
            _orbitPitch = Mathf.Clamp(_orbitPitch, k_MinOrbitPitch, k_MaxOrbitPitch);
        }


        // ---- Private Methods: Utility ----

        private static float GetAxisSafe(string axisName)
        {
            try
            {
                return Input.GetAxis(axisName);
            }
            catch (System.ArgumentException)
            {
                return 0f;
            }
        }
    }
}