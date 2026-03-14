using NUnit.Framework;
using R8EOX.GameFlow;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    [TestFixture]
    public sealed class SceneRegistryTests
    {
        private SceneRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<SceneRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_registry);
        }

        [Test]
        public void TryGetScene_ExistingId_ReturnsTrueAndEntry()
        {
            _registry.AddScene(new SceneEntry("outpost", "Outpost Track", "Assets/Scenes/OutpostTrack.unity"));

            bool found = _registry.TryGetScene("outpost", out SceneEntry entry);

            Assert.IsTrue(found);
            Assert.AreEqual("outpost", entry.Id);
            Assert.AreEqual("Outpost Track", entry.DisplayName);
            Assert.AreEqual("Assets/Scenes/OutpostTrack.unity", entry.ScenePath);
        }

        [Test]
        public void TryGetScene_UnknownId_ReturnsFalse()
        {
            _registry.AddScene(new SceneEntry("outpost", "Outpost Track", "Assets/Scenes/OutpostTrack.unity"));

            bool found = _registry.TryGetScene("nonexistent", out SceneEntry entry);

            Assert.IsFalse(found);
            Assert.IsNull(entry);
        }

        [Test]
        public void AllScenes_ReturnsAllEntries()
        {
            _registry.AddScene(new SceneEntry("outpost", "Outpost Track", "Assets/Scenes/OutpostTrack.unity"));
            _registry.AddScene(new SceneEntry("desert", "Desert Circuit", "Assets/Scenes/DesertCircuit.unity"));

            var allScenes = _registry.AllScenes;

            Assert.AreEqual(2, allScenes.Count);
            Assert.AreEqual("outpost", allScenes[0].Id);
            Assert.AreEqual("desert", allScenes[1].Id);
        }

        [Test]
        public void AllScenes_EmptyRegistry_ReturnsEmptyList()
        {
            var allScenes = _registry.AllScenes;

            Assert.AreEqual(0, allScenes.Count);
        }
    }
}
