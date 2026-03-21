#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// TerrainData asset management, GameObject creation, and material/collider configuration.
    /// Texture operations (heightmap, splatmap, normal map) are in TerrainTextureBuilder.
    /// </summary>
    internal static class TerrainBuilder
    {
        internal static TerrainData LoadOrCreateTerrainData(
            string terrainDataAsset, int heightmapRes, float terrainWidth,
            float terrainHeight, float terrainLength, int alphamapRes,
            int detailRes, int baseMapRes)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataAsset);
            if (existing != null)
            {
                existing.heightmapResolution = heightmapRes;
                existing.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
                existing.alphamapResolution = alphamapRes;
                existing.SetDetailResolution(detailRes, 16);
                existing.baseMapResolution = baseMapRes;
                return existing;
            }

            var data = new TerrainData();
            data.heightmapResolution = heightmapRes;
            data.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
            data.alphamapResolution = alphamapRes;
            data.SetDetailResolution(detailRes, 16);
            data.baseMapResolution = baseMapRes;
            AssetDatabase.CreateAsset(data, terrainDataAsset);
            return data;
        }

        /// <summary>Delegates to TerrainTextureBuilder.ImportHeightmap.</summary>
        internal static void ImportHeightmap(TerrainData terrainData, string dataPath, int heightmapRes)
            => TerrainTextureBuilder.ImportHeightmap(terrainData, dataPath, heightmapRes);

        /// <summary>Delegates to TerrainTextureBuilder.ApplyEdgeMaskSplatmap.</summary>
        internal static void ApplyEdgeMaskSplatmap(TerrainData terrainData, string texturePath)
            => TerrainTextureBuilder.ApplyEdgeMaskSplatmap(terrainData, texturePath);

        internal static GameObject CreateTerrainGameObject(
            TerrainData terrainData, float terrainWidth, float terrainLength)
        {
            var existing = GameObject.Find("OutpostTerrain");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = "OutpostTerrain";
            terrainGO.isStatic = true;
            // Center at origin
            terrainGO.transform.position = new Vector3(
                -terrainWidth * 0.5f,
                0f,
                -terrainLength * 0.5f);

            return terrainGO;
        }

        internal static void ConfigureTerrain(
            GameObject terrainGO, string terrainMaterialPath, string texturePath)
        {
            var terrain = terrainGO.GetComponent<Terrain>();
            // Persistent material asset required — in-memory Material has no GUID and
            // serializes as null after domain reload, leaving terrain invisible.
            Shader terrainShader = Shader.Find("Nature/Terrain/Standard")
                ?? Shader.Find("Nature/Terrain/Diffuse");
            if (terrainShader == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[OutpostTrack] Nature/Terrain/Standard shader not found — terrain may be invisible.");
            }
            else
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(terrainMaterialPath);
                if (mat != null)
                {
                    mat.shader = terrainShader;
                    EditorUtility.SetDirty(mat);
                }
                else
                {
                    mat = new Material(terrainShader);
                    AssetHelper.SaveOrReplaceAsset(mat, terrainMaterialPath);
                }

                terrain.materialTemplate = mat;
                EditorUtility.SetDirty(terrain);
            }

            terrain.heightmapPixelError = 5f;
            terrain.basemapDistance = 1000f;
            terrain.drawInstanced = true;

            TerrainTextureBuilder.ApplyMacroNormalMap(terrain, texturePath);

            // Ensure terrain collider is set up for physics
            var collider = terrainGO.GetComponent<TerrainCollider>();
            if (collider != null)
                collider.terrainData = terrain.terrainData;
        }
    }
}
#endif
