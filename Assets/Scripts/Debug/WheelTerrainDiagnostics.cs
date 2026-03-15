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
        const int k_FrameBufferSize = 20;


        // ---- Serialized Fields ----

        [Header("Target")]
        [Tooltip("The RC car to monitor. Auto-detected from this or parent if unset.")]
        [SerializeField] private RCCar _car;

        [Header("Normal Deviation")]
        [Tooltip("Contact normal Y below this triggers a steep-normal warning (1.0 = flat, 0.0 = vertical)")]
        [SerializeField] private float _normalDeviationThreshold = 0.85f;

        [Header("Suspension Force")]
        [Tooltip("Frame-over-frame suspension force delta (in Newtons) that triggers a spike warning")]
        [SerializeField] private float _forceSpikeThreshold = 15f;

        [Header("Velocity")]
        [Tooltip("Tire velocity magnitude change (m/s per frame) that triggers a discontinuity warning")]
        [SerializeField] private float _velocityDiscontinuityThreshold = 0.8f;

        [Header("Contact Point")]
        [Tooltip("Contact point jump distance (metres per frame) that triggers a warning while grounded")]
        [SerializeField] private float _contactJumpThreshold = 0.02f;

        [Header("Lateral Force")]
        [Tooltip("Lateral force magnitude (Newtons) that triggers a spike warning")]
        [SerializeField] private float _lateralForceSpikeThreshold = 15f;

        [Header("Total Force")]
        [Tooltip("Total combined force magnitude (Newtons) that triggers a spike warning")]
        [SerializeField] private float _totalForceSpikeThreshold = 80f;

        [Header("Ground Flicker")]
        [Tooltip("Number of physics frames in the sliding window for flicker detection")]
        [SerializeField] private int _flickerWindowFrames = 10;

        [Tooltip("Number of ground-state toggles within the window that triggers a flicker warning")]
        [SerializeField] private int _flickerCountThreshold = 3;

        [Header("Frame Buffer")]
        [Tooltip("Enable per-wheel circular buffer dump when any anomaly triggers")]
        [SerializeField] private bool _enableFrameDump = true;

        [Header("Log Throttle")]
        [Tooltip("Minimum seconds between logs of the same type per wheel to prevent spam")]
        [SerializeField] private float _logCooldownSeconds = 0.5f;

        [Header("Scene Debug Drawing")]
        [Tooltip("Draw contact normals in Scene view (green=normal, yellow=steep, red=very steep)")]
        [SerializeField] private bool _drawContactNormals = true;


        // ---- Nested Types ----

        /// <summary>Per-frame snapshot for the circular buffer.</summary>
        private struct FrameSnapshot
        {
            public Vector3 ContactPoint;
            public Vector3 ContactNormal;
            public float SuspensionForce;
            public float LateralForce;
            public float TotalForce;
            public Vector3 TireVelocity;
            public string HitColliderName;
            public bool IsOnGround;
        }

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

            // Frame buffer for dump-on-anomaly
            public FrameSnapshot[] FrameBuffer;
            public int FrameBufferIndex;
            public int FrameBufferCount;

            // Per-detection-type cooldown timestamps
            public float LastNormalLogTime;
            public float LastForceLogTime;
            public float LastVelocityLogTime;
            public float LastContactLogTime;
            public float LastFlickerLogTime;
            public float LastColliderLogTime;
            public float LastLateralLogTime;
            public float LastTotalForceLogTime;
            public float LastFrameDumpLogTime;
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
                bool anomalyTriggered = false;
                string anomalyReason = null;

                if (wheel.IsOnGround)
                {
                    // Collider identification — #1 most important check
                    if (CheckColliderIdentification(wheel, ref state))
                    {
                        anomalyTriggered = true;
                        anomalyReason = "non-terrain collider";
                    }

                    if (CheckNormalDeviation(wheel, ref state))
                    {
                        anomalyTriggered = true;
                        anomalyReason ??= "steep normal";
                    }

                    if (CheckForceSpikeDetection(wheel, ref state))
                    {
                        anomalyTriggered = true;
                        anomalyReason ??= "suspension force spike";
                    }

                    if (CheckVelocityDiscontinuity(wheel, ref state))
                    {
                        anomalyTriggered = true;
                        anomalyReason ??= "velocity discontinuity";
                    }

                    if (CheckContactPointJump(wheel, ref state))
                    {
                        anomalyTriggered = true;
                        anomalyReason ??= "contact point jump";
                    }

                    if (CheckLateralForceSpike(wheel, ref state))
                    {
                        anomalyTriggered = true;
                        anomalyReason ??= "lateral force spike";
                    }

                    if (CheckTotalForceSpike(wheel, ref state))
                    {
                        anomalyTriggered = true;
                        anomalyReason ??= "total force spike";
                    }

                    if (_drawContactNormals)
                        DrawContactNormal(wheel);
                }

                if (CheckGroundFlicker(wheel, ref state))
                {
                    anomalyTriggered = true;
                    anomalyReason ??= "ground flicker";
                }

                // Record frame to circular buffer
                RecordFrame(wheel, ref state);

                // Dump buffer on anomaly
                if (anomalyTriggered && _enableFrameDump)
                    DumpFrameBuffer(wheel, ref state, anomalyReason);

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
                    GroundHistoryIndex = 0,
                    FrameBuffer = new FrameSnapshot[k_FrameBufferSize],
                    FrameBufferIndex = 0,
                    FrameBufferCount = 0
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

        /// <summary>
        /// Checks if the wheel hit a non-terrain collider. Returns true if anomaly detected.
        /// </summary>
        private bool CheckColliderIdentification(RaycastWheel wheel, ref WheelState state)
        {
            if (string.IsNullOrEmpty(wheel.HitColliderName)) return false;
            if (wheel.HitColliderIsTerrain) return false;

            if (!CanLog(ref state.LastColliderLogTime)) return true; // anomaly but throttled

            string layerName = LayerMask.LayerToName(wheel.HitColliderLayer);
            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} hit non-terrain collider: \"{wheel.HitColliderName}\" " +
                $"(layer: {layerName}) at {wheel.ContactPoint}");

            return true;
        }

        private bool CheckNormalDeviation(RaycastWheel wheel, ref WheelState state)
        {
            float normalY = wheel.ContactNormal.y;
            if (normalY >= _normalDeviationThreshold) return false;

            if (!CanLog(ref state.LastNormalLogTime)) return true;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} hit steep normal ({wheel.ContactNormal}) " +
                $"at {wheel.ContactPoint} — possible terrain seam");

            return true;
        }

        private bool CheckForceSpikeDetection(RaycastWheel wheel, ref WheelState state)
        {
            float delta = Mathf.Abs(wheel.SuspensionForce - state.PrevSuspensionForce);
            if (delta <= _forceSpikeThreshold) return false;

            // Skip the first frame after landing — damping sanitisation handles that
            if (!state.PrevIsOnGround) return false;

            if (!CanLog(ref state.LastForceLogTime)) return true;

            UnityEngine.Debug.LogWarning(
                $"[suspension] {wheel.name} force spike: {state.PrevSuspensionForce:F1}" +
                $"→{wheel.SuspensionForce:F1} (Δ{delta:F1}N) at {wheel.ContactPoint}");

            return true;
        }

        private bool CheckVelocityDiscontinuity(RaycastWheel wheel, ref WheelState state)
        {
            if (!state.PrevIsOnGround) return false;

            float prevSpeed = state.PrevTireVelocity.magnitude;
            float currentSpeed = wheel.TireVelocity.magnitude;
            float deltaMag = (wheel.TireVelocity - state.PrevTireVelocity).magnitude;

            if (deltaMag <= _velocityDiscontinuityThreshold) return false;
            if (!CanLog(ref state.LastVelocityLogTime)) return true;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} velocity discontinuity: {prevSpeed:F2}→{currentSpeed:F2} m/s " +
                $"(Δ{deltaMag:F2}) at {wheel.ContactPoint}");

            return true;
        }

        private bool CheckContactPointJump(RaycastWheel wheel, ref WheelState state)
        {
            if (!state.PrevIsOnGround) return false;

            float distance = Vector3.Distance(wheel.ContactPoint, state.PrevContactPoint);
            if (distance <= _contactJumpThreshold) return false;
            if (!CanLog(ref state.LastContactLogTime)) return true;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} contact point jump: {distance:F3}m in one frame " +
                $"at {wheel.ContactPoint}");

            return true;
        }

        /// <summary>
        /// Checks if lateral force exceeds threshold. Returns true if anomaly detected.
        /// </summary>
        private bool CheckLateralForceSpike(RaycastWheel wheel, ref WheelState state)
        {
            float magnitude = wheel.LateralForceMagnitude;
            if (magnitude <= _lateralForceSpikeThreshold) return false;

            if (!CanLog(ref state.LastLateralLogTime)) return true;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} lateral force spike: {magnitude:F1}N at {wheel.ContactPoint}");

            return true;
        }

        /// <summary>
        /// Checks if total combined force exceeds threshold. Returns true if anomaly detected.
        /// </summary>
        private bool CheckTotalForceSpike(RaycastWheel wheel, ref WheelState state)
        {
            float magnitude = wheel.TotalForceMagnitude;
            if (magnitude <= _totalForceSpikeThreshold) return false;

            if (!CanLog(ref state.LastTotalForceLogTime)) return true;

            float suspForce = wheel.SuspensionForce;
            float latForce = wheel.LateralForceMagnitude;
            float longForce = magnitude; // approximation for log

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} total force spike: {magnitude:F1}N " +
                $"(susp={suspForce:F1} lat={latForce:F1}) at {wheel.ContactPoint}");

            return true;
        }

        private bool CheckGroundFlicker(RaycastWheel wheel, ref WheelState state)
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

            if (toggleCount < _flickerCountThreshold) return false;
            if (!CanLog(ref state.LastFlickerLogTime)) return true;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} ground contact flickering — {toggleCount} toggles " +
                $"in {_flickerWindowFrames} frames at {wheel.transform.position}");

            return true;
        }


        // ---- Frame Buffer ----

        private void RecordFrame(RaycastWheel wheel, ref WheelState state)
        {
            state.FrameBuffer[state.FrameBufferIndex] = new FrameSnapshot
            {
                ContactPoint = wheel.ContactPoint,
                ContactNormal = wheel.ContactNormal,
                SuspensionForce = wheel.SuspensionForce,
                LateralForce = wheel.LateralForceMagnitude,
                TotalForce = wheel.TotalForceMagnitude,
                TireVelocity = wheel.TireVelocity,
                HitColliderName = wheel.HitColliderName ?? "",
                IsOnGround = wheel.IsOnGround
            };
            state.FrameBufferIndex = (state.FrameBufferIndex + 1) % k_FrameBufferSize;
            if (state.FrameBufferCount < k_FrameBufferSize)
                state.FrameBufferCount++;
        }

        private void DumpFrameBuffer(RaycastWheel wheel, ref WheelState state, string reason)
        {
            if (!CanLog(ref state.LastFrameDumpLogTime)) return;
            if (state.FrameBufferCount == 0) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[physics] {wheel.name} FRAME DUMP (trigger: {reason}):");

            int start = (state.FrameBufferIndex - state.FrameBufferCount + k_FrameBufferSize)
                        % k_FrameBufferSize;

            for (int i = 0; i < state.FrameBufferCount; i++)
            {
                int idx = (start + i) % k_FrameBufferSize;
                var snap = state.FrameBuffer[idx];
                int frameOffset = i - state.FrameBufferCount;
                sb.AppendLine(
                    $"  frame {frameOffset}: pos={snap.ContactPoint} n={snap.ContactNormal} " +
                    $"susp={snap.SuspensionForce:F1} lat={snap.LateralForce:F1} " +
                    $"total={snap.TotalForce:F1} vel={snap.TireVelocity} " +
                    $"collider=\"{snap.HitColliderName}\" grounded={snap.IsOnGround}");
            }

            UnityEngine.Debug.Log(sb.ToString());
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
