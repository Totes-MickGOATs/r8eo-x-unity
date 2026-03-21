using NUnit.Framework;
using R8EOX.GameFlow;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for GameFlowManager screen navigation (NavigateTo, GoBack, breadcrumbs).
    /// Singleton and session tests live in GameFlowManagerSingletonTests.cs.
    /// </summary>
    [TestFixture]
    public sealed class GameFlowManagerNavigationTests
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
        public void NavigateTo_PushesScreen()
        {
            _manager.NavigateTo("main_menu");
            Assert.That(_manager.CurrentScreen, Is.EqualTo("main_menu"));
        }

        [Test]
        public void GoBack_PopsScreen()
        {
            _manager.NavigateTo("main_menu");
            _manager.NavigateTo("mode_select");
            _manager.GoBack();
            Assert.That(_manager.CurrentScreen, Is.EqualTo("main_menu"));
        }

        [Test]
        public void GetBreadcrumbs_ReturnsBottomFirst()
        {
            _manager.NavigateTo("main_menu");
            _manager.NavigateTo("mode_select");
            _manager.NavigateTo("car_select");

            string[] crumbs = _manager.GetBreadcrumbs();

            Assert.That(crumbs, Is.EqualTo(new[] { "main_menu", "mode_select", "car_select" }));
        }
    }
}
