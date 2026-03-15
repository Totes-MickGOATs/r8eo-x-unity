#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
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
    }
}
#endif
