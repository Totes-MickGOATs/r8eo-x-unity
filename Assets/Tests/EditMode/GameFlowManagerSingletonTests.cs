using System;
using NUnit.Framework;
using R8EOX.GameFlow;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for GameFlowManager singleton, transitions, and session management.
    /// Navigation (breadcrumb) tests live in GameFlowManagerNavigationTests.cs.
    /// </summary>
    [TestFixture]
    public sealed class GameFlowManagerSingletonTests
    {
        private GameObject _managerGo;
        private GameFlowManager _manager;

        [SetUp]
        public void SetUp()
        {
            if (GameFlowManager.Instance != null)
                UnityEngine.Object.DestroyImmediate(GameFlowManager.Instance.gameObject);

            _managerGo = new GameObject("TestManager");
            _manager = _managerGo.AddComponent<GameFlowManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_managerGo != null)
                UnityEngine.Object.DestroyImmediate(_managerGo);

            if (GameFlowManager.Instance != null)
                UnityEngine.Object.DestroyImmediate(GameFlowManager.Instance.gameObject);
        }

        [Test]
        public void Awake_SetsInstance()
        {
            Assert.That(GameFlowManager.Instance, Is.EqualTo(_manager));
        }

        [Test]
        public void Awake_DuplicateDestroyed()
        {
            var duplicateGo = new GameObject("DuplicateManager");
            duplicateGo.AddComponent<GameFlowManager>();

            Assert.That(GameFlowManager.Instance, Is.EqualTo(_manager));

            UnityEngine.Object.DestroyImmediate(duplicateGo);
        }

        [Test]
        public void RequestTransition_ValidTransition_ChangesState()
        {
            _manager.RequestTransition(GameState.Splash);
            Assert.That(_manager.CurrentState, Is.EqualTo(GameState.Splash));
        }

        [Test]
        public void RequestTransition_InvalidTransition_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _manager.RequestTransition(GameState.Playing));
        }

        [Test]
        public void SetSession_StoresConfig()
        {
            var session = new SessionConfig("race", "track1", "Scenes/Track1", "buggy", 3, 1);
            _manager.SetSession(session);
            Assert.That(_manager.CurrentSession, Is.EqualTo(session));
        }

        [Test]
        public void SetSession_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _manager.SetSession(null));
        }

        [Test]
        public void ReturnToMenu_ClearsSession()
        {
            var session = new SessionConfig("race", "track1", "Scenes/Track1", "buggy", 3, 1);
            _manager.SetSession(session);
            _manager.BootDirectToPlaying();
            _manager.ReturnToMenu();
            Assert.That(_manager.CurrentSession, Is.Null);
        }

        [Test]
        public void BootDirectToPlaying_WalksStatesToPlaying()
        {
            _manager.BootDirectToPlaying();
            Assert.That(_manager.CurrentState, Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void OnStateChanged_FiresOnTransition()
        {
            GameState capturedPrev = default;
            GameState capturedNext = default;
            _manager.OnStateChanged += (prev, next) =>
            {
                capturedPrev = prev;
                capturedNext = next;
            };

            _manager.RequestTransition(GameState.Splash);

            Assert.That(capturedPrev, Is.EqualTo(GameState.Boot));
            Assert.That(capturedNext, Is.EqualTo(GameState.Splash));
        }
    }
}
