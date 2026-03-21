using UnityEngine;

namespace R8EOX.Debug.Diagnostics
{
    /// <summary>
    /// Startup-time terrain and physics configuration checks extracted from
    /// <see cref="TerrainDiagnosticChecks"/>. Runs once during scene load —
    /// no per-frame state required.
    /// </summary>
    public static class TerrainStartupChecks
    {
        /// <summary>Warns if the Rigidbody is not using ContinuousSpeculative or ContinuousDynamic.</summary>
        public static void CheckCollisionDetectionMode(Rigidbody rb, string carName)
        {
            if (rb == null) return;

            if (rb.collisionDetectionMode != CollisionDetectionMode.ContinuousSpeculative
                && rb.collisionDetectionMode != CollisionDetectionMode.ContinuousDynamic)
            {
                UnityEngine.Debug.LogWarning(
                    $"[physics] {carName} Rigidbody uses {rb.collisionDetectionMode} collision detection " +
                    $"— consider ContinuousSpeculative or ContinuousDynamic for terrain snag prevention");
            }
        }

        /// <summary>Logs heightmap resolution; warns if it exceeds highResThreshold (micro-seam risk).</summary>
        public static void CheckTerrainCollider(int highResThreshold)
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
            UnityEngine.Debug.Log($"[physics] Terrain heightmap resolution: {resolution}x{resolution}");

            if (resolution > highResThreshold)
            {
                UnityEngine.Debug.Log(
                    $"[physics] High-res heightmap ({resolution}) may cause micro-seams — " +
                    "consider a simplified collider mesh for RC-scale vehicles");
            }
        }
    }
}
