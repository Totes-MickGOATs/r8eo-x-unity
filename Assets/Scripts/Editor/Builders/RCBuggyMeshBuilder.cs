#if UNITY_EDITOR
using UnityEngine;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Mesh, visual, and material helpers for the RCBuggy prefab.
    /// Extracted from RCBuggyBuilder to keep each file under 150 lines.
    /// All methods are pure/static — calling them twice appends duplicate meshes,
    /// so callers are responsible for ensuring single invocation per build.
    /// </summary>
    internal static class RCBuggyMeshBuilder
    {
        internal static void AddBodyMeshes(GameObject root, Material darkGrey, Material medGrey,
                                           Material blueSemi, Material blueSolid)
        {
            AddBoxMesh(root, "ChassisPlate",    new Vector3(1.3f, 0.08f, 3.4f),  new Vector3(0f, -0.5825f,  0f),    darkGrey);
            AddBoxMesh(root, "FrontBumperMesh", new Vector3(1.2f, 0.3f,  0.25f), new Vector3(0f, -0.3825f,  1.95f), darkGrey);
            AddBoxMesh(root, "RearBumperMesh",  new Vector3(1.0f, 0.4f,  0.4f),  new Vector3(0f, -0.3825f, -1.8f),  darkGrey);
            AddBoxMesh(root, "FrontShockTower", new Vector3(1.0f, 0.6f,  0.05f), new Vector3(0f, -0.1825f,  1.2f),  medGrey);
            AddBoxMesh(root, "RearShockTower",  new Vector3(0.8f, 0.6f,  0.05f), new Vector3(0f, -0.1825f, -1.2f),  medGrey);
            AddBoxMesh(root, "BodyShell",       new Vector3(1.2f, 0.4f,  2.8f),  new Vector3(0f, -0.125f,   0.2f),  blueSemi);

            GameObject wing = AddBoxMesh(root, "RearWing", new Vector3(1.2f, 0.02f, 0.4f),
                                         new Vector3(0f, 0.4175f, -1.5f), blueSolid);
            wing.transform.localRotation = Quaternion.Euler(-22.5f, 0f, 0f);
        }

        internal static void AddControlArms(GameObject root, Material darkGrey)
        {
            AddBoxMesh(root, "FrontArmL", new Vector3(0.65f, 0.05f, 0.2f), new Vector3(-0.65f, -0.5325f,  1.7f), darkGrey);
            AddBoxMesh(root, "FrontArmR", new Vector3(0.65f, 0.05f, 0.2f), new Vector3( 0.65f, -0.5325f,  1.7f), darkGrey);
            AddBoxMesh(root, "RearArmL",  new Vector3(0.65f, 0.05f, 0.2f), new Vector3(-0.65f, -0.5325f, -1.7f), darkGrey);
            AddBoxMesh(root, "RearArmR",  new Vector3(0.65f, 0.05f, 0.2f), new Vector3( 0.65f, -0.5325f, -1.7f), darkGrey);
        }

        internal static void BuildWheelVisual(GameObject pivot, string name,
            float radius, float height, Material mat, int layer)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(pivot.transform, false);
            go.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.layer = layer;
        }

        internal static GameObject AddBoxMesh(GameObject parent, string name,
            Vector3 size, Vector3 localPos, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        internal static Material CreateMaterial(string name, Color color, bool transparent = false)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.name = name;
            mat.color = color;
            if (transparent)
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            return mat;
        }
    }
}
#endif
