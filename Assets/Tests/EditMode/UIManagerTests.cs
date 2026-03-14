using System;
using NUnit.Framework;
using R8EOX.GameFlow;
using R8EOX.UI;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for UIManager — initialization and overlay count.
    /// </summary>
    [TestFixture]
    public sealed class UIManagerTests
    {
        private GameObject _managerGo;
        private UIManager _uiManager;

        [SetUp]
        public void SetUp()
        {
            // Clean up any leftover GameFlowManager
            if (GameFlowManager.Instance != null)
            {
                Object.DestroyImmediate(GameFlowManager.Instance.gameObject);
            }

            _managerGo = new GameObject("UIManager");
            _uiManager = _managerGo.AddComponent<UIManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_managerGo != null)
            {
                Object.DestroyImmediate(_managerGo);
            }

            if (GameFlowManager.Instance != null)
            {
                Object.DestroyImmediate(GameFlowManager.Instance.gameObject);
            }
        }

        [Test]
        public void Init_NullGameFlow_Throws()
        {
            var flowGo = new GameObject("Flow");
            var flow = flowGo.AddComponent<GameFlowManager>();

            Assert.Throws<ArgumentNullException>(() =>
                _uiManager.Init(null, flow));

            Object.DestroyImmediate(flowGo);
        }

        [Test]
        public void Init_NullNavigator_Throws()
        {
            var flowGo = new GameObject("Flow");
            var flow = flowGo.AddComponent<GameFlowManager>();

            Assert.Throws<ArgumentNullException>(() =>
                _uiManager.Init(flow, null));

            Object.DestroyImmediate(flowGo);
        }

        [Test]
        public void OverlayCount_InitiallyZero()
        {
            Assert.That(_uiManager.OverlayCount, Is.EqualTo(0));
        }
    }
}
