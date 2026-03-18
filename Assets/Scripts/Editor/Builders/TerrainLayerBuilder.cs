#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Configures Unity TerrainLayer assets with PBR textures (diffuse, normal, ARM).
    /// Called by TerrainBuilder.ConfigureTerrainLayers.
    /// </summary>
    internal static class TerrainLayerBuilder
    {
        internal static void ConfigureTerrainLayers(
            TerrainData terrainData, string dataPath, string texturePath, float dirtTileSize)
        {
            // Layer 0: Base soil (dark compacted surface, visible everywhere)
            // Layer 1: Top soil (lighter gravel, blended in via edge mask)
            var layers = new TerrainLayer[2];
            layers[0] = LoadOrConfigureTerrainLayer("DirtBase", "DirtBase", dirtTileSize, dataPath, texturePath);
            layers[1] = LoadOrConfigureTerrainLayer("DirtTop",  "DirtTop",  dirtTileSize, dataPath, texturePath);
            terrainData.terrainLayers = layers;
            UnityEngine.Debug.Log("[OutpostTrack] Terrain layers configured (Poly Haven PBR textures).");
        }

        internal static TerrainLayer LoadOrConfigureTerrainLayer(
            string layerName, string textureFolder, float tileSize, string dataPath, string texturePath)
        {
            string layerPath = $"{dataPath}/TerrainLayer_{layerName}.asset";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            bool isNew = layer == null;
            if (isNew) layer = new TerrainLayer();

            layer.name = layerName;
            layer.tileSize = new Vector2(tileSize, tileSize);
            layer.tileOffset = Vector2.zero;

            string folderPath = $"{texturePath}/{textureFolder}";

            // Diffuse
            Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folderPath}/diffuse.jpg");
            if (diffuse != null)
                layer.diffuseTexture = diffuse;
            else
                UnityEngine.Debug.LogWarning($"[OutpostTrack] Missing diffuse: {folderPath}/diffuse.jpg");

            // Normal map — ensure TextureImporter type is NormalMap
            string normalPath = $"{folderPath}/normal.png";
            TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal != null)
                layer.normalMapTexture = normal;
            else
                UnityEngine.Debug.LogWarning($"[OutpostTrack] Missing normal: {normalPath}");

            // ARM map (AO=R, Roughness=G, Metallic=B) — ensure linear import
            string armPath = $"{folderPath}/arm.jpg";
            TextureImporter armImporter = AssetImporter.GetAtPath(armPath) as TextureImporter;
            if (armImporter != null && armImporter.sRGBTexture)
            {
                armImporter.sRGBTexture = false;
                armImporter.SaveAndReimport();
            }
            Texture2D arm = AssetDatabase.LoadAssetAtPath<Texture2D>(armPath);
            if (arm != null)
                layer.maskMapTexture = arm;

            layer.metallic = 0f;
            layer.smoothness = 0.3f;
            layer.normalScale = 1.0f;

            if (isNew)
                AssetDatabase.CreateAsset(layer, layerPath);
            else
                EditorUtility.SetDirty(layer);

            return layer;
        }
    }
}
#endif
