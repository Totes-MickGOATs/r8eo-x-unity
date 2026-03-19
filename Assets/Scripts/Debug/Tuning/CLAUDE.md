# Debug/Tuning/

Runtime tuning overlay sections. Each `TuningSection` subclass controls a group
of related vehicle sliders shown in the in-game tuning panel.

## Files

| File | Purpose |
|------|---------|
| `TuningSection.cs` | Abstract base class defining the section lifecycle and slider registration API |
| `SliderDefinition.cs` | Data container describing a single slider (label, min, max, getter, setter) |
| `SliderRenderer.cs` | Renders a list of `SliderDefinition` instances as IMGUI sliders |
| `CrashTuningSection.cs` | Sliders for tumble engage/full angles, bounce, and friction |
| `DrivetrainTuningSection.cs` | Sliders for differential bias and torque split |
| `MotorTuningSection.cs` | Sliders for engine force, max speed, brake, reverse, and coast drag |
| `SteeringTuningSection.cs` | Sliders for steering max angle, speed, speed limit, and high-speed factor |
| `SuspensionTuningSection.cs` | Sliders for front and rear spring strength and damping |
| `VehicleTuningSection.cs` | Top-level section that composes motor, suspension, steering, and crash sub-sections |

## Relevant Skills

- **`unity-physics-tuning`** — Physics parameter tuning workflow, slider-driven live adjustment patterns
