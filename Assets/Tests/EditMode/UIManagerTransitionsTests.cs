using NUnit.Framework;
using R8EOX.UI;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for UIManagerTransitions — the ExitAndDestroy helper extracted from UIManager.
    /// </summary>
    public class UIManagerTransitionsTests
    {
        [Test]
        public void ExitAndDestroy_NonMonoBehaviour_DoesNotThrow()
        {
            // ExitAndDestroy must handle IScreen implementations that are not MonoBehaviour
            // without throwing.
            var screen = new StubScreen();
            Assert.DoesNotThrow(() => UIManagerTransitions.ExitAndDestroy(screen));
            Assert.IsTrue(screen.ExitWasCalled);
        }

        [Test]
        public void ExitAndDestroy_CalledTwice_NoException()
        {
            // Pure idempotency: calling ExitAndDestroy twice on a non-MB screen must not throw.
            var screen = new StubScreen();
            Assert.DoesNotThrow(() =>
            {
                UIManagerTransitions.ExitAndDestroy(screen);
                UIManagerTransitions.ExitAndDestroy(screen);
            });
        }

        // ---- Minimal stub ----

        private class StubScreen : IScreen
        {
            public bool ExitWasCalled { get; private set; }
            public void Enter(object data = null) { }
            public void Exit() { ExitWasCalled = true; }
            public System.Collections.IEnumerator AnimateIn() { yield break; }
            public System.Collections.IEnumerator AnimateOut() { yield break; }
        }
    }
}
