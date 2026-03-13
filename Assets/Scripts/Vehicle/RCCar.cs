using UnityEngine;

/// <summary>
/// Main RC buggy vehicle controller. Rigidbody on root GameObject.
/// Ported from rc_car.gd with Godot→Unity coordinate mapping.
///
/// Key differences from Godot:
///   forward: -basis.z → transform.forward (+Z)
///   forwardSpeed: vel.dot(-basis.z) → Vector3.Dot(vel, transform.forward)
///   apply_force(f, offset) → AddForceAtPosition(f, worldPos)
///   Steering Y rotation: negated (Godot RH vs Unity LH Y rotation)
///   Wheel positions: Z coordinates flipped
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RCCar : MonoBehaviour
{
    // ---- Motor Presets (matching Godot MotorPreset enum) ----
    public enum MotorPreset
    {
        Motor21_5T, Motor17_5T, Motor13_5T, Motor9_5T, Motor5_5T, Motor1_5T, Custom
    }

    struct MotorData
    {
        public float engineForceMax, brakeForce, reverseForce, coastDrag, maxSpeed, throttleRampUp;
        public MotorData(float e, float b, float r, float c, float m, float t)
        {
            engineForceMax = e; brakeForce = b; reverseForce = r;
            coastDrag = c; maxSpeed = m; throttleRampUp = t;
        }
    }

    static readonly MotorData[] MOTOR_PRESETS = {
        new MotorData(15.5f, 13.2f,  8.5f, 2.0f, 13f, 3.0f),  // 21.5T
        new MotorData(18.0f, 15.3f,  9.9f, 2.5f, 20f, 4.0f),  // 17.5T
        new MotorData(26.0f, 22.1f, 14.3f, 3.0f, 27f, 5.5f),  // 13.5T
        new MotorData(34.0f, 28.9f, 18.7f, 3.5f, 34f, 7.0f),  // 9.5T
        new MotorData(44.0f, 37.4f, 24.2f, 4.0f, 44f, 9.0f),  // 5.5T
        new MotorData(56.0f, 47.6f, 30.8f, 5.0f, 56f, 12.0f), // 1.5T
    };

    [Header("Motor")]
    public MotorPreset motorPreset = MotorPreset.Motor13_5T;

    [Header("Engine")]
    public float engineForceMax = 26f;
    public float maxSpeed = 27f;
    public float brakeForce = 22.1f;
    public float reverseForce = 14.3f;
    public float coastDrag = 3.0f;

    [Header("Throttle Response")]
    public float throttleRampUp = 5.5f;
    public float throttleRampDown = 10f;

    [Header("Steering")]
    [Tooltip("Max steering angle in radians (~29 deg)")]
    public float steeringMax = 0.50f;
    public float steeringSpeed = 7f;
    [Tooltip("Speed (m/s) at which steering reduces")]
    public float steeringSpeedLimit = 8f;
    [Tooltip("Fraction of steeringMax kept at high speed")]
    public float steeringHighSpeedFactor = 0.4f;

    [Header("Suspension")]
    public float springStrength = 75f;
    public float springDampingVal = 4.25f;

    [Header("Traction")]
    public float gripCoeff = 0.7f;

    [Header("Centre of Mass")]
    public Vector3 comGround = new Vector3(0f, -0.20f, 0f);
    public Vector3 comAir = new Vector3(0f, -1.2f, 0f);

    [Header("Crash Physics")]
    public float tumbleEngageDeg = 50f;
    public float tumbleFullDeg = 70f;
    public float tumbleBounce = 0.35f;
    public float tumbleFriction = 0.3f;
    public float tumbleHysteresisDeg = 5f;

    // ---- Runtime state (read by TelemetryHUD) ----
    [HideInInspector] public float currentEngineForce = 0f;
    [HideInInspector] public float currentBrakeForce = 0f;
    [HideInInspector] public float smoothThrottle = 0f;
    [HideInInspector] public float currentSteering = 0f;
    [HideInInspector] public bool isAirborne = false;
    [HideInInspector] public float tumbleFactor = 0f;
    [HideInInspector] public float tiltAngle = 0f; // degrees
    [HideInInspector] public bool reverseEngaged = false;
    [HideInInspector] public float forwardSpeed = 0f;

    // Internal
    Rigidbody rb;
    RCInput input;
    RCAirPhysics airPhysics;
    Drivetrain drivetrain;
    RaycastWheel[] allWheels;
    RaycastWheel[] frontWheels;
    RaycastWheel[] rearWheels;
    PhysicMaterial physMat;

    bool wasAirborne = false;
    bool wasTumbling = false;
    int airborneFrames = 0;
    const int AIRBORNE_THRESHOLD = 5;
    bool flipRequested = false;
    Collider[] colliders;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<RCInput>();
        airPhysics = GetComponentInChildren<RCAirPhysics>();
        drivetrain = GetComponentInChildren<Drivetrain>();
    }

    void Start()
    {
        // Apply motor preset
        ApplyMotorPreset();

        // Configure rigidbody
        rb.mass = 1.5f;
        rb.centerOfMass = comGround;
        rb.drag = 0f; // All friction from wheel forces
        rb.angularDrag = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Create physics material for tumble dynamics
        physMat = new PhysicMaterial("CarBody");
        physMat.dynamicFriction = 0f;
        physMat.staticFriction = 0f;
        physMat.bounciness = 0.05f;
        physMat.frictionCombine = PhysicMaterialCombine.Minimum;
        physMat.bounceCombine = PhysicMaterialCombine.Maximum;

        colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
            col.material = physMat;

        // Discover wheels
        var allWheelsList = new System.Collections.Generic.List<RaycastWheel>();
        var frontList = new System.Collections.Generic.List<RaycastWheel>();
        var rearList = new System.Collections.Generic.List<RaycastWheel>();

        foreach (var w in GetComponentsInChildren<RaycastWheel>())
        {
            allWheelsList.Add(w);
            // In Unity: front wheels have positive Z (forward)
            if (w.transform.localPosition.z > 0f)
                frontList.Add(w);
            else
                rearList.Add(w);
        }

        allWheels = allWheelsList.ToArray();
        frontWheels = frontList.ToArray();
        rearWheels = rearList.ToArray();

        // Configure wheels
        if (drivetrain != null)
            drivetrain.UpdateLayout(frontWheels, rearWheels);

        ApplySuspensionSettings();
        ApplyTractionSettings();

        // Exclude car's own colliders from wheel raycasts
        int carLayer = gameObject.layer;
        foreach (var w in allWheels)
        {
            w.groundMask = ~(1 << carLayer);
            w.showDebug = false;
        }

        Debug.Log($"[RCCar] Motor={motorPreset} engine={engineForceMax}N max={maxSpeed}m/s " +
                  $"mass={rb.mass}kg spring={springStrength} damp={springDampingVal} grip={gripCoeff}");
    }

    void Update()
    {
        if (input != null && input.ResetPressed)
            flipRequested = true;
        if (input != null && input.DebugTogglePressed)
        {
            foreach (var w in allWheels)
                w.showDebug = !w.showDebug;
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (flipRequested)
        {
            flipRequested = false;
            DoFlip();
        }

        wasAirborne = isAirborne;
        isAirborne = CheckAirborne();

        // Tumble detection
        ComputeTumbleFactor();

        // Anti-flip CoM
        rb.centerOfMass = isAirborne ? comAir : comGround;

        // Physics material tumble blending
        if (physMat != null)
        {
            physMat.bounciness = Mathf.Lerp(0.05f, tumbleBounce, tumbleFactor);
            physMat.dynamicFriction = Mathf.Lerp(0f, tumbleFriction, tumbleFactor);
            physMat.staticFriction = Mathf.Lerp(0f, tumbleFriction, tumbleFactor);
        }

        // Read input
        float throttleRaw = input != null ? input.Throttle : 0f;
        float brakeIn = input != null ? input.Brake : 0f;
        float steerIn = input != null ? input.Steer : 0f;

        // Ramp throttle
        float rampRate = throttleRaw > smoothThrottle ? throttleRampUp : throttleRampDown;
        smoothThrottle = Mathf.MoveTowards(smoothThrottle, throttleRaw, rampRate * dt);

        // Forward speed along car's forward axis
        forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        if (isAirborne)
        {
            currentEngineForce = 0f;
            currentBrakeForce = 0f;
            foreach (var w in allWheels)
                w.motorForceShare = 0f;
            currentSteering = Mathf.MoveTowards(currentSteering, 0f, steeringSpeed * dt);
            if (airPhysics != null)
                airPhysics.Apply(dt, smoothThrottle, brakeIn, steerIn);
        }
        else
        {
            ApplyGroundDrive(dt, smoothThrottle, brakeIn, forwardSpeed);
            if (drivetrain != null)
                drivetrain.Distribute(currentEngineForce, frontWheels, rearWheels);
            ApplySteering(dt, steerIn, forwardSpeed);
        }

        // Drive all wheels
        foreach (var w in allWheels)
        {
            w.isBraking = currentBrakeForce > 0f && w.isMotor;
            w.ApplyWheelPhysics(rb, dt);

            // Apply steering rotation to steer wheels
            if (w.isSteer)
                w.transform.localRotation = Quaternion.Euler(0f, currentSteering * Mathf.Rad2Deg, 0f);
        }
    }

    void ApplyMotorPreset()
    {
        if (motorPreset == MotorPreset.Custom) return;
        int idx = (int)motorPreset;
        if (idx < 0 || idx >= MOTOR_PRESETS.Length) return;
        var p = MOTOR_PRESETS[idx];
        engineForceMax = p.engineForceMax;
        brakeForce = p.brakeForce;
        reverseForce = p.reverseForce;
        coastDrag = p.coastDrag;
        maxSpeed = p.maxSpeed;
        throttleRampUp = p.throttleRampUp;
    }

    void ApplyGroundDrive(float dt, float throttleIn, float brakeIn, float fwdSpeed)
    {
        // Reverse-gear ESC state machine
        if (throttleIn > 0f || fwdSpeed > 0.50f)
            reverseEngaged = false;
        else if (brakeIn > 0f && fwdSpeed < 0.25f)
            reverseEngaged = true;

        if (throttleIn > 0f)
        {
            currentEngineForce = rb.velocity.magnitude >= maxSpeed ? 0f : throttleIn * engineForceMax;
            currentBrakeForce = 0f;
        }
        else if (brakeIn > 0f)
        {
            if (reverseEngaged)
            {
                currentEngineForce = -brakeIn * reverseForce;
                currentBrakeForce = 0f;
            }
            else
            {
                currentEngineForce = 0f;
                currentBrakeForce = brakeIn * brakeForce;
            }
        }
        else
        {
            reverseEngaged = false;
            currentEngineForce = 0f;
            currentBrakeForce = coastDrag;
        }
    }

    void ApplySteering(float dt, float steerIn, float fwdSpeed)
    {
        float spd = Mathf.Abs(fwdSpeed);
        float t = Mathf.Clamp01(spd / steeringSpeedLimit);
        float effectiveMax = Mathf.Lerp(steeringMax, steeringMax * steeringHighSpeedFactor, t);

        // Flip steering direction when reversing
        float steerSign = fwdSpeed < -0.25f ? -1f : 1f;
        float target = steerIn * effectiveMax * steerSign;
        currentSteering = Mathf.MoveTowards(currentSteering, target, steeringSpeed * dt);
    }

    void ComputeTumbleFactor()
    {
        // Tilt angle from upright (always computed for telemetry)
        tiltAngle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(transform.up, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;

        // Tumble is a ground-contact concept — airborne tilt is intentional
        if (isAirborne)
        {
            tumbleFactor = 0f;
            wasTumbling = false;
            return;
        }

        float fullDeg = tumbleFullDeg;
        float engageDeg = wasTumbling ? tumbleEngageDeg - tumbleHysteresisDeg : tumbleEngageDeg;

        if (tiltAngle <= engageDeg)
        {
            tumbleFactor = 0f;
        }
        else if (tiltAngle >= fullDeg)
        {
            tumbleFactor = 1f;
        }
        else
        {
            float tt = (tiltAngle - engageDeg) / (fullDeg - engageDeg);
            tumbleFactor = tt * tt * (3f - 2f * tt); // smoothstep
        }

        wasTumbling = tumbleFactor > 0f;
    }

    bool CheckAirborne()
    {
        bool offGround = true;
        foreach (var w in allWheels)
        {
            if (w.isOnGround)
            {
                offGround = false;
                break;
            }
        }

        if (offGround)
            airborneFrames = Mathf.Min(airborneFrames + 1, AIRBORNE_THRESHOLD);
        else
            airborneFrames = 0;

        return airborneFrames >= AIRBORNE_THRESHOLD;
    }

    void DoFlip()
    {
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        transform.position += Vector3.up * 1.4f;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void ApplySuspensionSettings()
    {
        foreach (var w in allWheels)
        {
            w.springStrength = springStrength;
            w.springDamping = springDampingVal;
        }
    }

    public void ApplyTractionSettings()
    {
        foreach (var w in allWheels)
            w.gripCoeff = gripCoeff;
    }

    // ---- Public API ----

    public float GetSpeedKmh()
    {
        return rb.velocity.magnitude * 3.6f;
    }

    public float GetForwardSpeedKmh()
    {
        return Vector3.Dot(rb.velocity, transform.forward) * 3.6f;
    }

    public float GetSlip()
    {
        float slip = 0f;
        int count = 0;
        foreach (var w in allWheels)
        {
            if (w.isMotor)
            {
                slip += w.slipRatio;
                count++;
            }
        }
        return count > 0 ? slip / count : 0f;
    }

    public RaycastWheel[] GetAllWheels() => allWheels;
}
