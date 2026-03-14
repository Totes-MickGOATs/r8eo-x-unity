# Assets/Scripts/Vehicle/Config/

ScriptableObject configurations for vehicle tuning. Create assets via Assets → Create → R8EOX.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `MotorPresetConfig.cs` | `MotorPresetConfig` | Motor preset: engine force, speed, braking, throttle response |
| `SuspensionConfig.cs` | `SuspensionConfig` | Suspension: spring stiffness, damping, travel distances |
| `TractionConfig.cs` | `TractionConfig` | Traction: grip coefficient, grip curve, friction factors |

## Usage

1. Create asset: Assets → Create → R8EOX → Motor Preset / Suspension Config / Traction Config
2. Assign to RCCar component in inspector (optional — falls back to inline defaults)
3. Multiple vehicles can share configs or have unique ones

## Relevant Skills

- **`unity-scriptable-objects`** — ScriptableObject patterns for data-driven configuration
