#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.IO;

[assembly: InternalsVisibleTo("R8EOX.Tests.EditMode")]

namespace R8EOX.Editor
{
    /// <summary>
    /// Editor tool to build the Outpost test track terrain.
    /// Imports heightmap, configures terrain layers with edge mask blending,
    /// and applies desert environment (skybox, fog, ambient).
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

        const string k_SkyboxPath = k_TexturePath + "/Skybox";
        const string k_SkyboxHdriPath = k_SkyboxPath + "/goegap_2k.hdr";
        const string k_SkyboxMaterialPath = k_DataPath + "/DesertSkybox.mat";

        // Desert fog settings
        const float k_FogDensity = 0.005f;
        // Desert directional light
        const float k_SunIntensity = 1.2f;


        // ---- Menu Item ----

        [MenuItem("RC Buggy/Build Outpost Track")]
        static void BuildOutpostTrack() => BuildOutpostTrackInternal();

        // Internal entry point — accessible to EditMode tests via InternalsVisibleTo
        internal static void BuildOutpostTrackInternal()
        {
            UnityEngine.Debug.Log("[OutpostTrack] Building Outpost track...");

            // Load existing TerrainData or create fresh — NEVER delete-and-recreate.
            // TerrainData must be a persisted asset before the Terrain GO is created,
            // otherwise the Terrain component's reference becomes invalid after asset refresh.
            TerrainData terrainData = LoadOrCreateTerrainData();
            ImportHeightmap(terrainData);
            ConfigureTerrainLayers(terrainData);
            ApplyEdgeMaskSplatmap(terrainData);
            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets(); // Persist before creating GO

            GameObject terrainGO = CreateTerrainGameObject(terrainData);
            ConfigureTerrain(terrainGO);

            SetupDesertEnvironment();

            UnityEngine.Debug.Log("[OutpostTrack] Outpost track built successfully!");
            UnityEngine.Debug.Log($"  Terrain: {k_TerrainWidth}x{k_TerrainLength}m, height={k_TerrainHeight}m");
            UnityEngine.Debug.Log($"  Heightmap: {k_HeightmapRes}x{k_HeightmapRes}");
            Selection.activeGameObject = terrainGO;
        }


        // ---- Helpers ----

        /// <summary>
        /// Deletes the existing asset at assetPath before creating a new one.
        /// Only safe for assets that no component references by GUID (e.g. generated
        /// materials). Never use for TerrainData or TerrainLayers — use LoadOrCreate.
        /// </summary>
        static void SaveOrReplaceAsset(UnityEngine.Object obj, string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(obj, assetPath);
        }


        // ---- Build Steps ----

        static TerrainData LoadOrCreateTerrainData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TerrainData>(k_TerrainDataAsset);
            if (existing != null)
            {
                // Update size/resolution constants in case they changed
                existing.heightmapResolution = k_HeightmapRes;
                existing.size = new Vector3(k_TerrainWidth, k_TerrainHeight, k_TerrainLength);
                existing.alphamapResolution = k_AlphamapRes;
                existing.SetDetailResolution(k_DetailRes, 16);
                existing.baseMapResolution = k_BaseMapRes;
                return existing;
            }

