# Camera/Modes/

Concrete camera mode implementations for the R8EO-X camera system. Each mode
implements `ICameraMode` and provides a distinct viewpoint behavior.

## Files

| File | Purpose |
|------|---------|
| `ChaseCameraMode.cs` | Third-person chase camera that follows behind and above the vehicle |
| `FpvCameraMode.cs` | First-person view mounted to the vehicle body |
| `OrbitCameraMode.cs` | Orbit camera allowing mouse/stick rotation around the vehicle |
| `TracksideCameraMode.cs` | Fixed trackside spectator camera that cuts to the nearest anchor |

## Relevant Skills

- **`unity-camera-systems`** — Camera modes, follow logic, and cinematic patterns
