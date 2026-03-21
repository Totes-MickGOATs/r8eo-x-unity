using NUnit.Framework;
using R8EOX.GameFlow;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Tests for GameFlowStateMachine full navigation flows (boot-to-play, pause, results).</summary>
    [TestFixture]
    public sealed class GameFlowNavigationFlowTests
    {
        private GameFlowStateMachine _sm;

        [SetUp]
        public void SetUp() { _sm = new GameFlowStateMachine(); }

        private void NavigateToPlaying()
        {
            _sm.TransitionTo(GameState.Splash);
            _sm.TransitionTo(GameState.MainMenu);
            _sm.TransitionTo(GameState.ModeSelect);
            _sm.TransitionTo(GameState.CarSelect);
            _sm.TransitionTo(GameState.TrackSelect);
            _sm.TransitionTo(GameState.Loading);
            _sm.TransitionTo(GameState.Playing);
        }

        [Test]
        public void FullMenuFlow_BootToPlaying()
        {
            NavigateToPlaying();
            Assert.AreEqual(GameState.Playing, _sm.CurrentState);
        }

        [Test]
        public void PauseResume_Cycle()
        {
            NavigateToPlaying();
            _sm.TransitionTo(GameState.Paused);
            Assert.AreEqual(GameState.Paused, _sm.CurrentState);
            _sm.TransitionTo(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _sm.CurrentState);
        }

        [Test]
        public void Results_CanReturnToMenu()
        {
            NavigateToPlaying();
            _sm.TransitionTo(GameState.Results);
            _sm.TransitionTo(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _sm.CurrentState);
        }

        [Test]
        public void Results_CanRestart()
        {
            NavigateToPlaying();
            _sm.TransitionTo(GameState.Results);
            _sm.TransitionTo(GameState.Loading);
            Assert.AreEqual(GameState.Loading, _sm.CurrentState);
        }

        [Test]
        public void Playing_CanReturnToMainMenu()
        {
            NavigateToPlaying();
            _sm.TransitionTo(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _sm.CurrentState);
        }
    }
}
