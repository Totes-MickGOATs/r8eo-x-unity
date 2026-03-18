#if UNITY_EDITOR
using UnityEngine;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Configures Unity physics and project time settings for the RC simulation.
    /// </summary>
    internal static class PhysicsConfigurator
    {
        // ---- Constants ----

        const float k_FixedTimestep = 0.008333f; // 120 Hz
        const int k_SolverIterations = 8;
        const int k_SolverVelocityIterations = 4;

        // ---- Public API ----

        /// <summary>
        /// Sets fixed timestep, gravity, and solver iteration counts.
        /// </summary>
        internal static void ConfigurePhysics()
        {
            Time.fixedDeltaTime = k_FixedTimestep;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Physics.defaultSolverIterations = k_SolverIterations;
            Physics.defaultSolverVelocityIterations = k_SolverVelocityIterations;

            UnityEngine.Debug.Log("[PhysicsConfigurator] Physics configured: 120Hz, gravity=-9.81, " +
                                  $"solver={k_SolverIterations}/{k_SolverVelocityIterations}");
        }

        /// <summary>
        /// Configures ambient lighting and render settings for the test scene.
        /// </summary>
        internal static void ConfigureProjectSettings()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.25f, 0.2f);
        }
    }
}
#endif
