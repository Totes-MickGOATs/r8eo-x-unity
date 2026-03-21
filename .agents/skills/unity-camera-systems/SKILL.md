---
name: unity-camera-systems
description: Unity Camera Systems
---


# Unity Camera Systems

Use this skill when setting up cameras, configuring Cinemachine virtual cameras, implementing camera blending, or building state-driven camera rigs in Unity.

## Package Setup

```json
// Packages/manifest.json
"com.unity.cinemachine": "3.1.2"
```

**Version Note:**
- Cinemachine 3.x: Unity 2023.1+ -- renamed components (CinemachineCamera, CinemachineFollow, etc.)
- Cinemachine 2.x: Unity 2019-2022 -- original names (CinemachineVirtualCamera, CinemachineTransposer, etc.)
- **Unity 6.1+:** Cinemachine 2.x reaches end of support. Use `using Unity.Cinemachine` (not `using Cinemachine`). Key renames: `CinemachineVirtualCamera` -> `CinemachineCamera`, `CinemachineThirdPersonFollow` -> `ThirdPersonFollow`, `CinemachineBlendListCamera` -> `CinemachineSequencerCamera`.

This guide uses Cinemachine 3.x naming. The 2.x equivalents are noted where they differ significantly.

## Core Architecture

Cinemachine separates camera logic from the actual Unity Camera:

```
Unity Camera (with CinemachineBrain)
    |
    +-- evaluates all active CinemachineCameras
    |
    +-- picks the highest-priority camera
    |
    +-- blends between cameras on transitions
```

**CinemachineBrain** (on the main Camera): orchestrates blending, picks the active virtual camera.
**CinemachineCamera** (on separate GameObjects): defines how the camera should behave. Only one is "live" at a time.

```csharp
// Basic setup hierarchy:
// MainCamera (Camera + CinemachineBrain)
// FollowCam (CinemachineCamera + CinemachineFollow + CinemachineRotationComposer)
// OverviewCam (CinemachineCamera + CinemachinePositionComposer)
```

## State-Driven Camera

Tie cameras to Animator states for automatic context switching:

```
CinemachineStateDrivenCamera:
  Animated Target: [Character Animator]
  Default Blend: Ease In Out, 0.5s

  State-Camera Map:
    Idle       -> IdleCam (close, low angle)
    Running    -> RunCam (pulled back, slight lag)
    Jumping    -> AirCam (wider FOV)
    Combat     -> CombatCam (over-shoulder)
```

Each child camera is a full CinemachineCamera with its own body/aim. The state machine selects which is live.

## FreeLook Camera (3-Rig Orbit)

Classic third-person orbit with three height rings:

```
CinemachineCamera + CinemachineOrbitalFollow + CinemachineRotationComposer:

  Orbits:
    Top Rig:    Height = 4.5,  Radius = 1.75
    Middle Rig: Height = 2.5,  Radius = 3.0
    Bottom Rig: Height = 0.4,  Radius = 1.3

  Spline Curvature: 0.5
```

The player's vertical input blends between rigs. Horizontal input orbits around the target.

## Timeline Integration

Cinemachine integrates with Timeline for cinematic sequences:

1. Add a **Cinemachine Track** to the Timeline
2. Set the track's binding to the CinemachineBrain's GameObject
3. Drag CinemachineCamera clips onto the track
4. Overlapping clips create automatic blends

```csharp
// Triggering a cutscene
[SerializeField] PlayableDirector _cutsceneDirector;
[SerializeField] CinemachineCamera _gameplayCam;

void StartCutscene()
{
    _gameplayCam.Priority = 0; // Let timeline cameras take over
    _cutsceneDirector.Play();
    _cutsceneDirector.stopped += OnCutsceneEnd;
}

void OnCutsceneEnd(PlayableDirector director)
{
    _gameplayCam.Priority = 10;
    director.stopped -= OnCutsceneEnd;
}
```

## Screen-to-World Conversion

Essential for mouse interaction, raycasting, and targeting:

```csharp
// Raycast from screen point
Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
if (Physics.Raycast(ray, out RaycastHit hit, 100f))
{
    Debug.Log($"Hit: {hit.collider.name} at {hit.point}");
}

// Screen to world position at a specific distance
Vector3 worldPos = Camera.main.ScreenToWorldPoint(
    new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));

// World to screen position (for UI tracking)
Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
bool isOnScreen = screenPos.z > 0
    && screenPos.x > 0 && screenPos.x < Screen.width
    && screenPos.y > 0 && screenPos.y < Screen.height;
```



## Topic Pages

- [CinemachineCamera Setup](skill-cinemachinecamera-setup.md)
- [Camera Shake and Impulse](skill-camera-shake-and-impulse.md)
- [Multiple Cameras](skill-multiple-cameras.md)

