using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug.Diagnostics
{
    /// <summary>
    /// Debug-draw helpers for terrain diagnostics, extracted from
    /// <see cref="TerrainDiagnosticChecks"/>. All methods emit only Debug.DrawRay calls —
    /// no per-frame state and no log output.
    /// </summary>
    public static class TerrainDiagnosticDrawing
    {
        /// <summary>Draws contact normal ray: green = ok, yellow = steep, red = very steep.</summary>
        public static void DrawContactNormal(
            RaycastWheel wheel, float deviationThreshold, float steepThreshold)
        {
            float normalY = wheel.ContactNormal.y;
            Color color = normalY < steepThreshold    ? Color.red
                        : normalY < deviationThreshold ? Color.yellow
                        : Color.green;

            UnityEngine.Debug.DrawRay(wheel.ContactPoint, wheel.ContactNormal * 0.3f, color);
        }
    }
}
