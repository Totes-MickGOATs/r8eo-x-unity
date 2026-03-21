# CinemachineCamera Setup

> Part of the `unity-camera-systems` skill. See [SKILL.md](SKILL.md) for the overview.

## CinemachineCamera Setup

Each CinemachineCamera needs at minimum a **body** behavior (how it moves) and optionally an **aim** behavior (where it looks).

### Inspector Properties

| Property | Purpose |
|----------|---------|
| Follow | Target transform the camera follows |
| LookAt | Target transform the camera aims at |
| Lens | FOV, near/far clip, orthographic size |
| Priority | Higher priority cameras take precedence |
| Standby Update | How to update when not live (Never, Always, Round Robin) |

### Enabling/Disabling

```csharp
// Switch cameras by adjusting priority or enabling/disabling
[SerializeField] CinemachineCamera _followCam;
[SerializeField] CinemachineCamera _overviewCam;

public void SwitchToOverview()
{
    _followCam.Priority = 0;
    _overviewCam.Priority = 10;
}

// Or simply:
_followCam.enabled = false;
_overviewCam.enabled = true;
```

## Body Algorithms (Positioning)

Body components control how the camera positions itself relative to the Follow target.

### CinemachineFollow (Transposer in 2.x)

Maintains a fixed offset from the target in the target's local space.

```
CinemachineFollow:
  Follow Offset: (0, 5, -10)    // Behind and above
  Damping: (1, 1, 1)             // Smooth follow delay
  Binding Mode: Lock To Target With World Up
```

**Binding Modes:**

| Mode | Behavior | Use Case |
|------|----------|----------|
| Lock To Target With World Up | Offset rotates with target yaw only | 3rd-person character |
| Lock To Target | Full rotation tracking | Vehicle chase cam |
| World Space | Offset is absolute | Fixed-offset overview |
| Lazy Follow | Only follows when target moves far enough | Relaxed exploration |

### CinemachinePositionComposer (Framing Transposer in 2.x)

Keeps the target within a screen-space frame. Ideal for 2.5D sidescrollers and third-person.

```
CinemachinePositionComposer:
  Tracked Object Offset: (0, 1.5, 0)   // Aim at character head
  Lookahead Time: 0.3                    // Anticipate movement
  Screen Position: (0.5, 0.5)           // Center of screen
  Dead Zone: (0.1, 0.1)                 // No movement in this region
  Soft Zone: (0.8, 0.8)                 // Gradual correction zone
  Damping: 2                            // Smoothing strength
  Camera Distance: 10
```

**Dead Zone:** Target can move here without camera reacting.
**Soft Zone:** Camera gradually corrects to keep target here.
**Hard Limit:** Outside this, camera moves immediately (no damping).

### CinemachineOrbitalFollow (Orbital Transposer in 2.x)

Orbits around the target, driven by input axes.

```csharp
// Orbital follow with player-controlled rotation
CinemachineOrbitalFollow orbital = vcam.GetComponent<CinemachineOrbitalFollow>();

// Input is typically wired via CinemachineInputAxisController component
// or driven manually:
public class CameraOrbitInput : MonoBehaviour, IInputAxisOwner
{
    public void GetInputAxis(int axis, out float value)
    {
        // axis 0 = horizontal, axis 1 = vertical
        value = axis == 0 ? Input.GetAxis("Mouse X") : Input.GetAxis("Mouse Y");
    }
}
```

## Aim Algorithms (Rotation)

### CinemachineRotationComposer (Composer in 2.x)

Rotates the camera to keep the LookAt target in a screen-space zone.

```
CinemachineRotationComposer:
  Tracked Object Offset: (0, 0, 0)
  Lookahead Time: 0
  Screen Position: (0.5, 0.6)    // Slightly above center
  Dead Zone: (0.1, 0.05)
  Soft Zone: (0.5, 0.5)
  Damping: (0.5, 0.5)
```

### CinemachineGroupFraming (Group Composer in 2.x)

Frames multiple targets automatically. Use with a CinemachineTargetGroup as LookAt.

```csharp
// CinemachineTargetGroup on a separate GameObject
// Add targets with weight and radius:
[SerializeField] CinemachineTargetGroup _targetGroup;

void AddPlayer(Transform player)
{
    _targetGroup.AddMember(player, 1f, 2f); // weight, radius
}

void RemovePlayer(Transform player)
{
    _targetGroup.RemoveMember(player);
}
```

```
CinemachineGroupFraming:
  Framing Size: 0.8              // How much screen to fill
  Framing Mode: Horizontal And Vertical
  Damping: 2
  Size Adjustment: Dolly Then Zoom
  Lateral Adjustment: 0.5
```

### Hard Look At

Simply points at the LookAt target with zero damping. Good for locked-on targeting.

## CinemachineBrain -- Blending

The Brain controls how transitions between cameras look.

### Default Blend

```
CinemachineBrain:
  Default Blend:
    Style: Ease In Out
    Duration: 2.0
  Update Method: Smart Update     // Handles mixed Update/FixedUpdate targets
  Blend Update Method: Late Update
```

### Custom Blends

Create a **CinemachineBlenderSettings** asset to define per-camera-pair blends:

```
From            To              Style           Duration
FollowCam       OverviewCam     Cut             0
OverviewCam     FollowCam       Ease In Out     1.5
*               CutsceneCam     Ease In         0.5
```

Assign this asset to the CinemachineBrain's Custom Blends field.

### Blend Styles

| Style | Behavior |
|-------|----------|
| Cut | Instant switch |
| Ease In Out | Smooth acceleration and deceleration |
| Ease In | Smooth start, abrupt end |
| Ease Out | Abrupt start, smooth end |
| Hard In | Fast start, slow end |
| Hard Out | Slow start, fast end |
| Linear | Constant speed |
| Custom | AnimationCurve |

