using UnityEngine;
using UnityEngine.InputSystem;

namespace R8EOX.Debug
{
    /// <summary>
    /// Runtime diagnostic script for debugging input issues.
    /// Attach to the car GameObject to log device state and processed input.
    /// </summary>
    public class InputDiagnostics : MonoBehaviour
    {
        void Start()
        {
            foreach (var device in InputSystem.devices)
                UnityEngine.Debug.Log($"[InputDiag] Device: {device.displayName} ({device.deviceId})");

            if (Gamepad.current != null)
            {
                UnityEngine.Debug.Log($"[InputDiag] Gamepad: {Gamepad.current.displayName}");
                UnityEngine.Debug.Log($"[InputDiag] RT={Gamepad.current.rightTrigger.ReadValue():F4}");
                UnityEngine.Debug.Log($"[InputDiag] LT={Gamepad.current.leftTrigger.ReadValue():F4}");
                UnityEngine.Debug.Log($"[InputDiag] LeftStick={Gamepad.current.leftStick.ReadValue()}");
            }
            else
            {
                UnityEngine.Debug.Log("[InputDiag] No gamepad detected");
            }
        }

        private int _frame;
        void Update()
        {
            _frame++;
            if (_frame % 30 != 0) return;

            var input = GetComponent<R8EOX.Input.RCInput>();
            if (input != null)
                UnityEngine.Debug.Log($"[InputDiag] F{_frame} T={input.Throttle:F4} B={input.Brake:F4} S={input.Steer:F4}");

            if (Gamepad.current != null)
                UnityEngine.Debug.Log($"[InputDiag] F{_frame} RT={Gamepad.current.rightTrigger.ReadValue():F4} LT={Gamepad.current.leftTrigger.ReadValue():F4}");

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
                UnityEngine.Debug.Log($"[InputDiag] F{_frame} vel={rb.velocity} |v|={rb.velocity.magnitude:F3}");
        }
    }
}
