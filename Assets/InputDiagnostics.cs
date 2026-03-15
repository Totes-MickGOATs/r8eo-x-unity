using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runtime diagnostic script for debugging input issues.
/// Attach to the car GameObject to log device state and processed input.
/// </summary>
public class InputDiagnostics : MonoBehaviour
{
    void Start()
    {
        foreach (var device in InputSystem.devices)
            Debug.Log($"[InputDiag] Device: {device.displayName} ({device.deviceId})");

        if (Gamepad.current != null)
        {
            Debug.Log($"[InputDiag] Gamepad: {Gamepad.current.displayName}");
            Debug.Log($"[InputDiag] RT={Gamepad.current.rightTrigger.ReadValue():F4}");
            Debug.Log($"[InputDiag] LT={Gamepad.current.leftTrigger.ReadValue():F4}");
            Debug.Log($"[InputDiag] LeftStick={Gamepad.current.leftStick.ReadValue()}");
        }
        else
        {
            Debug.Log("[InputDiag] No gamepad detected");
        }
    }

    private int _frame;
    void Update()
    {
        _frame++;
        if (_frame % 30 != 0) return;

        var input = GetComponent<R8EOX.Input.RCInput>();
        if (input != null)
            Debug.Log($"[InputDiag] F{_frame} T={input.Throttle:F4} B={input.Brake:F4} S={input.Steer:F4}");

        if (Gamepad.current != null)
            Debug.Log($"[InputDiag] F{_frame} RT={Gamepad.current.rightTrigger.ReadValue():F4} LT={Gamepad.current.leftTrigger.ReadValue():F4}");

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            Debug.Log($"[InputDiag] F{_frame} vel={rb.velocity} |v|={rb.velocity.magnitude:F3}");
    }
}
