using UnityEngine;
using R8EOX.Vehicle;

namespace R8EOX.Debug
{
    /// <summary>
    /// Auto-attaches debug components to the RCCar GameObject at runtime.
    /// Uses <see cref="RuntimeInitializeOnLoadMethodAttribute"/> so no manual scene setup is required.
    /// Stripped from release builds via <c>#if UNITY_EDITOR || DEBUG</c>.
    /// </summary>
    public static class DebugBootstrap
    {
#if UNITY_EDITOR || DEBUG
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AttachDebugComponents()
        {
            var car = Object.FindAnyObjectByType<RCCar>();

            if (car == null)
            {
                UnityEngine.Debug.Log("[DebugBootstrap] No RCCar found in scene — skipping debug attachment");
                return;
            }

            AttachTo(car.gameObject);
        }

        /// <summary>
        /// Attaches all debug diagnostic components to the given <paramref name="target"/> GameObject.
        /// Existing components are never duplicated — idempotent by design.
        /// </summary>
        /// <param name="target">The GameObject to attach debug components to.</param>
        public static void AttachTo(GameObject target)
        {
            if (target == null) return;

            int attached = 0;

            if (target.GetComponent<ContractDebugger>() == null)
            {
                target.AddComponent<ContractDebugger>();
                attached++;
            }

            if (target.GetComponent<WheelTerrainDiagnostics>() == null)
            {
                target.AddComponent<WheelTerrainDiagnostics>();
                attached++;
            }

            if (target.GetComponent<InputDiagnostics>() == null)
            {
                target.AddComponent<InputDiagnostics>();
                attached++;
            }

            UnityEngine.Debug.Log($"[DebugBootstrap] Attached {attached} debug components to {target.name}");
        }
#endif
    }
}
