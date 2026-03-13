using UnityEngine;

/// <summary>
/// Airborne pitch/roll/gyro torques for RC buggy.
/// Ported from rc_air_physics.gd with coordinate system adjustments.
///
/// Coordinate mapping (Godot → Unity):
///   Pitch axis: basis.x → -transform.right (negated because Z-forward flips pitch sense)
///   Roll axis:  basis.z → transform.forward (both are local Z; rotation around Z uses X/Y only)
///   Gyro damp:  direct port — angular velocity damping is coordinate-agnostic
/// </summary>
public class RCAirPhysics : MonoBehaviour
{
    [Header("Pitch (throttle/brake in air)")]
    public float pitchTorque = 40f;
    public float pitchSensitivity = 1f;

    [Header("Roll (steering in air)")]
    public float rollTorque = 12.8f;
    public float rollSensitivity = 0.6f;

    [Header("Gyroscopic Stabilization")]
    [Tooltip("Damps angular velocity based on wheel spin speed. 0 = off.")]
    public float gyroStrength = 4f;
    [Tooltip("Wheel RPM at which full gyro effect is reached.")]
    public float gyroFullRpm = 125f;

    Rigidbody rb;
    RaycastWheel[] wheels;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        wheels = GetComponentsInChildren<RaycastWheel>();
        if (wheels.Length == 0)
            wheels = transform.parent.GetComponentsInChildren<RaycastWheel>();
    }

    /// <summary>
    /// Apply air control torques. Called by RCCar when airborne.
    /// </summary>
    public void Apply(float dt, float throttle, float brake, float steer)
    {
        // Pitch: throttle → nose UP, brake → nose DOWN
        // Godot uses basis.x; in Unity we negate because +X torque pitches nose DOWN
        float pitchInput = throttle - brake;
        float pitchForce = pitchInput * pitchTorque * pitchSensitivity;
        rb.AddTorque(-rb.transform.right * pitchForce);

        // Roll: subtle counter-roll from steering input
        // Godot uses basis.z (backward); in Unity, transform.forward gives same rotation sense
        float rollForce = steer * rollTorque * rollSensitivity;
        rb.AddTorque(rb.transform.forward * rollForce);

        // Gyroscopic stabilization: resist tumbling based on wheel spin
        float avgRpm = GetAvgWheelRpm();
        if (avgRpm > 10f)
        {
            float gyroFactor = Mathf.Min(avgRpm / gyroFullRpm, 1f) * gyroStrength;
            Vector3 damp = -rb.angularVelocity * gyroFactor;
            damp.y = 0f; // allow yaw — natural to spin slightly on landing
            rb.AddTorque(damp);
        }
    }

    float GetAvgWheelRpm()
    {
        if (wheels == null || wheels.Length == 0) return 0f;
        float total = 0f;
        foreach (var w in wheels)
            total += Mathf.Abs(w.wheelRpm);
        return total / wheels.Length;
    }
}
