using NUnit.Framework;
using UnityEngine.InputSystem;
using R8EOX.Input;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for InputBridge — the thin typed wrapper over R8EOXInputActions.
    /// Verifies the public API surface, construction contract, and delegation to the
    /// underlying generated actions class.
    /// </summary>
    public class InputBridgeTests
    {
        // ---- Construction ----

        [Test]
        public void Constructor_ValidActions_DoesNotThrow()
        {
            // InputBridge must accept a live R8EOXInputActions without throwing.
            // This is the minimum viability check for the wrapper.
            var actions = new R8EOXInputActions();
            Assert.DoesNotThrow(() =>
            {
                var bridge = new InputBridge(actions);
                Assert.IsNotNull(bridge);
            });
            actions.Dispose();
        }

        [Test]
        public void Constructor_NullActions_ThrowsArgumentNullException()
        {
            // Passing null must throw immediately — fail-fast prevents hidden NREs later.
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                var _ = new InputBridge(null);
            });
        }

        // ---- Enable / Disable delegation ----

        [Test]
        public void Enable_SetsGameplayMapEnabled()
        {
            var actions = new R8EOXInputActions();
            var bridge = new InputBridge(actions);

            bridge.Enable();

            Assert.IsTrue(actions.Gameplay.enabled,
                "Enable() must enable the Gameplay action map");

            actions.Gameplay.Disable();
            actions.Dispose();
        }

        [Test]
        public void Disable_SetsGameplayMapDisabled()
        {
            var actions = new R8EOXInputActions();
            var bridge = new InputBridge(actions);
            bridge.Enable();

            bridge.Disable();

            Assert.IsFalse(actions.Gameplay.enabled,
                "Disable() must disable the Gameplay action map");

            actions.Dispose();
        }

        // ---- API surface existence (compile-time guard) ----

        [Test]
        public void Properties_ThrottleBrakeSteer_AreAccessible()
        {
            // This test exists purely to enforce at compile time that the three
            // axis properties exist and return float. If InputBridge is refactored
            // to remove any of them, this test will fail to compile before running.
            var actions = new R8EOXInputActions();
            var bridge = new InputBridge(actions);
            bridge.Enable();

            float throttle = bridge.Throttle;
            float brake    = bridge.Brake;
            float steer    = bridge.Steer;

            // In EditMode with no device active, values must be zero.
            Assert.AreEqual(0f, throttle, 0.0001f, "Throttle must be zero with no device active");
            Assert.AreEqual(0f, brake,    0.0001f, "Brake must be zero with no device active");
            Assert.AreEqual(0f, steer,    0.0001f, "Steer must be zero with no device active");

            bridge.Disable();
            actions.Dispose();
        }

        [Test]
        public void Properties_ButtonActions_AreAccessible()
        {
            // Ensures Reset, Pause, CameraCycle, DebugToggle properties compile and
            // return bool. Values must be false with no device active.
            var actions = new R8EOXInputActions();
            var bridge = new InputBridge(actions);
            bridge.Enable();

            bool reset       = bridge.WasResetPressedThisFrame;
            bool pause       = bridge.WasPausePressedThisFrame;
            bool camera      = bridge.WasCameraCyclePressedThisFrame;
            bool debugToggle = bridge.WasDebugTogglePressedThisFrame;

            Assert.IsFalse(reset,       "Reset must be false with no device active");
            Assert.IsFalse(pause,       "Pause must be false with no device active");
            Assert.IsFalse(camera,      "CameraCycle must be false with no device active");
            Assert.IsFalse(debugToggle, "DebugToggle must be false with no device active");

            bridge.Disable();
            actions.Dispose();
        }

        // ---- Dispose forwarding ----

        [Test]
        public void Dispose_DisposesUnderlyingActions()
        {
            // InputBridge.Dispose() must call Dispose on the underlying R8EOXInputActions
            // so asset destruction happens. We verify by calling it and confirming no exception.
            var actions = new R8EOXInputActions();
            var bridge = new InputBridge(actions);

            Assert.DoesNotThrow(() => bridge.Dispose(),
                "Dispose() must not throw");
        }
    }
}
