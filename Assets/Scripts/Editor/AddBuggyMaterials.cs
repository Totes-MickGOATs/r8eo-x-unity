using UnityEditor;
using UnityEngine;

namespace R8EOX.Editor
{
    /// <summary>Menu item to assign persistent materials to RCBuggy visual meshes.</summary>
    public static class AddBuggyMaterials
    {
        const string k_PrefabPath   = "Assets/Prefabs/RCBuggy.prefab";
        const string k_MaterialsDir = "Assets/Materials/Vehicle";

        [MenuItem("Tools/RC Buggy/Apply Vehicle Materials")]
        public static void Apply()
        {
            System.IO.Directory.CreateDirectory(
                System.IO.Path.Combine(Application.dataPath, "..", k_MaterialsDir));
            AssetDatabase.Refresh();

            // ── Create / load material assets ────────────────────────────────
            var darkGrey  = GetOrCreateMat("DarkGrey",   new Color(0.20f, 0.20f, 0.20f));
            var medGrey   = GetOrCreateMat("MediumGrey", new Color(0.50f, 0.50f, 0.50f));
            var blueSolid = GetOrCreateMat("BlueSolid",  new Color(0.18f, 0.45f, 0.90f));
            var blueSemi  = GetOrCreateMat("BlueSemi",   new Color(0.18f, 0.45f, 0.90f, 0.85f),
                                           transparent: true);
            var blackTire = GetOrCreateMat("BlackTire",  new Color(0.05f, 0.05f, 0.05f));
            var whiteHub  = GetOrCreateMat("WhiteHub",   new Color(0.90f, 0.90f, 0.90f));

            AssetDatabase.SaveAssets();

            // ── Open prefab for editing ───────────────────────────────────────
            var root = PrefabUtility.LoadPrefabContents(k_PrefabPath);

            // Body parts
            Assign(root, "ChassisPlate",    darkGrey);
            Assign(root, "FrontBumperMesh", darkGrey);
            Assign(root, "RearBumperMesh",  darkGrey);
            Assign(root, "FrontShockTower", medGrey);
            Assign(root, "RearShockTower",  medGrey);
            Assign(root, "BodyShell",       blueSemi);
            Assign(root, "RearWing",        blueSolid);
            Assign(root, "FrontArmL",       darkGrey);
            Assign(root, "FrontArmR",       darkGrey);
            Assign(root, "RearArmL",        darkGrey);
            Assign(root, "RearArmR",        darkGrey);

            // Wheel visuals (WheelVisual and HubVisual are grandchildren under WheelFL/FR/RL/RR)
            foreach (var wheelName in new[] { "WheelFL", "WheelFR", "WheelRL", "WheelRR" })
            {
                var wheel = root.transform.Find(wheelName);
                if (wheel == null) continue;
                AssignChild(wheel, "WheelVisual", blackTire);
                AssignChild(wheel, "HubVisual",   whiteHub);
            }

            PrefabUtility.SaveAsPrefabAsset(root, k_PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[AddBuggyMaterials] Materials applied to RCBuggy prefab.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        static Material GetOrCreateMat(string matName, Color color, bool transparent = false)
        {
            string path     = $"{k_MaterialsDir}/{matName}.mat";
            var    existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var mat = new Material(Shader.Find("Standard")) { name = matName };
            mat.color = color;

            if (transparent)
            {
                mat.SetFloat("_Mode", 3);   // Transparent
                mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void Assign(GameObject root, string childName, Material mat)
        {
            var child = root.transform.Find(childName);
            if (child == null)
            {
                UnityEngine.Debug.LogWarning($"[AddBuggyMaterials] Child not found: {childName}");
                return;
            }
            var r = child.GetComponent<Renderer>();
            if (r == null)
            {
                UnityEngine.Debug.LogWarning($"[AddBuggyMaterials] No Renderer on: {childName}");
                return;
            }
            r.sharedMaterial = mat;
        }

        static void AssignChild(Transform parent, string childName, Material mat)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                UnityEngine.Debug.LogWarning($"[AddBuggyMaterials] Child not found under {parent.name}: {childName}");
                return;
            }
            var r = child.GetComponent<Renderer>();
            if (r == null)
            {
                UnityEngine.Debug.LogWarning($"[AddBuggyMaterials] No Renderer on: {childName}");
                return;
            }
            r.sharedMaterial = mat;
        }
    }
}
