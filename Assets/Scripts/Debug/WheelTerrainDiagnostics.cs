using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug
{
    /// <summary>
    /// Runtime diagnostics for detecting terrain snags, contact anomalies, and suspension spikes.
    /// Logs tagged messages ([physics], [suspension]) that DebugLogSink captures to SQLite.
    /// Attach to the same GameObject as <see cref="RCCar"/> or any child with access to wheels.
    /// </summary>
    public class WheelTerrainDiagnostics : MonoBehaviour
    {
        // ---- Constants ----

        const float k_VerySteepThreshold = 0.7f;
        const int k_HighResHeightmap = 513;


        // ---- Serialized Fields ----

        [Header("Target")]
        [Tooltip("The RC car to monitor. Auto-detected from this or parent if unset.")]
        [SerializeField] private RCCar _car;

        [Header("Normal Deviation")]
        [Tooltip("Contact normal Y below this triggers a steep-normal warning (1.0 = flat, 0.0 = vertical)")]
        [SerializeField] private float _normalDeviationThreshold = 0.85f;

        [Header("Suspension Force")]
        [Tooltip("Frame-over-frame suspension force delta (in Newtons) that triggers a spike warning")]
        [SerializeField] private float _forceSpikeThreshold = 30f;

        [Header("Velocity")]
        [Tooltip("Tire velocity magnitude change (m/s per frame) that triggers a discontinuity warning")]
        [SerializeField] private float _velocityDiscontinuityThreshold = 2f;

        [Header("Contact Point")]
        [Tooltip("Contact point jump distance (metres per frame) that triggers a warning while grounded")]
        [SerializeField] private float _contactJumpThreshold = 0.05f;

        [Header("Ground Flicker")]
        [Tooltip("Number of physics frames in the sliding window for flicker detection")]
        [SerializeField] private int _flickerWindowFrames = 10;

        [Tooltip("Number of ground-state toggles within the window that triggers a flicker warning")]
        [SerializeField] private int _flickerCountThreshold = 3;

        [Header("Log Throttle")]
        [Tooltip("Minimum seconds between logs of the same type per wheel to prevent spam")]
        [SerializeField] private float _logCooldownSeconds = 0.5f;

        [Header("Scene Debug Drawing")]
        [Tooltip("Draw contact normals in Scene view (green=normal, yellow=steep, red=very steep)")]
        [SerializeField] private bool _drawContactNormals = true;


        // ---- Nested Types ----

        /// <summary>Per-wheel tracking state for frame-over-frame comparisons.</summary>
        private struct WheelState
        {
            public float PrevSuspensionForce;
            public Vector3 PrevTireVelocity;
            public Vector3 PrevContactPoint;
            public bool PrevIsOnGround;

            // Ground-state flicker ring buffer
            public bool[] GroundHistory;
            public int GroundHistoryIndex;

            // Per-detection-type cooldown timestamps
            public float LastNormalLogTime;
            public float LastForceLogTime;
            public float LastVelocityLogTime;
            public float LastContactLogTime;
            public float LastFlickerLogTime;
        }


        // ---- Private Fields ----

        private RaycastWheel[] _wheels;
        private WheelState[] _states;


        // ---- Unity Lifecycle ----

        void Awake()
        {
            if (_car == null)
                _car = GetComponentInParent<RCCar>();

            if (_car == null)
            {
                UnityEngine.Debug.LogError(
                    $"[physics] {nameof(WheelTerrainDiagnostics)} could not find RCCar — disabling.");
                enabled = false;
                return;
            }
        }

        void Start()
        {
            _wheels = _car.GetAllWheels();
            if (_wheels == null || _wheels.Length == 0)
            {
                UnityEngine.Debug.LogError(
                    $"[physics] {nameof(WheelTerrainDiagnostics)} found no wheels on {_car.name} — disabling.");
                enabled = false;
                return;
            }

            InitStates();
            CheckCollisionDetectionMode();
            CheckTerrainCollider();
        }

        void FixedUpdate()
        {
            if (_wheels == null) return;

            for (int i = 0; i < _wheels.Length; i++)
            {
                var wheel = _wheels[i];
                if (wheel == null) continue;

                ref WheelState state = ref _states[i];

                if (wheel.IsOnGround)
                {
                    CheckNormalDeviation(wheel, ref state);
                    CheckForceSpikeDetection(wheel, ref state);
                    CheckVelocityDiscontinuity(wheel, ref state);
                    CheckContactPointJump(wheel, ref state);

                    if (_drawContactNormals)
                        DrawContactNormal(wheel);
                }

                CheckGroundFlicker(wheel, ref state);

                // Store current frame as previous for next frame
                state.PrevSuspensionForce = wheel.SuspensionForce;
                state.PrevTireVelocity = wheel.TireVelocity;
                state.PrevContactPoint = wheel.ContactPoint;
                state.PrevIsOnGround = wheel.IsOnGround;
            }
        }


        // ---- Startup Checks ----

        private void InitStates()
        {
            _states = new WheelState[_wheels.Length];
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i] = new WheelState
                {
                    GroundHistory = new bool[_flickerWindowFrames],
                    GroundHistoryIndex = 0
                };
            }
        }

        private void CheckCollisionDetectionMode()
        {
            var rb = _car.GetComponent<Rigidbody>();
            if (rb == null) return;

            if (rb.collisionDetectionMode != CollisionDetectionMode.ContinuousSpeculative
                && rb.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic)
            {
                UnityEngine.Debug.LogWarning(
                    $"[physics] {_car.name} Rigidbody uses {rb.collisionDetectionMode} collision detection " +
                    $"— consider ContinuousSpeculative or ContinuousDynamic for terrain snag prevention");
            }
        }

        private void CheckTerrainCollider()
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return;

            var terrainCollider = terrain.GetComponent<TerrainCollider>();
            if (terrainCollider == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[physics] Active terrain has no TerrainCollider — wheel raycasts will miss it");
                return;
            }

            var terrainData = terrain.terrainData;
            if (terrainData == null) return;

            int resolution = terrainData.heightmapResolution;
            UnityEngine.Debug.Log(
                $"[physics] Terrain heightmap resolution: {resolution}x{resolution}");

            if (resolution > k_HighResHeightmap)
            {
                UnityEngine.Debug.Log(
                    $"[physics] High-res heightmap ({resolution}) may cause micro-seams — " +
                    "consider a simplified collider mesh for RC-scale vehicles");
            }
        }


        // ---- Per-Frame Checks ----

        private void CheckNormalDeviation(RaycastWheel wheel, ref WheelState state)
        {
            float normalY = wheel.ContactNormal.y;
            if (normalY >= _normalDeviationThreshold) return;

            if (!CanLog(ref state.LastNormalLogTime)) return;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} hit steep normal ({wheel.ContactNormal}) " +
                $"at {wheel.ContactPoint} — possible terrain seam");
        }

        private void CheckForceSpikeDetection(RaycastWheel wheel, ref WheelState state)
        {
            float delta = Mathf.Abs(wheel.SuspensionForce - state.PrevSuspensionForce);
            if (delta <= _forceSpikeThreshold) return;

            // Skip the first frame after landing — damping sanitisation handles that
            if (!state.PrevIsOnGround) return;

            if (!CanLog(ref state.LastForceLogTime)) return;

            UnityEngine.Debug.LogWarning(
                $"[suspension] {wheel.name} force spike: {state.PrevSuspensionForce:F1}" +
                $"→{wheel.SuspensionForce:F1} (Δ{delta:F1}N) at {wheel.ContactPoint}");
        }

        private void CheckVelocityDiscontinuity(RaycastWheel wheel, ref WheelState state)
        {
            if (!state.PrevIsOnGround) return;

            float prevSpeed = state.PrevTireVelocity.magnitude;
            float currentSpeed = wheel.TireVelocity.magnitude;
            float deltaMag = (wheel.TireVelocity - state.PrevTireVelocity).magnitude;

            if (deltaMag <= _velocityDiscontinuityThreshold) return;
            if (!CanLog(ref state.LastVelocityLogTime)) return;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} velocity discontinuity: {prevSpeed:F2}→{currentSpeed:F2} m/s " +
                $"(Δ{deltaMag:F2}) at {wheel.ContactPoint}");
        }

        private void CheckContactPointJump(RaycastWheel wheel, ref WheelState state)
        {
            if (!state.PrevIsOnGround) return;

            float distance = Vector3.Distance(wheel.ContactPoint, state.PrevContactPoint);
            if (distance <= _contactJumpThreshold) return;
            if (!CanLog(ref state.LastContactLogTime)) return;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} contact point jump: {distance:F3}m in one frame " +
                $"at {wheel.ContactPoint}");
        }

        private void CheckGroundFlicker(RaycastWheel wheel, ref WheelState state)
        {
            // Record current ground state in ring buffer
            state.GroundHistory[state.GroundHistoryIndex] = wheel.IsOnGround;
            state.GroundHistoryIndex = (state.GroundHistoryIndex + 1) % _flickerWindowFrames;

            // Count toggles within the window
            int toggleCount = 0;
            for (int j = 1; j < _flickerWindowFrames; j++)
            {
                int curr = (state.GroundHistoryIndex - 1 - j + _flickerWindowFrames * 2) % _flickerWindowFrames;
                int prev = (curr - 1 + _flickerWindowFrames) % _flickerWindowFrames;
                if (state.GroundHistory[curr] != state.GroundHistory[prev])
                    toggleCount++;
            }

            if (toggleCount < _flickerCountThreshold) return;
            if (!CanLog(ref state.LastFlickerLogTime)) return;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} ground contact flickering — {toggleCount} toggles " +
                $"in {_flickerWindowFrames} frames at {wheel.transform.position}");
        }


        // ---- Debug Drawing ----

        private void DrawContactNormal(RaycastWheel wheel)
        {
            float normalY = wheel.ContactNormal.y;
            Color color;

            if (normalY < k_VerySteepThreshold)
                color = Color.red;
            else if (normalY < _normalDeviationThreshold)
                color = Color.yellow;
            else
                color = Color.green;

            UnityEngine.Debug.DrawRay(wheel.ContactPoint, wheel.ContactNormal * 0.3f, color);
        }


        // ---- Helpers ----

        private bool CanLog(ref float lastLogTime)
        {
            float now = Time.time;
            if (now - lastLogTime < _logCooldownSeconds)
                return false;

            lastLogTime = now;
            return true;
        }
    }
}
