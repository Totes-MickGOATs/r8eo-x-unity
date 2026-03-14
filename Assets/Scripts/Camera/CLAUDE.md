# Camera System

Multi-mode camera system for following the RC buggy.

## Files

| File | Class | Role |
|------|-------|------|
| `CameraController.cs` | `CameraController` | Main camera controller with Chase, Orbit, FPV, and Trackside modes |
| `CameraMode.cs` | `CameraMode` | Enum defining available camera modes |
| `TracksideAnchor.cs` | `TracksideAnchor` | Scene marker for trackside camera positions |
| `ChaseCamera.cs` | `ChaseCamera` | Legacy chase-only camera (deprecated, kept for scene migration) |
| `R8EOX.Camera.asmdef` | — | Assembly definition for the camera system |

## Camera Modes

| Mode | Behavior |
|------|----------|
| **Chase** | Follows behind the car with smooth position interpolation |
| **Orbit** | Player orbits around the car using right mouse drag or right stick |
| **FPV** | Fixed to the car body with configurable local offset, simulating an onboard FPV camera |
| **Trackside** | Static position (nearest TracksideAnchor or fallback), tracks car with rotation only |

## Usage

- Attach `CameraController` to the main camera
- Set the `Target` to the RC buggy root transform
- Press **C** (configurable) to cycle modes
- Place `TracksideAnchor` objects in the scene for trackside camera positions

## Relevant Skills

- **`unity-camera-systems`** — Camera modes, follow logic, and cinematic patterns
