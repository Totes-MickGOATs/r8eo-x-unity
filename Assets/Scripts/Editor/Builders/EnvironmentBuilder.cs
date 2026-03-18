#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace R8EOX.Editor.Builders
{
    /// <summary>
    /// Configures the desert scene environment: skybox, fog, ambient lighting, and sun.
    /// </summary>
    internal static class EnvironmentBuilder
    {
        internal static void SetupDesertEnvironment(
            string skyboxHdriPath, string skyboxMaterialPath,
            float fogDensity, float sunIntensity)
        {
            // ---- Skybox ----
            // Import HDRI as 2D texture (equirectangular panorama, not cubemap)
            TextureImporter hdriImporter = AssetImporter.GetAtPath(skyboxHdriPath) as TextureImporter;
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

            Texture2D hdriTex = AssetDatabase.LoadAssetAtPath<Texture2D>(skyboxHdriPath);
            if (hdriTex == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[OutpostTrack] Desert HDRI not found at " + skyboxHdriPath +
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
                AssetHelper.SaveOrReplaceAsset(skyboxMat, skyboxMaterialPath);
                RenderSettings.skybox = skyboxMat;
                UnityEngine.Debug.Log("[OutpostTrack] Desert HDRI panoramic skybox applied.");
            }

            // ---- Fog ----
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.85f, 0.75f, 0.6f);
            RenderSettings.fogDensity = fogDensity;
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
                sun.intensity = sunIntensity;
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
