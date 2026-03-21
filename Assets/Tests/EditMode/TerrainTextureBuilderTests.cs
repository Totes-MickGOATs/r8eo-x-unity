#if UNITY_EDITOR
using NUnit.Framework;
using R8EOX.Editor.Builders;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for TerrainTextureBuilder — the texture/splatmap/normalmap helpers
    /// extracted from TerrainBuilder.
    /// Idempotency: calling Apply* twice on the same TerrainData must not throw.
    /// </summary>
    public class TerrainTextureBuilderTests
    {
        private TerrainData _terrainData;

        [SetUp]
        public void SetUp()
        {
            _terrainData = new TerrainData();
            _terrainData.heightmapResolution = 33;
            _terrainData.size = new Vector3(100f, 10f, 100f);
            _terrainData.alphamapResolution = 16;
        }

        [TearDown]
        public void TearDown()
        {
            if (_terrainData != null)
                Object.DestroyImmediate(_terrainData);
        }

        [Test]
        public void ApplyEdgeMaskSplatmap_MissingFile_DoesNotThrow()
        {
            // When the mask file does not exist the method should log a warning
            // and return gracefully — not throw.
            Assert.DoesNotThrow(() =>
                TerrainTextureBuilder.ApplyEdgeMaskSplatmap(_terrainData, "Assets/NonExistent/Textures"));
        }

        [Test]
        public void ApplyEdgeMaskSplatmap_CalledTwice_IsIdempotent()
        {
            // Calling twice should not throw or leave the TerrainData in a broken state.
            Assert.DoesNotThrow(() =>
            {
                TerrainTextureBuilder.ApplyEdgeMaskSplatmap(_terrainData, "Assets/NonExistent/Textures");
                TerrainTextureBuilder.ApplyEdgeMaskSplatmap(_terrainData, "Assets/NonExistent/Textures");
            });
        }

        [Test]
        public void ApplyMacroNormalMap_MissingFile_DoesNotThrow()
        {
            // When the normal map file does not exist the method should log a warning
            // and return gracefully — not throw.
            var terrain = new GameObject("TestTerrain").AddComponent<Terrain>();
            terrain.terrainData = _terrainData;
            Assert.DoesNotThrow(() =>
                TerrainTextureBuilder.ApplyMacroNormalMap(terrain, "Assets/NonExistent/Textures"));
            Object.DestroyImmediate(terrain.gameObject);
        }

        [Test]
        public void ApplyMacroNormalMap_CalledTwice_IsIdempotent()
        {
            var terrain = new GameObject("TestTerrain2").AddComponent<Terrain>();
            terrain.terrainData = _terrainData;
            Assert.DoesNotThrow(() =>
            {
                TerrainTextureBuilder.ApplyMacroNormalMap(terrain, "Assets/NonExistent/Textures");
                TerrainTextureBuilder.ApplyMacroNormalMap(terrain, "Assets/NonExistent/Textures");
            });
            Object.DestroyImmediate(terrain.gameObject);
        }

        [Test]
        public void ImportHeightmap_MissingFile_DoesNotThrow()
        {
            // When the heightmap .raw file does not exist the method should log an error
            // and return gracefully — not throw.
            Assert.DoesNotThrow(() =>
                TerrainTextureBuilder.ImportHeightmap(_terrainData, "Assets/NonExistent", 33));
        }
    }
}
#endif
