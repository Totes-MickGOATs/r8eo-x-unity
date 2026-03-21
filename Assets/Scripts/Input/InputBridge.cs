using System;

namespace R8EOX.Input
{
    /// <summary>
    /// Thin typed bridge over the generated R8EOXInputActions. Exposes only the
    /// actions that the game's runtime assemblies actually read, keeping consumers
    /// isolated from the full generated API surface.
    ///
    /// Design rules:
    ///   - No string-based action lookups (policy linter bans FindAction("string")).
    ///   - No direct UnityEngine log calls (runtime assembly; use RuntimeLog).
    ///   - MonoBehaviour-free so it can be unit-tested in EditMode without a scene.
    ///
    /// Consumed by: RCInput (MonoBehaviour lifecycle owner).
    /// </summary>
    public sealed class InputBridge : IDisposable
    {
        // ---- Private State ----

        private readonly R8EOXInputActions _actions;
        private bool _disposed;


        // ---- Construction ----

        /// <param name="actions">
        ///   A fully constructed R8EOXInputActions instance. Must not be null.
        ///   Ownership of disposal is transferred to this bridge.
        /// </param>
        public InputBridge(R8EOXInputActions actions)
        {
            if (actions == null) throw new ArgumentNullException(nameof(actions));
            _actions = actions;
        }


        // ---- Lifecycle ----

        /// <summary>Enables the Gameplay action map so inputs are received.</summary>
        public void Enable()
        {
            ThrowIfDisposed();
            _actions.Gameplay.Enable();
        }

        /// <summary>Disables the Gameplay action map.</summary>
        public void Disable()
        {
            ThrowIfDisposed();
            _actions.Gameplay.Disable();
        }

        /// <summary>
        /// Disposes the underlying R8EOXInputActions asset.
        /// Must be called when the owning MonoBehaviour is destroyed.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _actions.Dispose();
        }


        // ---- Axis Inputs (float, read every Update) ----

        /// <summary>Throttle axis in [0, 1]. Right trigger / W key.</summary>
        public float Throttle
        {
            get { ThrowIfDisposed(); return _actions.Gameplay.Throttle.ReadValue<float>(); }
        }

        /// <summary>Brake axis in [0, 1]. Left trigger / S key.</summary>
        public float Brake
        {
            get { ThrowIfDisposed(); return _actions.Gameplay.Brake.ReadValue<float>(); }
        }

        /// <summary>Steering axis in [-1, 1]. Left stick X / A-D keys.</summary>
        public float Steer
        {
            get { ThrowIfDisposed(); return _actions.Gameplay.Steer.ReadValue<float>(); }
        }


        // ---- Button Inputs (bool, sampled via WasPressedThisFrame) ----

        /// <summary>True during the frame the Reset action was first pressed.</summary>
        public bool WasResetPressedThisFrame
        {
            get { ThrowIfDisposed(); return _actions.Gameplay.Reset.WasPressedThisFrame(); }
        }

        /// <summary>True during the frame the Pause action was first pressed.</summary>
        public bool WasPausePressedThisFrame
        {
            get { ThrowIfDisposed(); return _actions.Gameplay.Pause.WasPressedThisFrame(); }
        }

        /// <summary>True during the frame the CameraCycle action was first pressed.</summary>
        public bool WasCameraCyclePressedThisFrame
        {
            get { ThrowIfDisposed(); return _actions.Gameplay.CameraCycle.WasPressedThisFrame(); }
        }

        /// <summary>True during the frame the DebugToggle action was first pressed.</summary>
        public bool WasDebugTogglePressedThisFrame
        {
            get { ThrowIfDisposed(); return _actions.Gameplay.DebugToggle.WasPressedThisFrame(); }
        }


        // ---- Private Helpers ----

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InputBridge));
        }
    }
}
