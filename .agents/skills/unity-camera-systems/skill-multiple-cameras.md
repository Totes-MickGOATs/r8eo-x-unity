# Multiple Cameras

> Part of the `unity-camera-systems` skill. See [SKILL.md](SKILL.md) for the overview.

## Multiple Cameras

### Render Order (Built-in Pipeline)

```csharp
// Lower depth renders first, higher renders on top
mainCamera.depth = 0;
uiCamera.depth = 1;       // Renders UI on top
minimapCamera.depth = 2;  // Minimap overlay
```

Set each camera's **Clear Flags** and **Culling Mask** appropriately:
- Main camera: Skybox, everything except UI layer
- UI camera: Depth Only, UI layer only
- Minimap camera: Solid Color, renders to RenderTexture

### Camera Stacking (URP)

```
Main Camera (Base):
  Render Type: Base
  Stack: [OverlayCamera1, OverlayCamera2]

Overlay Camera 1 (UI):
  Render Type: Overlay
  Culling Mask: UI

Overlay Camera 2 (Effects):
  Render Type: Overlay
  Culling Mask: Effects
```

URP camera stacking is the proper way to layer cameras. Avoid multiple Base cameras.

## Common Patterns

### Smooth Camera Transition Helper

```csharp
public static class CameraHelper
{
    public static void SwitchTo(CinemachineCamera cam, int priority = 10)
    {
        // Lower all other cameras, raise this one
        foreach (var vc in CinemachineCore.GetAllCameras())
            vc.Priority = 0;
        cam.Priority = priority;
    }
}
```

### Look-At Target Offset for Aiming

```csharp
// Shift the camera's LookAt target toward crosshair during aiming
CinemachineRotationComposer composer = vcam.GetComponent<CinemachineRotationComposer>();

void Update()
{
    float aimInfluence = isAiming ? 1f : 0f;
    composer.ScreenPosition = Vector2.Lerp(
        new Vector2(0.5f, 0.5f),     // Centered (hip fire)
        new Vector2(0.35f, 0.55f),   // Over-shoulder (ADS)
        aimInfluence);
}
```

### Cinemachine Camera Priorities by Context

| Context | Camera | Priority |
|---------|--------|----------|
| Default gameplay | FollowCam | 10 |
| Aiming/ADS | AimCam | 15 |
| Cutscene | CutsceneCam | 20 |
| Death/spectate | SpectatorCam | 25 |
| Menu/pause | MenuCam | 5 |

Higher priority = becomes live. When a camera is deactivated or lowered, the Brain blends back to the next highest.
