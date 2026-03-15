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

            int attached = 0;
            GameObject carGO = car.gameObject;

            if (carGO.GetComponent<ContractDebugger>() == null)
            {
                carGO.AddComponent<ContractDebugger>();
                attached++;
            }

            if (carGO.GetComponent<WheelTerrainDiagnostics>() == null)
            {
                carGO.AddComponent<WheelTerrainDiagnostics>();
                attached++;
            }

            if (carGO.GetComponent<InputDiagnostics>() == null)
            {
                carGO.AddComponent<InputDiagnostics>();
                attached++;
            }

            UnityEngine.Debug.Log($"[DebugBootstrap] Attached {attached} debug components to {car.name}");
        }
#endif
    }
}
