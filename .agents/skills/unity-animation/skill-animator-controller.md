# Animator Controller

> Part of the `unity-animation` skill. See [SKILL.md](SKILL.md) for the overview.

## Animator Controller

The Animator Controller is a state machine that manages animation states and transitions.

### States

Each state plays an animation clip (or Blend Tree). Special states:
- **Entry**: Starting point, leads to default state
- **Any State**: Transitions from here fire regardless of current state
- **Exit**: Returns to the parent state machine (for sub-state machines)

### Parameters

Drive transitions and Blend Trees:

| Type | Use Case | Code Access |
|------|----------|-------------|
| Bool | Toggle states (isGrounded, isDead) | `animator.SetBool("isGrounded", true)` |
| Int | Indexed states (weaponType) | `animator.SetInteger("weaponType", 2)` |
| Float | Blended values (speed, direction) | `animator.SetFloat("speed", velocity.magnitude)` |
| Trigger | One-shot events (attack, jump) | `animator.SetTrigger("attack")` |

```csharp
// Driving the animator each frame
void Update()
{
    float speed = _rigidbody.velocity.magnitude;
    _animator.SetFloat("Speed", speed);
    _animator.SetBool("IsGrounded", _characterController.isGrounded);
    _animator.SetFloat("Direction", Input.GetAxis("Horizontal"));
}
```

### Transitions

| Setting | Purpose | Typical Value |
|---------|---------|---------------|
| Has Exit Time | Wait for clip to finish before transitioning | true for attacks, false for movement |
| Exit Time | Normalized time when transition can begin | 0.85 (near end of clip) |
| Transition Duration | Blend time (normalized or fixed) | 0.1 - 0.25s |
| Transition Offset | Start point in destination clip | 0 |
| Conditions | Parameter checks required | Speed > 0.1, IsGrounded == true |

**Best Practice:** Use **Fixed Duration** (seconds) rather than normalized for consistent feel regardless of clip length.

## Blend Trees

Blend Trees smoothly interpolate between multiple clips based on parameters.

### 1D Blend Tree

Blend by one parameter (e.g., speed):

```
Blend Tree (Speed):
  0.0 -> Idle
  0.5 -> Walk
  1.0 -> Run
  2.0 -> Sprint
```

### 2D Blend Trees

| Type | When to Use |
|------|------------|
| 2D Simple Directional | Each motion represents a unique direction (N, S, E, W) |
| 2D Freeform Directional | Multiple motions per direction at different speeds |
| 2D Freeform Cartesian | Parameters represent independent axes (speed + turn) |

```
2D Freeform Directional (SpeedX, SpeedY):
  (0, 0)   -> Idle
  (0, 1)   -> Walk Forward
  (0, 2)   -> Run Forward
  (0, -1)  -> Walk Backward
  (-1, 0)  -> Strafe Left
  (1, 0)   -> Strafe Right
```

```csharp
// Driving a 2D Blend Tree
Vector3 localVelocity = transform.InverseTransformDirection(_rigidbody.velocity);
_animator.SetFloat("VelocityX", localVelocity.x, 0.1f, Time.deltaTime);
_animator.SetFloat("VelocityZ", localVelocity.z, 0.1f, Time.deltaTime);
```

The `SetFloat` overload with damping (`dampTime`, `deltaTime`) provides smooth blending.

## Animation Layers

Layers allow multiple animations to play simultaneously on different body parts.

### Layer Setup

| Property | Purpose |
|----------|---------|
| Weight | Influence of this layer (0-1) |
| Blending | Override (replace) or Additive (add on top) |
| Avatar Mask | Which bones this layer affects |

**Common Layer Setup:**

| Layer | Blending | Mask | Purpose |
|-------|----------|------|---------|
| Base Layer | - | Full body | Locomotion |
| Upper Body | Override | Spine + Arms + Head | Aiming, holding weapon |
| Additive | Additive | Full body | Hit reactions, breathing |
| Face | Override | Head only | Facial expressions |

### Avatar Masks

Create via **Assets > Create > Avatar Mask**. Enable/disable specific bones:

```
Upper Body Mask:
  [x] Spine, Chest, Upper Chest
  [x] Left/Right Shoulder, Arm, Forearm, Hand
  [x] Head, Neck
  [ ] Hips, Left/Right Leg (disabled -- base layer handles these)
```

```csharp
// Runtime layer weight control
_animator.SetLayerWeight(1, isAiming ? 1f : 0f);  // Fade upper body layer
```

## Inverse Kinematics (IK)

IK adjusts bones to reach target positions, overriding the animation.

### Setup Requirements

1. Humanoid rig on the model
2. **IK Pass** enabled on the Animator layer
3. Override `OnAnimatorIK` in a script on the same GameObject

```csharp
public class FootIK : MonoBehaviour
{
    [SerializeField] float _footOffset = 0.1f;
    [SerializeField] LayerMask _groundLayer;

    Animator _animator;

    void Awake() => _animator = GetComponent<Animator>();

    void OnAnimatorIK(int layerIndex)
    {
        // Left foot placement
        PlaceFoot(AvatarIKGoal.LeftFoot);
        PlaceFoot(AvatarIKGoal.RightFoot);
    }

    void PlaceFoot(AvatarIKGoal foot)
    {
        _animator.SetIKPositionWeight(foot, 1f);
        _animator.SetIKRotationWeight(foot, 1f);

        Vector3 footPos = _animator.GetIKPosition(foot);
        if (Physics.Raycast(footPos + Vector3.up * 0.5f, Vector3.down,
            out RaycastHit hit, 1f, _groundLayer))
        {
            _animator.SetIKPosition(foot, hit.point + Vector3.up * _footOffset);
            _animator.SetIKRotation(foot,
                Quaternion.LookRotation(transform.forward, hit.normal));
        }
    }
}
```

### Look-At IK

```csharp
void OnAnimatorIK(int layerIndex)
{
    if (_lookTarget != null)
    {
        _animator.SetLookAtWeight(1f, 0.3f, 0.6f, 1f, 0.5f);
        // (weight, bodyWeight, headWeight, eyesWeight, clampWeight)
        _animator.SetLookAtPosition(_lookTarget.position);
    }
}
```

## Root Motion

Root Motion transfers the animation's root bone movement to the GameObject's transform.

### Setup

1. Check **Apply Root Motion** on the Animator component
2. Ensure clips have root motion baked (not "Bake Into Pose")

### OnAnimatorMove Override

Override default root motion application for custom control:

```csharp
public class RootMotionController : MonoBehaviour
{
    Animator _animator;
    CharacterController _controller;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
    }

    void OnAnimatorMove()
    {
        // Apply root motion through CharacterController instead of Transform
        Vector3 velocity = _animator.deltaPosition / Time.deltaTime;
        velocity.y = _currentVerticalSpeed; // Preserve gravity
        _controller.Move(velocity * Time.deltaTime);
        transform.rotation *= _animator.deltaRotation;
    }
}
```

