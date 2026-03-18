using UnityEngine;
using UnityEngine.InputSystem;

namespace R8EOX.Camera
{
    /// <summary>
    /// Multi-mode camera controller for the RC buggy.
    /// Delegates per-mode logic to <see cref="ICameraMode"/> strategy objects.
    /// Supports Chase, Orbit, FPV, and Trackside modes with smooth transitions.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        // ---- Serialized Fields ----

        [Header("Target")]
        [Tooltip("The transform to follow (typically the RC buggy root)")]
        [SerializeField] private Transform _target;

        [Header("Mode")]
        [Tooltip("Input action for cycling through camera modes")]
        [SerializeField] private InputActionReference _cameraCycleAction;

        [Tooltip("Speed of the smooth transition between modes")]
        [SerializeField] private float _transitionSpeed = 5f;

        [Header("Chase Mode")]
        [SerializeField] private ChaseCameraMode _chaseMode = new ChaseCameraMode();

        [Header("Orbit Mode")]
        [SerializeField] private OrbitCameraMode _orbitMode = new OrbitCameraMode();

        [Header("FPV Mode")]
        [SerializeField] private FpvCameraMode _fpvMode = new FpvCameraMode();

        [Header("Trackside Mode")]
        [SerializeField] private TracksideCameraMode _tracksideMode = new TracksideCameraMode();


        // ---- Public Properties ----

        /// <summary>The transform this camera follows. Set via inspector or code.</summary>
        public Transform Target { get => _target; set => _target = value; }

        /// <summary>The currently active camera mode.</summary>
        public CameraMode CurrentMode => _currentMode;


        // ---- Private Fields ----

        private CameraMode _currentMode = CameraMode.Chase;
        private ICameraMode _activeStrategy;
        private readonly CameraTransition _transition = new CameraTransition();


        // ---- Unity Lifecycle ----

        void Start()
        {
            _activeStrategy = _chaseMode;
            _activeStrategy.OnEnter(transform, _target);
        }

        void OnEnable()
        {
            _cameraCycleAction?.action?.Enable();
            _orbitMode.OrbitAction?.action?.Enable();
            _orbitMode.OrbitEnableAction?.action?.Enable();
        }

        void OnDisable()
        {
            _cameraCycleAction?.action?.Disable();
            _orbitMode.OrbitAction?.action?.Disable();
            _orbitMode.OrbitEnableAction?.action?.Disable();
        }

        void Update()
        {
            if (_cameraCycleAction != null && _cameraCycleAction.action.WasPressedThisFrame())
                CycleMode();

            if (_currentMode == CameraMode.Orbit)
                _orbitMode.UpdateInput();
        }

        void LateUpdate()
        {
            if (_target == null) return;

            if (_transition.IsActive)
            {
                CameraPose pose = _transition.Advance();
                ApplyPose(pose);
                return;
            }

            if (_currentMode == CameraMode.Chase)
            {
                _chaseMode.ApplySmooth(transform, _target);
            }
            else
            {
                CameraPose pose = _activeStrategy.ComputeTargetPose(_target);
                ApplyPose(pose);
            }
        }


        // ---- Public Methods ----

        /// <summary>Switch to a specific camera mode with a smooth transition.</summary>
        public void SetMode(CameraMode mode)
        {
            if (mode == _currentMode) return;

            CameraPose startPose = new CameraPose(transform.position, transform.rotation);
            _activeStrategy.OnExit();

            _currentMode = mode;
            _activeStrategy = ModeToStrategy(mode);
            _activeStrategy.OnEnter(transform, _target);

            CameraPose endPose = _activeStrategy.ComputeTargetPose(_target);
            _transition.Begin(startPose, endPose, _transitionSpeed);
        }

        /// <summary>Cycle to the next camera mode in sequence.</summary>
        public void CycleMode()
        {
            int next = ((int)_currentMode + 1) % 4;
            SetMode((CameraMode)next);
        }


        // ---- Helpers ----

        private ICameraMode ModeToStrategy(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.Chase:     return _chaseMode;
                case CameraMode.Orbit:     return _orbitMode;
                case CameraMode.Fpv:       return _fpvMode;
                case CameraMode.Trackside: return _tracksideMode;
                default:                   return _chaseMode;
            }
        }

        private void ApplyPose(CameraPose pose)
        {
            transform.position = pose.Position;
            transform.rotation = pose.Rotation;
        }
    }
}
