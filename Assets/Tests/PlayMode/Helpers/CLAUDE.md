# Assets/Tests/PlayMode/Helpers/

Shared test utilities and scene setup factories for PlayMode tests.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `ConformanceSceneSetup.cs` | `ConformanceSceneSetup` | Static factory: creates flat ground + fully-wired RC car; shared spawn and drive-input helpers |
| `VehicleIntegrationHelper.cs` | `VehicleIntegrationHelper` | Instance-based setup for VehicleIntegration tests; provides static `WaitPhysicsFrames` |

## Usage

```csharp
// Spawn and get all references at once:
ConformanceSceneSetup.SpawnTestVehicle(
    new Vector3(0f, 0.5f, 0f),
    out var ground, out var car, out var carRb, out var rcCar, out var wheels);

// Or create separately:
var ground = ConformanceSceneSetup.CreateGround();
var car = ConformanceSceneSetup.CreateTestVehicle(new Vector3(0f, 0.5f, 0f));

// Drive input helpers:
ConformanceSceneSetup.SetMotorForce(wheels, 26f);
ConformanceSceneSetup.SetBraking(wheels, true);
ConformanceSceneSetup.ClearDriveInputs(wheels);

// Shared physics frame waiting:
yield return VehicleIntegrationHelper.WaitPhysicsFrames(60);

// ... run test ...
Object.DestroyImmediate(car);
Object.DestroyImmediate(ground);
```

## Constants

`ConformanceSceneSetup` exposes physics reference constants matching `adr-001-physics-model.md`:
- `k_Mass` (15 kg), `k_WheelRadiusFront` (0.425 m), `k_WheelRadiusRear` (0.420 m), `k_Wheelbase` (13.6 m)
- `k_Gravity` (9.81 m/s^2), `k_GripCoeff` (0.7)
- `k_CarLayer` (8), `k_GroundLayer` (9)

## Relevant Skills

- **`unity-testing-patterns`** -- Test fixture setup patterns
- **`clean-room-qa`** -- Black-box test environment design
