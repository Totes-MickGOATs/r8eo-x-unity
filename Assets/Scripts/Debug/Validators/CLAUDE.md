# Debug/Validators/

Contract validators that assert system invariants at runtime and in edit mode.
These run on debug boot and are referenced by the physics conformance audit.

## Files

| File | Purpose |
|------|---------|
| `InputContractValidator.cs` | Asserts that `IVehicleInput` implementations satisfy axis range and timing contracts |
| `ObservableContractValidator.cs` | Validates that observable event sources fire and complete correctly |
| `VehicleContractValidator.cs` | Checks vehicle component wiring: required components, null references, physics material |
| `WheelContractValidator.cs` | Validates wheel count, layer assignments, and suspension configuration invariants |

## Relevant Skills

- **`unity-testing-patterns`** — Contract validator patterns, NUnit assertion conventions, EditMode test fixtures
