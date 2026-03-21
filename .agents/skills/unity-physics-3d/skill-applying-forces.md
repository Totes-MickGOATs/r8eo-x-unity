# Applying Forces

> Part of the `unity-physics-3d` skill. See [SKILL.md](SKILL.md) for the overview.

## Applying Forces

### ForceMode Options

```csharp
// Force — continuous force, affected by mass. Use for engines, wind.
rb.AddForce(direction * 100f, ForceMode.Force);

// Impulse — instant kick, affected by mass. Use for explosions, jumps.
rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);

// Acceleration — continuous, ignores mass. Use for uniform gravity zones.
rb.AddForce(direction * 10f, ForceMode.Acceleration);

// VelocityChange — instant, ignores mass. Use for dash, teleport-like movement.
rb.AddForce(direction * 5f, ForceMode.VelocityChange);
```

### Torque

```csharp
rb.AddTorque(Vector3.up * turnForce, ForceMode.Force);        // Spin around Y
rb.AddRelativeTorque(Vector3.up * turnForce, ForceMode.Force); // Relative to local axes
```

### Direct Velocity (Use Sparingly)

```csharp
// Override velocity directly — breaks physics realism but useful for snappy movement
rb.velocity = new Vector3(rb.velocity.x, jumpSpeed, rb.velocity.z);
```

## Joints

### Joint Types

| Joint | Use Case | Example |
|-------|----------|---------|
| `FixedJoint` | Weld two bodies together | Attached cargo, compound objects |
| `HingeJoint` | Single-axis rotation | Doors, wheels, pendulums |
| `SpringJoint` | Elastic connection | Bungee, suspension, tethered objects |
| `ConfigurableJoint` | Full control over all 6 DOF | Vehicle suspension, ragdoll, robotic arms |
| `CharacterJoint` | Humanoid joint limits | Ragdoll limbs |

### Hinge Joint Example

```csharp
var hinge = gameObject.AddComponent<HingeJoint>();
hinge.connectedBody = frameRigidbody;
hinge.axis = Vector3.up;                  // Rotation axis
hinge.useLimits = true;
hinge.limits = new JointLimits { min = -90f, max = 90f };
hinge.useMotor = true;
hinge.motor = new JointMotor { targetVelocity = 100f, force = 50f };
```

### Configurable Joint (Vehicle Suspension)

```csharp
var joint = gameObject.AddComponent<ConfigurableJoint>();
joint.connectedBody = chassisRb;

// Lock X and Z, free Y for suspension travel
joint.xMotion = ConfigurableJointMotion.Locked;
joint.yMotion = ConfigurableJointMotion.Limited;
joint.zMotion = ConfigurableJointMotion.Locked;

// Spring on Y axis
var drive = new JointDrive
{
    positionSpring = 5000f,
    positionDamper = 200f,
    maximumForce = float.MaxValue
};
joint.yDrive = drive;
```

## Raycasting

### Basic Raycast

```csharp
if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 100f))
{
    Debug.Log($"Hit: {hit.collider.name}, Distance: {hit.distance}, Normal: {hit.normal}");
    Debug.DrawLine(transform.position, hit.point, Color.red, 1f);
}
```

### Shape Casts

```csharp
// SphereCast — thick raycast, good for ground checks
Physics.SphereCast(origin, radius, direction, out hit, maxDistance, layerMask);

// BoxCast — rectangular sweep
Physics.BoxCast(center, halfExtents, direction, out hit, orientation, maxDistance, layerMask);

// CapsuleCast — capsule sweep, good for character movement
Physics.CapsuleCast(point1, point2, radius, direction, out hit, maxDistance, layerMask);
```

### RaycastAll and RaycastNonAlloc

```csharp
// Returns ALL hits (allocates array — avoid in hot paths)
RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);

// Non-allocating version (pre-allocate buffer)
RaycastHit[] buffer = new RaycastHit[16];
int count = Physics.RaycastNonAlloc(origin, direction, buffer, maxDistance, layerMask);
for (int i = 0; i < count; i++)
{
    // Process buffer[i]
}
```

### Overlap Tests

```csharp
// Check what's inside a sphere (explosions, area detection)
Collider[] results = new Collider[32];
int count = Physics.OverlapSphereNonAlloc(center, radius, results, layerMask);

// Box overlap
Physics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask);
```

## Triggers vs Collisions

### Triggers (isTrigger = true)

Pass-through volumes. Use for: pickup zones, damage areas, checkpoints.

```csharp
// At least one object needs a Rigidbody (can be kinematic)
void OnTriggerEnter(Collider other)  { /* First frame of overlap */ }
void OnTriggerStay(Collider other)   { /* Every fixed frame while overlapping */ }
void OnTriggerExit(Collider other)   { /* First frame after separation */ }
```

### Collisions (isTrigger = false)

Physical contact. Use for: ground detection, impact damage, bounce.

```csharp
void OnCollisionEnter(Collision collision)
{
    float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;
    ContactPoint contact = collision.GetContact(0);
    Vector3 hitPoint = contact.point;
    Vector3 hitNormal = contact.normal;
}
void OnCollisionStay(Collision collision)  { /* Ongoing contact */ }
void OnCollisionExit(Collision collision)  { /* Separation */ }
```

### Collision Matrix Requirements

| | Static Collider | Kinematic Rigidbody | Dynamic Rigidbody |
|---|---|---|---|
| **Static Collider** | No | Trigger only | Yes |
| **Kinematic Rigidbody** | Trigger only | Trigger only | Yes |
| **Dynamic Rigidbody** | Yes | Yes | Yes |

