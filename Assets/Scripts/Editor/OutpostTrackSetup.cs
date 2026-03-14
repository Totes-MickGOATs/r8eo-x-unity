#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace R8EOX.Editor
{
    /// <summary>
    /// Editor tool to build the Outpost test track terrain.
    /// Imports heightmap, configures terrain layers with edge mask blending,
    /// and applies macro normal and specular maps.
    /// Use: Menu -> RC Buggy -> Build Outpost Track
    /// </summary>
    public static class OutpostTrackSetup
    {
        // ---- Constants ----

        const string k_TerrainAssetPath = "Assets/Terrain/Outpost";
        const string k_TexturePath = k_TerrainAssetPath + "/Textures";
        const string k_DataPath = k_TerrainAssetPath + "/Data";
        const string k_TerrainDataAsset = k_DataPath + "/OutpostTerrainData.asset";

        const int k_HeightmapRes = 2049; // 2^11 + 1
        const float k_TerrainWidth = 100f;  // metres
        const float k_TerrainLength = 100f; // metres
        const float k_TerrainHeight = 10f;  // metres (height scale 0.1 applied via heightmapScale)
        const int k_DetailRes = 1024;
        const int k_AlphamapRes = 2048; // Resolution for splatmap blending
        const int k_BaseMapRes = 1024;

        const float k_DirtTileSize = 5f; // Repeating dirt texture tile size in metres


        // ---- Menu Item ----

        [MenuItem("RC Buggy/Build Outpost Track")]
        static void BuildOutpostTrack()
        {
            UnityEngine.Debug.Log("[OutpostTrack] Building Outpost track...");

            TerrainData terrainData = CreateTerrainData();
            ImportHeightmap(terrainData);
            ConfigureTerrainLayers(terrainData);
            ApplyEdgeMaskSplatmap(terrainData);

            GameObject terrainGO = CreateTerrainGameObject(terrainData);
            ConfigureTerrain(terrainGO);

            // Save the TerrainData as an asset
            AssetDatabase.CreateAsset(terrainData, k_TerrainDataAsset);
            AssetDatabase.SaveAssets();

            UnityEngine.Debug.Log("[OutpostTrack] Outpost track built successfully!");
            UnityEngine.Debug.Log($"  Terrain: {k_TerrainWidth}x{k_TerrainLength}m, height={k_TerrainHeight}m");
            UnityEngine.Debug.Log($"  Heightmap: {k_HeightmapRes}x{k_HeightmapRes}");
            Selection.activeGameObject = terrainGO;
        }


        // ---- Build Steps ----

        static TerrainData CreateTerrainData()
        {
            var data = new TerrainData();
            data.heightmapResolution = k_HeightmapRes;
            data.size = new Vector3(k_TerrainWidth, k_TerrainHeight, k_TerrainLength);
            data.alphamapResolution = k_AlphamapRes;
            data.SetDetailResolution(k_DetailRes, 16);
            data.baseMapResolution = k_BaseMapRes;
            return data;
        }

        static void ImportHeightmap(TerrainData terrainData)
        {
            string rawPath = Path.Combine(
                Application.dataPath.Replace("Assets", ""),
                k_DataPath, "heightmap.raw");

            if (!File.Exists(rawPath))
            {
                UnityEngine.Debug.LogError($"[OutpostTrack] Heightmap not found: {rawPath}");
                return;
            }

            byte[] rawBytes = File.ReadAllBytes(rawPath);
            int expectedSize = k_HeightmapRes * k_HeightmapRes * 2; // 16-bit
            if (rawBytes.Length != expectedSize)
            {
                UnityEngine.Debug.LogError(
                    $"[OutpostTrack] Heightmap size mismatch: {rawBytes.Length} vs expected {expectedSize}");
                return;
            }

            // Convert 16-bit unsigned LE to Unity's float[,] (0-1 range)
            float[,] heights = new float[k_HeightmapRes, k_HeightmapRes];
            for (int y = 0; y < k_HeightmapRes; y++)
            {
                for (int x = 0; x < k_HeightmapRes; x++)
                {
                    int idx = (y * k_HeightmapRes + x) * 2;
                    ushort raw = (ushort)(rawBytes[idx] | (rawBytes[idx + 1] << 8));
                    heights[y, x] = raw / 65535f;
                }
            }

            terrainData.SetHeights(0, 0, heights);
            UnityEngine.Debug.Log("[OutpostTrack] Heightmap imported successfully.");
        }

        static void ConfigureTerrainLayers(TerrainData terrainData)
        {
            // Create two dirt terrain layers (repeating textures)
            // Layer 0: Base dirt (bottom layer — visible everywhere by default)
            // Layer 1: Top dirt (blended in via edge mask)
            var layers = new TerrainLayer[2];

            layers[0] = CreateDirtLayer(
                "DirtBase",
                new Color(0.45f, 0.35f, 0.25f), // Warm brown
                k_DirtTileSize);

            layers[1] = CreateDirtLayer(
                "DirtTop",
                new Color(0.55f, 0.42f, 0.30f), // Lighter sandy brown
                k_DirtTileSize);

            // Save terrain layers as assets
            for (int i = 0; i < layers.Length; i++)
            {
                string layerPath = $"{k_DataPath}/TerrainLayer_{layers[i].name}.asset";
                AssetDatabase.CreateAsset(layers[i], layerPath);
            }

            terrainData.terrainLayers = layers;
            UnityEngine.Debug.Log("[OutpostTrack] Terrain layers configured (2 dirt layers).");
        }

        static TerrainLayer CreateDirtLayer(string name, Color baseColor, float tileSize)
        {
            var layer = new TerrainLayer();
            layer.name = name;
            layer.tileSize = new Vector2(tileSize, tileSize);
            layer.tileOffset = Vector2.zero;

            // Create a simple procedural dirt texture (will be replaced with real textures later)
            Texture2D diffuse = CreateProceduralDirtTexture(name + "_Diffuse", baseColor, 512);
            string texPath = $"{k_TexturePath}/{name}_Diffuse.asset";
            AssetDatabase.CreateAsset(diffuse, texPath);
            layer.diffuseTexture = diffuse;

            // Create a flat normal map for now (will be replaced with real textures later)
            Texture2D normal = CreateFlatNormalTexture(name + "_Normal", 512);
            string normalPath = $"{k_TexturePath}/{name}_Normal.asset";
            AssetDatabase.CreateAsset(normal, normalPath);
            layer.normalMapTexture = normal;

            layer.metallic = 0f;
            layer.smoothness = 0.2f;

            return layer;
        }

        static void ApplyEdgeMaskSplatmap(TerrainData terrainData)
        {
            // Load the edge mask (grayscale PNG extracted from TGA alpha)
            string maskPath = $"{k_TexturePath}/edge-mask.png";
            string fullMaskPath = Path.Combine(
                Application.dataPath.Replace("Assets", ""), maskPath);

            if (!File.Exists(fullMaskPath))
            {
                UnityEngine.Debug.LogWarning(
                    "[OutpostTrack] Edge mask not found, using uniform 50/50 blend.");
                return;
            }

            // Import as texture to read pixels
            AssetDatabase.ImportAsset(maskPath);
            Texture2D maskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);

            if (maskTex == null)
            {
                // Load directly from file as fallback
                byte[] maskBytes = File.ReadAllBytes(fullMaskPath);
                maskTex = new Texture2D(2, 2);
                maskTex.LoadImage(maskBytes);
            }

            int alphaRes = terrainData.alphamapResolution;
            float[,,] splatmap = new float[alphaRes, alphaRes, 2];

            for (int y = 0; y < alphaRes; y++)
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    // Sample the mask texture at this splatmap position
                    float u = (float)x / (alphaRes - 1);
                    float v = (float)y / (alphaRes - 1);
                    Color pixel = maskTex.GetPixelBilinear(u, v);

                    // Mask value: 0 = base dirt only, 1 = top dirt fully visible
                    float maskValue = pixel.grayscale;

                    splatmap[y, x, 0] = 1f - maskValue; // Base dirt
                    splatmap[y, x, 1] = maskValue;       // Top dirt (blended in by mask)
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmap);
            UnityEngine.Debug.Log("[OutpostTrack] Edge mask splatmap applied.");
        }

        static GameObject CreateTerrainGameObject(TerrainData terrainData)
        {
            // Remove existing terrain if present
            var existing = GameObject.Find("OutpostTerrain");
            if (existing != null)
                Object.DestroyImmediate(existing);

            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = "OutpostTerrain";
            terrainGO.isStatic = true;

            // Position so terrain is centered at origin
            terrainGO.transform.position = new Vector3(
                -k_TerrainWidth * 0.5f,
                0f,
                -k_TerrainLength * 0.5f);

            return terrainGO;
        }

        static void ConfigureTerrain(GameObject terrainGO)
        {
            var terrain = terrainGO.GetComponent<Terrain>();
            terrain.materialTemplate = null; // Use default terrain material
            terrain.heightmapPixelError = 5f;
            terrain.basemapDistance = 1000f;
            terrain.drawInstanced = true;

            // Apply macro normal map if available
            ApplyMacroNormalMap(terrain);

            // Ensure terrain collider is set up for physics
            var collider = terrainGO.GetComponent<TerrainCollider>();
            if (collider != null)
                collider.terrainData = terrain.terrainData;
        }

        static void ApplyMacroNormalMap(Terrain terrain)
        {
            string normalPath = $"{k_TexturePath}/normal-map.png";
            AssetDatabase.ImportAsset(normalPath);
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            if (normalTex != null)
            {
                // Set the texture import settings for normal map
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


        // ---- Texture Generation Helpers ----

        static Texture2D CreateProceduralDirtTexture(string name, Color baseColor, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            // Simple noise-based dirt texture
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.15f;
                    float detail = Mathf.PerlinNoise(x * 0.2f + 100f, y * 0.2f + 100f) * 0.08f;
                    float variation = noise + detail - 0.1f;

                    Color pixel = new Color(
                        Mathf.Clamp01(baseColor.r + variation),
                        Mathf.Clamp01(baseColor.g + variation),
                        Mathf.Clamp01(baseColor.b + variation),
                        1f);
                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            return tex;
        }

        static Texture2D CreateFlatNormalTexture(string name, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color flatNormal = new Color(0.5f, 0.5f, 1f, 1f); // Flat normal (0,0,1)
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = flatNormal;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
#endif
