# Common Gotchas

> Part of the `unity-physics-3d` skill. See [SKILL.md](SKILL.md) for the overview.

## Common Gotchas

### Moving Colliders Without Rigidbody

Moving a static collider forces PhysX to rebuild its spatial tree. Add a `Rigidbody` with `isKinematic = true` instead:

```csharp
// BAD — moving static collider every frame
transform.position += Vector3.right * Time.deltaTime;

// GOOD — kinematic rigidbody
rb.MovePosition(rb.position + Vector3.right * Time.fixedDeltaTime);
```

### Scale on Colliders

Non-uniform scale on MeshColliders is expensive. Bake scale into the mesh or use primitive colliders.

### MeshCollider + Rigidbody

A `MeshCollider` on a dynamic Rigidbody **must** be convex. Non-convex MeshColliders are static only.

### Rigidbody on Parent Only

Place the Rigidbody on the root. Child colliders automatically become part of the compound collider. Do NOT put Rigidbodies on children unless you want separate physics bodies.

### Collision Detection Modes

```csharp
// Discrete — default, can tunnel through thin objects at high speed
rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

// Continuous — prevents tunneling for this body vs static colliders
rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

// ContinuousDynamic — prevents tunneling vs static AND dynamic colliders (expensive)
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

// ContinuousSpeculative — cheaper CCD, works with kinematic too
rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
```

## Vehicle Physics: WheelCollider

Unity's built-in vehicle physics uses `WheelCollider` — a raycast-based suspension + tire model.

### Basic Setup

```csharp
public class SimpleVehicle : MonoBehaviour
{
    [SerializeField] private WheelCollider[] driveWheels;
    [SerializeField] private WheelCollider[] steerWheels;
    [SerializeField] private float maxMotorTorque = 400f;
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float maxBrakeTorque = 1000f;

    void FixedUpdate()
    {
        float throttle = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");
        bool brake = Input.GetKey(KeyCode.Space);

        foreach (var wheel in steerWheels)
            wheel.steerAngle = steer * maxSteerAngle;

        foreach (var wheel in driveWheels)
        {
            wheel.motorTorque = throttle * maxMotorTorque;
            wheel.brakeTorque = brake ? maxBrakeTorque : 0f;
        }
    }
}
```

### WheelCollider Properties

| Property | Purpose |
|----------|---------|
| `radius` | Wheel radius in meters |
| `suspensionDistance` | Max suspension travel |
| `suspensionSpring` | Spring/damper/target position |
| `forwardFriction` | Longitudinal grip curve |
| `sidewaysFriction` | Lateral grip curve |
| `mass` | Wheel mass (affects suspension) |

### Syncing Visual Wheels

```csharp
void UpdateWheelVisual(WheelCollider collider, Transform visual)
{
    collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
    visual.position = pos;
    visual.rotation = rot;
}
```

### Suspension Tuning

```csharp
JointSpring spring = wheel.suspensionSpring;
spring.spring = 35000f;        // Spring force (higher = stiffer)
spring.damper = 4500f;         // Damping (higher = less oscillation)
spring.targetPosition = 0.5f;  // 0 = fully extended, 1 = fully compressed
wheel.suspensionSpring = spring;
wheel.suspensionDistance = 0.3f; // Travel distance in meters
```

### Anti-Roll Bar (Stabilizer)

```csharp
void ApplyAntiRollBar(WheelCollider leftWheel, WheelCollider rightWheel, float antiRollForce)
{
    float travelL = 1f, travelR = 1f;

    if (leftWheel.GetGroundHit(out WheelHit hitL))
        travelL = (-leftWheel.transform.InverseTransformPoint(hitL.point).y
                   - leftWheel.radius) / leftWheel.suspensionDistance;

    if (rightWheel.GetGroundHit(out WheelHit hitR))
        travelR = (-rightWheel.transform.InverseTransformPoint(hitR.point).y
                   - rightWheel.radius) / rightWheel.suspensionDistance;

    float antiRoll = (travelL - travelR) * antiRollForce;

    if (leftWheel.isGrounded)
        rb.AddForceAtPosition(leftWheel.transform.up * -antiRoll, leftWheel.transform.position);
    if (rightWheel.isGrounded)
        rb.AddForceAtPosition(rightWheel.transform.up * antiRoll, rightWheel.transform.position);
}
```

