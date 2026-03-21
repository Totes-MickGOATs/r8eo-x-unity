#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Heightmap import, splatmap application, and normal-map setup for OutpostTerrain.
    /// Extracted from TerrainBuilder to keep each file under 150 lines.
    /// </summary>
    internal static class TerrainTextureBuilder
    {
        internal static void ImportHeightmap(TerrainData terrainData, string dataPath, int heightmapRes)
        {
            string rawPath = Path.Combine(
                Application.dataPath.Replace("Assets", ""),
                dataPath, "heightmap.raw");

            if (!File.Exists(rawPath))
            {
                UnityEngine.Debug.LogError($"[OutpostTrack] Heightmap not found: {rawPath}");
                return;
            }

            byte[] rawBytes = File.ReadAllBytes(rawPath);
            int expectedSize = heightmapRes * heightmapRes * 2;
            if (rawBytes.Length != expectedSize)
            {
                UnityEngine.Debug.LogError(
                    $"[OutpostTrack] Heightmap size mismatch: {rawBytes.Length} vs {expectedSize}");
                return;
            }

            // 16-bit unsigned LE → float[,] 0-1
            float[,] heights = new float[heightmapRes, heightmapRes];
            for (int y = 0; y < heightmapRes; y++)
            {
                for (int x = 0; x < heightmapRes; x++)
                {
                    int idx = (y * heightmapRes + x) * 2;
                    ushort raw = (ushort)(rawBytes[idx] | (rawBytes[idx + 1] << 8));
                    heights[y, x] = raw / 65535f;
                }
            }

            terrainData.SetHeights(0, 0, heights);
            UnityEngine.Debug.Log("[OutpostTrack] Heightmap imported successfully.");
        }

        internal static void ApplyEdgeMaskSplatmap(TerrainData terrainData, string texturePath)
        {
            string maskPath = $"{texturePath}/edge-mask.png";
            string fullMaskPath = Path.Combine(
                Application.dataPath.Replace("Assets", ""), maskPath);

            if (!File.Exists(fullMaskPath))
            {
                UnityEngine.Debug.LogWarning(
                    "[OutpostTrack] Edge mask not found, using uniform 50/50 blend.");
                return;
            }

            AssetDatabase.ImportAsset(maskPath);
            Texture2D maskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);

            if (maskTex == null)
            {
                maskTex = new Texture2D(2, 2);
                maskTex.LoadImage(File.ReadAllBytes(fullMaskPath));
            }

            int alphaRes = terrainData.alphamapResolution;
            float[,,] splatmap = new float[alphaRes, alphaRes, 2];

            for (int y = 0; y < alphaRes; y++)
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    float u = (float)x / (alphaRes - 1);
                    float v = 1f - (float)y / (alphaRes - 1); // flip V: file row 0 → terrain y=0
                    Color pixel = maskTex.GetPixelBilinear(u, v);

                    float maskValue = pixel.grayscale;
                    splatmap[y, x, 0] = 1f - maskValue; // Base dirt
                    splatmap[y, x, 1] = maskValue;       // Top dirt (blended in by mask)
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmap);
            UnityEngine.Debug.Log("[OutpostTrack] Edge mask splatmap applied.");
        }

        internal static void ApplyMacroNormalMap(Terrain terrain, string texturePath)
        {
            string normalPath = $"{texturePath}/normal-map.png";
            AssetDatabase.ImportAsset(normalPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            if (normalTex != null)
            {
                TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                    normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                }
                UnityEngine.Debug.Log("[OutpostTrack] Macro normal map applied.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[OutpostTrack] Normal map not found at " + normalPath);
            }
        }
    }
}
#endif
