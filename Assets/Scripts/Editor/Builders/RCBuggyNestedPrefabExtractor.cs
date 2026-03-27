#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Low-level extraction helpers for <see cref="RCBuggyModularBuilder"/>.
    /// Saves named child GameObjects as nested prefab assets, then replaces the
    /// original in-hierarchy children with prefab instances so all GUID references
    /// are preserved.
    /// </summary>
    internal static class RCBuggyNestedPrefabExtractor
    {
        static readonly string[] k_WheelNames = { "WheelFL", "WheelFR", "WheelRL", "WheelRR" };

        static readonly string[] k_WheelPaths =
        {
            RCBuggyModularBuilder.k_WheelFLPath,
            RCBuggyModularBuilder.k_WheelFRPath,
            RCBuggyModularBuilder.k_WheelRLPath,
            RCBuggyModularBuilder.k_WheelRRPath
        };

        static readonly string[] k_BodyChildNames =
        {
            "ChassisPlate", "FrontBumperMesh", "RearBumperMesh",
            "FrontShockTower", "RearShockTower", "BodyShell", "RearWing",
            "FrontArmL", "FrontArmR", "RearArmL", "RearArmR"
        };

        // ---- Public API ----

        internal static void ExtractWheels(GameObject root)
        {
            for (int i = 0; i < k_WheelNames.Length; i++)
                ExtractChildAsNestedPrefab(root, k_WheelNames[i], k_WheelPaths[i]);
        }

        internal static void ExtractBody(GameObject root)
        {
            string destPath = RCBuggyModularBuilder.k_BodyPrefabPath;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destPath) != null)
            {
                Debug.Log("[RCBuggyNestedPrefabExtractor] " + destPath + " exists — skipping.");
                return;
            }

            var bodyGroup = new GameObject("RCBody");
            bodyGroup.transform.SetParent(root.transform, false);

            foreach (string childName in k_BodyChildNames)
            {
                Transform child = root.transform.Find(childName);
                if (child != null) child.SetParent(bodyGroup.transform, worldPositionStays: true);
            }

            var nestedPrefab = PrefabUtility.SaveAsPrefabAsset(bodyGroup, destPath);
            if (nestedPrefab == null)
            {
                Debug.LogError("[RCBuggyNestedPrefabExtractor] Failed to save body prefab.");
                foreach (Transform t in bodyGroup.transform)
                    t.SetParent(root.transform, worldPositionStays: true);
                Object.DestroyImmediate(bodyGroup);
                return;
            }

            Object.DestroyImmediate(bodyGroup);
            var inst = PrefabUtility.InstantiatePrefab(nestedPrefab, root.transform) as GameObject;
            if (inst != null) inst.name = "RCBody";
        }

        // ---- Private helpers ----

        static void ExtractChildAsNestedPrefab(GameObject root, string childName, string destPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destPath) != null)
            {
                Debug.Log("[RCBuggyNestedPrefabExtractor] " + destPath + " exists — skipping.");
                return;
            }

            Transform child = root.transform.Find(childName);
            if (child == null)
            {
                Debug.LogWarning("[RCBuggyNestedPrefabExtractor] '" + childName + "' not found.");
                return;
            }

            Vector3 pos    = child.localPosition;
            Quaternion rot = child.localRotation;
            Vector3 scl    = child.localScale;
            int sibIdx     = child.GetSiblingIndex();

            var nestedPrefab = PrefabUtility.SaveAsPrefabAsset(child.gameObject, destPath);
            if (nestedPrefab == null)
            {
                Debug.LogError("[RCBuggyNestedPrefabExtractor] Failed to save: " + destPath);
                return;
            }

            Object.DestroyImmediate(child.gameObject);

            var inst = PrefabUtility.InstantiatePrefab(nestedPrefab, root.transform) as GameObject;
            if (inst == null) return;
            inst.name                  = childName;
            inst.transform.localPosition = pos;
            inst.transform.localRotation = rot;
            inst.transform.localScale    = scl;
            inst.transform.SetSiblingIndex(sibIdx);
        }
    }
}
#endif
