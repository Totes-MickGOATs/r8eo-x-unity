using UnityEngine;

/// <summary>
/// Input abstraction for the RC buggy. Uses legacy Input Manager.
/// Keyboard works immediately. Gamepad triggers use three axis definitions:
///   - "RightTrigger" (axis 10) and "LeftTrigger" (axis 9) for separate triggers
///   - "CombinedTriggers" (axis 3) as fallback for combined LT/RT axis
/// All three are pre-configured in InputManager.asset.
/// </summary>
public class RCInput : MonoBehaviour
{
    [Header("Steering")]
    [Tooltip("Exponent applied to steering input for non-linear response")]
    public float steerCurveExponent = 1.5f;

    [Header("Gamepad")]
    [Tooltip("Deadzone for trigger axes — values below this are treated as 0")]
    public float triggerDeadzone = 0.15f;
    [Tooltip("Deadzone for steering stick")]
    public float steerDeadzone = 0.1f;

    // Processed input values (read by RCCar)
    public float Throttle { get; private set; }
    public float Brake { get; private set; }
    public float Steer { get; private set; }
    public bool ResetPressed { get; private set; }
    public bool DebugTogglePressed { get; private set; }

    // Trigger mode detection
    enum TriggerMode { Detecting, Separate, Combined, None }
    TriggerMode triggerMode = TriggerMode.Detecting;
    int detectFrames = 0;
    const int DETECT_WINDOW = 300; // ~5 sec at 60fps

    void Update()
    {
        // --- Throttle ---
        float kbThrottle = Input.GetKey(KeyCode.W) ? 1f : 0f;
        float gpThrottle = GetGamepadThrottle();
        Throttle = Mathf.Max(kbThrottle, gpThrottle);

        // --- Brake ---
        float kbBrake = Input.GetKey(KeyCode.S) ? 1f : 0f;
        float gpBrake = GetGamepadBrake();
        Brake = Mathf.Max(kbBrake, gpBrake);

        // --- Steer: A/D keys or left stick X ---
        float kbSteer = 0f;
        if (Input.GetKey(KeyCode.D)) kbSteer += 1f;
        if (Input.GetKey(KeyCode.A)) kbSteer -= 1f;

        float gpSteer = Input.GetAxisRaw("Horizontal");
        // Apply deadzone to gamepad stick
        if (Mathf.Abs(gpSteer) < steerDeadzone) gpSteer = 0f;

        float rawSteer = Mathf.Abs(gpSteer) > Mathf.Abs(kbSteer) ? gpSteer : kbSteer;

        // Apply curve exponent for non-linear response
        Steer = Mathf.Sign(rawSteer) * Mathf.Pow(Mathf.Abs(rawSteer), steerCurveExponent);

        // --- Action buttons ---
        ResetPressed = Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton3); // Y
        DebugTogglePressed = Input.GetKeyDown(KeyCode.F2);

        // --- Auto-detect trigger mode ---
        if (triggerMode == TriggerMode.Detecting)
            DetectTriggerMode();
    }

    void DetectTriggerMode()
    {
        detectFrames++;

        float sepRT = Mathf.Abs(Input.GetAxisRaw("RightTrigger"));
        float sepLT = Mathf.Abs(Input.GetAxisRaw("LeftTrigger"));
        float combined = Mathf.Abs(Input.GetAxisRaw("CombinedTriggers"));

        // If separate axes show clear input, lock to separate mode
        if (sepRT > 0.3f || sepLT > 0.3f)
        {
            triggerMode = TriggerMode.Separate;
            Debug.Log("[RCInput] Gamepad triggers: separate axes (9/10)");
            return;
        }

        // If combined axis shows clear input, lock to combined mode
        if (combined > 0.3f)
        {
            triggerMode = TriggerMode.Combined;
            Debug.Log("[RCInput] Gamepad triggers: combined axis (3)");
            return;
        }

        // After detection window, default to no gamepad triggers (keyboard only)
        // User can still use triggers — they'll be detected on first strong press
        if (detectFrames >= DETECT_WINDOW)
        {
            triggerMode = TriggerMode.None;
            Debug.Log("[RCInput] No gamepad triggers detected — using keyboard only. " +
                      "Press a trigger firmly to enable gamepad.");
        }
    }

    float GetGamepadThrottle()
    {
        switch (triggerMode)
        {
            case TriggerMode.Separate:
                return ApplyDeadzone(Input.GetAxisRaw("RightTrigger"));

            case TriggerMode.Combined:
                return ApplyDeadzone(Input.GetAxisRaw("CombinedTriggers"));

            case TriggerMode.Detecting:
                // During detection, only accept strong inputs to avoid drift
                float rt = Input.GetAxisRaw("RightTrigger");
                if (rt > 0.3f) return ApplyDeadzone(rt);
                float com = Input.GetAxisRaw("CombinedTriggers");
                if (com > 0.3f) return ApplyDeadzone(com);
                return 0f;

            default: // None
                return 0f;
        }
    }

    float GetGamepadBrake()
    {
        switch (triggerMode)
        {
            case TriggerMode.Separate:
                return ApplyDeadzone(Input.GetAxisRaw("LeftTrigger"));

            case TriggerMode.Combined:
                return ApplyDeadzone(-Input.GetAxisRaw("CombinedTriggers"));

            case TriggerMode.Detecting:
                float lt = Input.GetAxisRaw("LeftTrigger");
                if (lt > 0.3f) return ApplyDeadzone(lt);
                float com = -Input.GetAxisRaw("CombinedTriggers");
                if (com > 0.3f) return ApplyDeadzone(com);
                return 0f;

            default:
                return 0f;
        }
    }

    float ApplyDeadzone(float raw)
    {
        if (Mathf.Abs(raw) < triggerDeadzone) return 0f;
        // Remap from [deadzone, 1] to [0, 1] for smooth response
        float sign = Mathf.Sign(raw);
        float remapped = (Mathf.Abs(raw) - triggerDeadzone) / (1f - triggerDeadzone);
        return Mathf.Clamp01(sign * remapped);
    }
}
