using UnityEngine;

/// <summary>
/// Per-wheel physics: suspension spring, lateral grip, longitudinal friction, motor force.
/// Ported from raycast_wheel.gd with Godot→Unity coordinate mapping.
///
/// Coordinate mapping:
///   forward:  -basis.z → transform.forward
///   right:     basis.x → transform.right
///   down:     -basis.y → -transform.up
///   apply_force(f, offset) → AddForceAtPosition(f, worldPos)
///   get_point_velocity manual → rb.GetPointVelocity(worldPos)
///
/// Attach as MonoBehaviour on each wheel pivot GameObject (child of RCCar root).
/// Each wheel pivot should have a child "WheelVisual" with the tire mesh.
/// </summary>
public class RaycastWheel : MonoBehaviour
{
    [Header("Suspension")]
    [HideInInspector] public float springStrength = 75f;  // Set by RCCar
    [HideInInspector] public float springDamping = 4.25f; // Set by RCCar
    public float restDistance = 0.20f;
    public float overExtend = 0.08f;
    public float maxSpringForce = 50f;
    public float minSpringLen = 0.032f;

    [Header("Wheel")]
    public float wheelRadius = 0.166f;

    [Header("Motor/Steer")]
    public bool isMotor = false;
    public bool isSteer = false;

    [Header("Traction")]
    [HideInInspector] public float gripCoeff = 0.7f; // Set by RCCar
    public float zTraction = 0.10f;
    public float zBrakeTraction = 0.5f;
    public AnimationCurve gripCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.15f, 0.8f),
        new Keyframe(0.4f, 1.0f),
        new Keyframe(1.0f, 0.7f)
    );

    [Header("Ground Detection")]
    public LayerMask groundMask = ~0; // Default: hit everything

    [Header("Debug")]
    public bool showDebug = false;

    // Runtime state — read by RCCar and TelemetryHUD
    [HideInInspector] public bool isBraking = false;
    [HideInInspector] public float gripFactor = 0f;
    [HideInInspector] public float slipRatio = 0f;
    [HideInInspector] public bool isOnGround = false;
    [HideInInspector] public float wheelRpm = 0f;
    [HideInInspector] public float motorForceShare = 0f;
    [HideInInspector] public float lastSpringLen = 0f;
    [HideInInspector] public float lastGripLoad = 0f;

    // Visual
    Transform wheelVisual;
    Transform hubVisual;

    // Internal
    RCCar cachedCar;
    float prevSpringLen;
    float rayLen;

    // Per-frame physics state
    Vector3 contactPoint;
    Vector3 contactNormal;
    float springLen;
    float springForce;
    float suspensionForce;
    float gripLoad;
    Vector3 tireVelocity;
    float speed;
    float fSpeed;
    Vector3 yForce;
    Vector3 xForce;
    Vector3 zForce;
    Vector3 motorForce;

    // Debug
    const float DBG_SCALE = 0.024f;

    void Awake()
    {
        rayLen = restDistance + overExtend + wheelRadius;
        prevSpringLen = restDistance;

        // Find visual children
        Transform wm = transform.Find("WheelVisual");
        if (wm != null) wheelVisual = wm;
        Transform hm = transform.Find("HubVisual");
        if (hm != null) hubVisual = hm;
    }

    /// <summary>
    /// Main physics entry point. Called by RCCar each FixedUpdate.
    /// </summary>
    public void ApplyWheelPhysics(Rigidbody carRb, float dt)
    {
        if (cachedCar == null)
            cachedCar = carRb.GetComponent<RCCar>();
        rayLen = restDistance + overExtend + wheelRadius;

        // Raycast downward from wheel anchor
        Ray ray = new Ray(transform.position, -transform.up);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, rayLen, groundMask))
        {
            // Airborne — droop mesh toward full extension
            gripFactor = 0f;
            slipRatio = 0f;
            isOnGround = false;
            prevSpringLen = restDistance + overExtend;

            float droopTarget = -(restDistance + overExtend);
            if (wheelVisual != null)
                wheelVisual.localPosition = new Vector3(0f,
                    Mathf.MoveTowards(wheelVisual.localPosition.y, droopTarget, 20f * dt), 0f);
            if (hubVisual != null)
                hubVisual.localPosition = new Vector3(0f,
                    Mathf.MoveTowards(hubVisual.localPosition.y, droopTarget, 20f * dt), 0f);
            return;
        }

        // Validate normal (reject upside-down contacts)
        if (hit.normal.y < 0f)
        {
            isOnGround = false;
            prevSpringLen = restDistance + overExtend;
            return;
        }

        isOnGround = true;
        contactPoint = hit.point;
        contactNormal = hit.normal;

        // --- Suspension ---
        ComputeSuspension(carRb, dt);

        // --- Lateral grip ---
        ComputeLateralForce();

        // --- Longitudinal friction ---
        ComputeLongitudinalForce(carRb);

        // --- Motor force ---
        ComputeMotorForce();

        // --- Apply composite force at contact point ---
        Vector3 totalForce = yForce + xForce + zForce + motorForce;
        carRb.AddForceAtPosition(totalForce, contactPoint);

        // --- Visual wheel update ---
        // Rotate around the pivot's right axis (the actual wheel axle), in world space.
        // The visual cylinder is rotated 90° around Z so its local axes don't match the axle.
        // Unity forward rolling = positive angle around +X (top moves forward).
        // Sign: Godot uses -fSpeed (because forward=-Z); Unity uses +fSpeed (forward=+Z).
        float spinAngle = fSpeed / wheelRadius * dt * Mathf.Rad2Deg;
        if (wheelVisual != null)
        {
            wheelVisual.localPosition = new Vector3(0f, -springLen, 0f);
            wheelVisual.Rotate(transform.right, spinAngle, Space.World);
        }
        if (hubVisual != null)
        {
            hubVisual.localPosition = new Vector3(0f, -springLen, 0f);
            hubVisual.Rotate(transform.right, spinAngle, Space.World);
        }

        // RPM for air physics gyro
        wheelRpm = (fSpeed / wheelRadius) * 60f / (2f * Mathf.PI);

        // Debug visualization
        if (showDebug)
            DrawDebug();
    }

    void ComputeSuspension(Rigidbody carRb, float dt)
    {
        springLen = Vector3.Distance(transform.position, contactPoint) - wheelRadius;
        springLen = Mathf.Max(springLen, minSpringLen);

        float offset = restDistance - springLen;
        springForce = springStrength * offset;
        float dampingForce = springDamping * (prevSpringLen - springLen) / dt;
        float rawForce = springForce + dampingForce;
        suspensionForce = Mathf.Max(rawForce, 0f);
        prevSpringLen = springLen;

        yForce = contactNormal * suspensionForce;

        // Tire velocity at contact point
        tireVelocity = carRb.GetPointVelocity(contactPoint);
        speed = tireVelocity.magnitude;
        fSpeed = Vector3.Dot(transform.forward, tireVelocity);

        gripLoad = Mathf.Clamp(springForce, 0f, maxSpringForce);

        lastSpringLen = springLen;
        lastGripLoad = gripLoad;
    }

    void ComputeLateralForce()
    {
        Vector3 steerSideDir = transform.right;
        float lateralVel = Vector3.Dot(steerSideDir, tireVelocity);

        if (speed < 0.1f || gripCurve == null)
        {
            gripFactor = 0f;
            slipRatio = 0f;
            xForce = Vector3.zero;
            return;
        }

        slipRatio = Mathf.Clamp01(Mathf.Abs(lateralVel) / speed);
        gripFactor = gripCurve.Evaluate(slipRatio);

        xForce = -steerSideDir * lateralVel * gripFactor * gripCoeff * gripLoad;
    }

    void ComputeLongitudinalForce(Rigidbody carRb)
    {
        float effectiveZTraction = isBraking ? zBrakeTraction : zTraction;

        // Static friction: hold on ramps when stopped
        if (Mathf.Abs(fSpeed) < 0.5f && cachedCar != null && cachedCar.currentEngineForce == 0f)
            effectiveZTraction = 5.0f;

        // Longitudinal friction opposes forward motion along the CAR's axis
        // Godot: _car.global_basis.z (backward) * fSpeed → Unity: -car.forward (backward) * fSpeed
        zForce = -carRb.transform.forward * fSpeed * effectiveZTraction * gripCoeff * gripLoad;

        // Ramp sliding fix: cancel the spring's horizontal component when stopped
        if (Mathf.Abs(fSpeed) < 0.5f)
        {
            xForce.x -= contactNormal.x * suspensionForce;
            zForce.z -= contactNormal.z * suspensionForce;
        }
    }

    void ComputeMotorForce()
    {
        motorForce = Vector3.zero;
        if (isMotor && motorForceShare != 0f)
        {
            // Motor drives along wheel's forward axis
            // Godot: -global_basis.z (forward) → Unity: transform.forward
            motorForce = transform.forward * motorForceShare;
        }
    }

    void DrawDebug()
    {
        if (!isOnGround) return;

        // White — suspension travel
        Debug.DrawLine(transform.position, contactPoint, Color.white);

        // Yellow — suspension force
        if (yForce.sqrMagnitude > 0.0001f)
            Debug.DrawRay(contactPoint, yForce * DBG_SCALE, Color.yellow);

        // Red — lateral force
        if (xForce.sqrMagnitude > 0.0001f)
            Debug.DrawRay(contactPoint, xForce * DBG_SCALE, Color.red);

        // Green — longitudinal friction
        if (zForce.sqrMagnitude > 0.0001f)
            Debug.DrawRay(contactPoint, zForce * DBG_SCALE, Color.green);

        // Cyan — motor force
        if (motorForce.sqrMagnitude > 0.0001f)
            Debug.DrawRay(contactPoint, motorForce * DBG_SCALE, Color.cyan);
    }
}
