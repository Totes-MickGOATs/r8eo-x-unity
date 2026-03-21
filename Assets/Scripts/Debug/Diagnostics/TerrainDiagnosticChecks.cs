using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug.Diagnostics
{
    /// <summary>
    /// Pure-static check methods extracted from WheelTerrainDiagnostics.
    /// All methods are side-effect-free apart from logging; no MonoBehaviour state.
    /// </summary>
    public static class TerrainDiagnosticChecks
    {
        // ---- Nested Types ----

        /// <summary>Per-wheel frame-over-frame tracking state.</summary>
        public struct WheelState
        {
            public float PrevSuspensionForce;
            public Vector3 PrevTireVelocity;
            public Vector3 PrevContactPoint;
            public bool PrevIsOnGround;
            public bool[] GroundHistory;     // ring buffer for flicker detection
            public int GroundHistoryIndex;
            public float LastNormalLogTime;
            public float LastForceLogTime;
            public float LastVelocityLogTime;
            public float LastContactLogTime;
            public float LastFlickerLogTime;
        }


        // ---- Per-Frame Checks ----

        /// <summary>Warns when contact normal Y falls below threshold — steep face or seam.</summary>
        public static void CheckNormalDeviation(
            RaycastWheel wheel, ref WheelState state, float threshold, float logCooldown)
        {
            float normalY = wheel.ContactNormal.y;
            if (normalY >= threshold) return;
            if (!CanLog(ref state.LastNormalLogTime, logCooldown)) return;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} hit steep normal ({wheel.ContactNormal}) " +
                $"at {wheel.ContactPoint} — possible terrain seam");
        }

        /// <summary>Warns on frame-over-frame suspension force spike. Skips the first landing frame.</summary>
        public static void CheckForceSpikeDetection(
            RaycastWheel wheel, ref WheelState state, float threshold, float logCooldown)
        {
            float delta = Mathf.Abs(wheel.SuspensionForce - state.PrevSuspensionForce);
            if (delta <= threshold) return;
            if (!state.PrevIsOnGround) return;   // first landing frame — damping handles it
            if (!CanLog(ref state.LastForceLogTime, logCooldown)) return;

            UnityEngine.Debug.LogWarning(
                $"[suspension] {wheel.name} force spike: {state.PrevSuspensionForce:F1}" +
                $"→{wheel.SuspensionForce:F1} (Δ{delta:F1}N) at {wheel.ContactPoint}");
        }

        /// <summary>Warns when tire velocity delta magnitude exceeds threshold m/s.</summary>
        public static void CheckVelocityDiscontinuity(
            RaycastWheel wheel, ref WheelState state, float threshold, float logCooldown)
        {
            if (!state.PrevIsOnGround) return;

            float prevSpeed = state.PrevTireVelocity.magnitude;
            float currentSpeed = wheel.TireVelocity.magnitude;
            float deltaMag = (wheel.TireVelocity - state.PrevTireVelocity).magnitude;

            if (deltaMag <= threshold) return;
            if (!CanLog(ref state.LastVelocityLogTime, logCooldown)) return;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} velocity discontinuity: {prevSpeed:F2}→{currentSpeed:F2} m/s " +
                $"(Δ{deltaMag:F2}) at {wheel.ContactPoint}");
        }

        /// <summary>Warns when the contact point moves more than threshold metres in one frame.</summary>
        public static void CheckContactPointJump(
            RaycastWheel wheel, ref WheelState state, float threshold, float logCooldown)
        {
            if (!state.PrevIsOnGround) return;

            float distance = Vector3.Distance(wheel.ContactPoint, state.PrevContactPoint);
            if (distance <= threshold) return;
            if (!CanLog(ref state.LastContactLogTime, logCooldown)) return;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} contact point jump: {distance:F3}m in one frame " +
                $"at {wheel.ContactPoint}");
        }

        /// <summary>Warns when ground-state toggles within a sliding window exceed countThreshold.</summary>
        public static void CheckGroundFlicker(
            RaycastWheel wheel, ref WheelState state,
            int windowFrames, int countThreshold, float logCooldown)
        {
            state.GroundHistory[state.GroundHistoryIndex] = wheel.IsOnGround;
            state.GroundHistoryIndex = (state.GroundHistoryIndex + 1) % windowFrames;

            int toggleCount = 0;
            for (int j = 1; j < windowFrames; j++)
            {
                int curr = (state.GroundHistoryIndex - 1 - j + windowFrames * 2) % windowFrames;
                int prev = (curr - 1 + windowFrames) % windowFrames;
                if (state.GroundHistory[curr] != state.GroundHistory[prev])
                    toggleCount++;
            }

            if (toggleCount < countThreshold) return;
            if (!CanLog(ref state.LastFlickerLogTime, logCooldown)) return;

            UnityEngine.Debug.LogWarning(
                $"[physics] {wheel.name} ground contact flickering — {toggleCount} toggles " +
                $"in {windowFrames} frames at {wheel.transform.position}");
        }


        // ---- Startup Checks (forwarded to TerrainStartupChecks) ----

        /// <summary>Warns if the Rigidbody is not using ContinuousSpeculative or ContinuousDynamic.</summary>
        public static void CheckCollisionDetectionMode(Rigidbody rb, string carName)
            => TerrainStartupChecks.CheckCollisionDetectionMode(rb, carName);

        /// <summary>Logs heightmap resolution; warns if it exceeds highResThreshold (micro-seam risk).</summary>
        public static void CheckTerrainCollider(int highResThreshold)
            => TerrainStartupChecks.CheckTerrainCollider(highResThreshold);

        // ---- Debug Drawing (forwarded to TerrainDiagnosticDrawing) ----

        /// <summary>Draws contact normal ray: green = ok, yellow = steep, red = very steep.</summary>
        public static void DrawContactNormal(
            RaycastWheel wheel, float deviationThreshold, float steepThreshold)
            => TerrainDiagnosticDrawing.DrawContactNormal(wheel, deviationThreshold, steepThreshold);

        // ---- Helpers ----

        /// <summary>Returns true (and updates lastLogTime) when cooldown has elapsed; false otherwise.</summary>
        public static bool CanLog(ref float lastLogTime, float cooldown)
        {
            float now = Time.time;
            if (now - lastLogTime < cooldown) return false;

            lastLogTime = now;
            return true;
        }
    }
}
