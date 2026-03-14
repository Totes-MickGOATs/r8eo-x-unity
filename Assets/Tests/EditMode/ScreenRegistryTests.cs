using NUnit.Framework;
using R8EOX.UI;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for ScreenRegistry — screen lookup by ID.
    /// </summary>
    [TestFixture]
    public sealed class ScreenRegistryTests
    {
        private ScreenRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<ScreenRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_registry);
        }

        [Test]
        public void TryGetScreen_ExistingId_ReturnsTrueAndPrefab()
        {
            var prefab = new GameObject("TestPrefab");
            _registry.AddEntry(new ScreenRegistryEntry("main_menu", prefab));

            bool found = _registry.TryGetScreen("main_menu", out var result);

            Assert.That(found, Is.True);
            Assert.That(result, Is.EqualTo(prefab));

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void TryGetScreen_UnknownId_ReturnsFalse()
        {
            bool found = _registry.TryGetScreen("nonexistent", out var result);

            Assert.That(found, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryGetScreen_NullPrefab_ReturnsFalse()
        {
            _registry.AddEntry(new ScreenRegistryEntry("loading", null));

            bool found = _registry.TryGetScreen("loading", out var result);

            Assert.That(found, Is.False);
        }

        [Test]
        public void AllEntries_ReturnsAllRegistered()
        {
            var prefab1 = new GameObject("Prefab1");
            var prefab2 = new GameObject("Prefab2");
            _registry.AddEntry(new ScreenRegistryEntry("screen_a", prefab1));
            _registry.AddEntry(new ScreenRegistryEntry("screen_b", prefab2));

            Assert.That(_registry.AllEntries.Count, Is.EqualTo(2));

            Object.DestroyImmediate(prefab1);
            Object.DestroyImmediate(prefab2);
        }
    }
}
