#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using R8EOX.Editor;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Black-box tests for OutpostTrackSetup editor script.
    /// Verifies terrain creation, idempotency, and desert environment configuration.
    ///
    /// These tests prove the script works on first run AND on re-runs (idempotency).
    /// Each test calls BuildOutpostTrackInternal() in isolation via TearDown cleanup.
    /// </summary>
    public class OutpostTrackSetupTests
    {
        [TearDown]
        public void TearDown()
        {
            // Remove the terrain GO created during the test so tests don't interfere
            var terrain = Object.FindObjectOfType<Terrain>();
            if (terrain != null)
                Object.DestroyImmediate(terrain.gameObject);
        }

        [Test]
        public void BuildOutpostTrack_CreatesTerrainGameObject()
        {
            OutpostTrackSetup.BuildOutpostTrackInternal();

            var terrain = Object.FindObjectOfType<Terrain>();
            Assert.IsNotNull(terrain, "Terrain component should exist after build");
            Assert.AreEqual("OutpostTerrain", terrain.gameObject.name);
        }

        [Test]
        public void BuildOutpostTrack_TerrainDataNotNull()
        {
            OutpostTrackSetup.BuildOutpostTrackInternal();

            var terrain = Object.FindObjectOfType<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist");
            Assert.IsNotNull(terrain.terrainData, "TerrainData must not be null");
        }

        [Test]
        public void BuildOutpostTrack_TerrainHasTwoLayers()
        {
            OutpostTrackSetup.BuildOutpostTrackInternal();

            var terrain = Object.FindObjectOfType<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist");
            Assert.AreEqual(2, terrain.terrainData.terrainLayers.Length,
                "Should have exactly 2 layers (DirtBase + DirtTop)");
        }

        [Test]
        public void BuildOutpostTrack_IsIdempotent()
        {
            // Two runs must produce a single valid terrain — no errors, no duplicates
            LogAssert.NoUnexpectedReceived();
            OutpostTrackSetup.BuildOutpostTrackInternal();
            OutpostTrackSetup.BuildOutpostTrackInternal();

            var terrains = Object.FindObjectsOfType<Terrain>();
            Assert.AreEqual(1, terrains.Length, "Should have exactly one terrain after two builds");
            Assert.IsNotNull(terrains[0].terrainData, "TerrainData must not be null after second build");
        }

        [Test]
        public void BuildOutpostTrack_TerrainHasMaterialAssigned()
        {
            OutpostTrackSetup.BuildOutpostTrackInternal();

            var terrain = Object.FindObjectOfType<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist");
            Assert.IsNotNull(terrain.materialTemplate,
                "materialTemplate must not be null — terrain would be invisible in Built-in RP");
        }

        [Test]
        public void BuildOutpostTrack_ConfiguresDesertFog()
        {
            OutpostTrackSetup.BuildOutpostTrackInternal();

            Assert.IsTrue(RenderSettings.fog, "Fog should be enabled");
            Assert.AreEqual(FogMode.Exponential, RenderSettings.fogMode, "Should use exponential fog");
        }

        [Test]
        public void BuildOutpostTrack_ConfiguresAmbientTrilight()
        {
            OutpostTrackSetup.BuildOutpostTrackInternal();

            Assert.AreEqual(UnityEngine.Rendering.AmbientMode.Trilight,
                RenderSettings.ambientMode, "Should use trilight ambient mode");
        }

        [Test]
        public void BuildOutpostTrack_CreatesConfigAssetWithDefaultDimensions()
        {
            const string configPath = "Assets/Terrain/Outpost/Data/OutpostTerrainConfig.asset";

            // Delete any pre-existing config to force auto-creation
            if (AssetDatabase.LoadAssetAtPath<OutpostTerrainConfig>(configPath) != null)
                AssetDatabase.DeleteAsset(configPath);

            OutpostTrackSetup.BuildOutpostTrackInternal();

            var cfg = AssetDatabase.LoadAssetAtPath<OutpostTerrainConfig>(configPath);
            Assert.IsNotNull(cfg, "OutpostTerrainConfig asset should be auto-created");
            Assert.AreEqual(100f, cfg.Width,   1e-4f, "Default width should be 100m");
            Assert.AreEqual(100f, cfg.Length,  1e-4f, "Default length should be 100m");
            Assert.AreEqual(2f,   cfg.MaxHeight, 1e-4f, "Default max height should be 2m");
            Assert.AreEqual(5f,   cfg.DirtTileSize, 1e-4f, "Default tile size should be 5m");
        }

        [Test]
        public void BuildOutpostTrack_TerrainSizeMatchesConfig()
        {
            OutpostTrackSetup.BuildOutpostTrackInternal();

            const string configPath = "Assets/Terrain/Outpost/Data/OutpostTerrainConfig.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<OutpostTerrainConfig>(configPath);
            Assert.IsNotNull(cfg, "Config asset must exist after build");

            var terrain = Object.FindObjectOfType<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist");
            Assert.AreEqual(cfg.Width,     terrain.terrainData.size.x, 1e-4f, "Terrain X matches config Width");
            Assert.AreEqual(cfg.MaxHeight, terrain.terrainData.size.y, 1e-4f, "Terrain Y matches config MaxHeight");
            Assert.AreEqual(cfg.Length,    terrain.terrainData.size.z, 1e-4f, "Terrain Z matches config Length");
        }
    }
}
#endif