            var data = new TerrainData();
            data.heightmapResolution = k_HeightmapRes;
            data.size = new Vector3(k_TerrainWidth, k_TerrainHeight, k_TerrainLength);
            data.alphamapResolution = k_AlphamapRes;
            data.SetDetailResolution(k_DetailRes, 16);
            data.baseMapResolution = k_BaseMapRes;
            AssetDatabase.CreateAsset(data, k_TerrainDataAsset);
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
            // Layer 0: Base soil (dark compacted surface, visible everywhere)
            // Layer 1: Top soil (lighter gravel, blended in via edge mask)
            var layers = new TerrainLayer[2];
            layers[0] = LoadOrConfigureTerrainLayer("DirtBase", "DirtBase", k_DirtTileSize);
            layers[1] = LoadOrConfigureTerrainLayer("DirtTop", "DirtTop", k_DirtTileSize);
            terrainData.terrainLayers = layers;
            UnityEngine.Debug.Log("[OutpostTrack] Terrain layers configured (Poly Haven PBR textures).");
        }

        static TerrainLayer LoadOrConfigureTerrainLayer(
            string layerName, string textureFolder, float tileSize)
        {
            string layerPath = $"{k_DataPath}/TerrainLayer_{layerName}.asset";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            bool isNew = layer == null;
            if (isNew) layer = new TerrainLayer();

            layer.name = layerName;
            layer.tileSize = new Vector2(tileSize, tileSize);
            layer.tileOffset = Vector2.zero;

            string folderPath = $"{k_TexturePath}/{textureFolder}";

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

            if (isNew)
                AssetDatabase.CreateAsset(layer, layerPath);
            else
                EditorUtility.SetDirty(layer);

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

            // Assign Nature/Terrain/Standard shader material so terrain is visible in Built-in RP.
            // materialType=3 (Custom) + materialTemplate=null makes terrain invisible in Unity 2022.3.
            Shader terrainShader = Shader.Find("Nature/Terrain/Standard")
                ?? Shader.Find("Nature/Terrain/Diffuse");
            if (terrainShader != null)
                terrain.materialTemplate = new Material(terrainShader);
            else
                UnityEngine.Debug.LogWarning(
                    "[OutpostTrack] Nature/Terrain/Standard shader not found — terrain may be invisible.");

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

        static void SetupDesertEnvironment()
        {
            // ---- Skybox ----
            // Import HDRI as 2D texture (equirectangular panorama, not cubemap)
            TextureImporter hdriImporter = AssetImporter.GetAtPath(k_SkyboxHdriPath) as TextureImporter;
            if (hdriImporter != null)
            {
                bool needsReimport = hdriImporter.textureShape != TextureImporterShape.Texture2D
                    || hdriImporter.sRGBTexture;
                if (needsReimport)
                {
                    hdriImporter.textureShape = TextureImporterShape.Texture2D;
                    hdriImporter.sRGBTexture = false;
                    hdriImporter.SaveAndReimport();
                }
            }

            Texture2D hdriTex = AssetDatabase.LoadAssetAtPath<Texture2D>(k_SkyboxHdriPath);
            if (hdriTex == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[OutpostTrack] Desert HDRI not found at " + k_SkyboxHdriPath +
                    " — skipping skybox setup.");
            }
            else
            {
                // Skybox/Panoramic is correct for equirectangular (lat-long) HDRIs
                Material skyboxMat = new Material(Shader.Find("Skybox/Panoramic"));
                skyboxMat.SetTexture("_MainTex", hdriTex);
                skyboxMat.SetFloat("_Exposure", 1.0f);
                skyboxMat.SetFloat("_Mapping", 1f);    // 1 = Latitude Longitude Layout
                skyboxMat.SetFloat("_ImageType", 0f);  // 0 = 360 degrees
                SaveOrReplaceAsset(skyboxMat, k_SkyboxMaterialPath);
                RenderSettings.skybox = skyboxMat;
                UnityEngine.Debug.Log("[OutpostTrack] Desert HDRI panoramic skybox applied.");
            }

            // ---- Fog ----
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.85f, 0.75f, 0.6f);
            RenderSettings.fogDensity = k_FogDensity;
            UnityEngine.Debug.Log("[OutpostTrack] Desert fog configured (exponential, density=0.005).");

            // ---- Ambient Lighting ----
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.85f, 0.75f, 0.55f); // Warm tan/orange sky
            RenderSettings.ambientEquatorColor = new Color(0.70f, 0.60f, 0.45f); // Sandy equator
            RenderSettings.ambientGroundColor  = new Color(0.35f, 0.28f, 0.18f); // Dark sand ground
            UnityEngine.Debug.Log("[OutpostTrack] Desert ambient trilight configured.");

            // ---- Directional Light ----
            Light sun = GameObject.FindObjectOfType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.color = new Color(1.0f, 0.92f, 0.70f);
                sun.intensity = k_SunIntensity;
                UnityEngine.Debug.Log("[OutpostTrack] Desert sun color applied to directional light.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[OutpostTrack] No directional light found in scene.");
            }
        }


    }
}
#endif
