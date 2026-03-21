using System;
using NUnit.Framework;
using R8EOX.GameFlow;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Tests for GameFlowStateMachine basic transitions and event firing.</summary>
    [TestFixture]
    public sealed class GameFlowStateTransitionTests
    {
        private GameFlowStateMachine _sm;

        [SetUp]
        public void SetUp() { _sm = new GameFlowStateMachine(); }

        [Test]
        public void InitialState_IsBoot()
        {
            Assert.AreEqual(GameState.Boot, _sm.CurrentState);
        }

        [Test]
        public void TransitionTo_BootToSplash_Succeeds()
        {
            _sm.TransitionTo(GameState.Splash);
            Assert.AreEqual(GameState.Splash, _sm.CurrentState);
        }

        [Test]
        public void TransitionTo_SplashToMainMenu_Succeeds()
        {
            _sm.TransitionTo(GameState.Splash);
            _sm.TransitionTo(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _sm.CurrentState);
        }

        [Test]
        public void TransitionTo_InvalidTransition_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _sm.TransitionTo(GameState.Playing));
        }

        [Test]
        public void CanTransitionTo_ValidTarget_ReturnsTrue()
        {
            Assert.IsTrue(_sm.CanTransitionTo(GameState.Splash));
        }

        [Test]
        public void CanTransitionTo_InvalidTarget_ReturnsFalse()
        {
            Assert.IsFalse(_sm.CanTransitionTo(GameState.Playing));
        }

        [Test]
        public void TransitionTo_FiresEvent()
        {
            GameState from = GameState.Results;
            GameState to = GameState.Results;
            _sm.OnStateChanged += (f, t) => { from = f; to = t; };
            _sm.TransitionTo(GameState.Splash);
            Assert.AreEqual(GameState.Boot, from);
            Assert.AreEqual(GameState.Splash, to);
        }

        [Test]
        public void MainMenu_CanGoDirectlyToLoading()
        {
            _sm.TransitionTo(GameState.Splash);
            _sm.TransitionTo(GameState.MainMenu);
            _sm.TransitionTo(GameState.Loading);
            Assert.AreEqual(GameState.Loading, _sm.CurrentState);
        }

        [Test]
        public void ModeSelect_CanGoBackToMainMenu()
        {
            _sm.TransitionTo(GameState.Splash);
            _sm.TransitionTo(GameState.MainMenu);
            _sm.TransitionTo(GameState.ModeSelect);
            _sm.TransitionTo(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _sm.CurrentState);
        }
    }
}
