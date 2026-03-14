using UnityEngine;

/// <summary>
/// Runtime diagnostic script for debugging phantom input issues.
/// Attach to the car GameObject to log raw axis values and processed input.
/// Remove after debugging is complete.
/// </summary>
public class InputDiagnostics : MonoBehaviour
{
    void Start()
    {
        // Log ALL raw joystick axes 0-19
        for (int i = 0; i < 20; i++)
        {
            float val = Input.GetAxisRaw($"joystick axis {i}");
            if (Mathf.Abs(val) > 0.001f)
                Debug.Log($"[InputDiag] STARTUP axis{i}={val:F4}");
        }
        // Log named axes
        string[] axes = { "Horizontal", "Vertical", "RightTrigger", "LeftTrigger", "CombinedTriggers" };
        foreach (var a in axes)
        {
            try
            {
                Debug.Log($"[InputDiag] STARTUP {a}={Input.GetAxisRaw(a):F4}");
            }
            catch (System.Exception e)
            {
                Debug.Log($"[InputDiag] STARTUP {a} axis not found: {e.Message}");
            }
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

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            Debug.Log($"[InputDiag] F{_frame} vel={rb.velocity} |v|={rb.velocity.magnitude:F3}");
    }
}
