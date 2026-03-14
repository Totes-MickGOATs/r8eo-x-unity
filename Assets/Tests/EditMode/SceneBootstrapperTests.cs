using NUnit.Framework;
using R8EOX.GameFlow;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for SceneBootstrapper — standalone mode detection and manager creation.
    /// </summary>
    [TestFixture]
    public sealed class SceneBootstrapperTests
    {
        private GameObject _bootstrapperGo;

        [SetUp]
        public void SetUp()
        {
            // Ensure no leftover instance
            if (GameFlowManager.Instance != null)
            {
                Object.DestroyImmediate(GameFlowManager.Instance.gameObject);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_bootstrapperGo != null)
            {
                Object.DestroyImmediate(_bootstrapperGo);
            }

            if (GameFlowManager.Instance != null)
            {
                Object.DestroyImmediate(GameFlowManager.Instance.gameObject);
            }
        }

        [Test]
        public void Awake_NoManagerExists_CreatesManager()
        {
            Assert.That(GameFlowManager.Instance, Is.Null);

            _bootstrapperGo = new GameObject("Bootstrapper");
            _bootstrapperGo.AddComponent<SceneBootstrapper>();

            Assert.That(GameFlowManager.Instance, Is.Not.Null);
        }

        [Test]
        public void Awake_ManagerExists_DoesNotCreateAnother()
        {
            // Pre-create a manager
            var existingGo = new GameObject("ExistingManager");
            var existingManager = existingGo.AddComponent<GameFlowManager>();

            _bootstrapperGo = new GameObject("Bootstrapper");
            var bootstrapper = _bootstrapperGo.AddComponent<SceneBootstrapper>();

            // Should still be the original manager
            Assert.That(GameFlowManager.Instance, Is.EqualTo(existingManager));

            Object.DestroyImmediate(existingGo);
        }

        [Test]
        public void Awake_StandaloneMode_SetsDefaultSession()
        {
            _bootstrapperGo = new GameObject("Bootstrapper");
            _bootstrapperGo.AddComponent<SceneBootstrapper>();

            Assert.That(GameFlowManager.Instance.CurrentSession, Is.Not.Null);
            Assert.That(GameFlowManager.Instance.CurrentSession.ModeId, Is.EqualTo("testing"));
        }

        [Test]
        public void Awake_StandaloneMode_BootsToPlaying()
        {
            _bootstrapperGo = new GameObject("Bootstrapper");
            _bootstrapperGo.AddComponent<SceneBootstrapper>();

            Assert.That(GameFlowManager.Instance.CurrentState, Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void IsStandaloneMode_True_WhenCreatedManager()
        {
            _bootstrapperGo = new GameObject("Bootstrapper");
            var bootstrapper = _bootstrapperGo.AddComponent<SceneBootstrapper>();

            Assert.That(bootstrapper.IsStandaloneMode, Is.True);
        }

        [Test]
        public void IsStandaloneMode_False_WhenManagerPreexisted()
        {
            // Pre-create a manager
            var existingGo = new GameObject("ExistingManager");
            existingGo.AddComponent<GameFlowManager>();

            _bootstrapperGo = new GameObject("Bootstrapper");
            var bootstrapper = _bootstrapperGo.AddComponent<SceneBootstrapper>();

            Assert.That(bootstrapper.IsStandaloneMode, Is.False);

            Object.DestroyImmediate(existingGo);
        }
    }
}
