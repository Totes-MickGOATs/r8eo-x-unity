# Assets/Tests/PlayMode/

PlayMode integration and conformance tests that run inside a live Unity game loop with real physics simulation.

## Running PlayMode Tests

**CI does NOT run PlayMode tests** — only lint/preflight. Always run locally after writing PlayMode tests.

Unity Editor must be **closed** first (batch mode crashes if the project is already open):

```bash
"/c/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe" \
  -batchmode -nographics -runTests -testPlatform PlayMode \
  -projectPath "$(pwd)" \
  -testResults test-results/playmode.xml -logFile test-results/playmode.log

# Check summary
grep -E 'testcasecount|passed=|failed=' test-results/playmode.xml | head -3
```

Exit code 0 = all passed; exit code 2 = some failed (check XML).

## Files

| File | Class | Purpose |
|------|-------|---------|
| `VehicleIntegrationTests.cs` | `VehicleIntegrationTests` | Integration tests: settlement, zero-input safety, motor direction, friction, steering, drive layout |
| `CompoundConformanceTests.cs` | `CompoundConformanceTests` | Physics conformance compound scenarios (L1, L3, L5, L7, L8, L10, D8, L12) |
| `ConformanceAtRestTests.cs` | `ConformanceAtRestTests` | L1, L3, L10 conformance tests; uses `ConformanceSceneSetup` helpers |
| `ConformanceTransitionTests.cs` | `ConformanceTransitionTests` | L5, L7, L8 conformance tests; uses `ConformanceSceneSetup.SpawnTestVehicle` |
| `ConformanceDynamicsTests.cs` | `ConformanceDynamicsTests` | D8, L12 conformance tests; uses `ConformanceSceneSetup` drive input helpers |
| `DebugLoggingTests.cs` | `DebugLoggingTests` | Black-box tests: car active/driving and car landing → tagged console logs appear |
| `TerrainRegressionTests.cs` | `TerrainRegressionTests` | Flat-ground regression: spring length, motor drive, no backward drift |
| `TerrainSeamTests.cs` | `TerrainSeamTests` | Seam/edge terrain physics tests |
| `R8EOX.Tests.PlayMode.asmdef` | -- | Assembly definition referencing Vehicle, Input, Camera, Debug, Core |

## Subdirectories

| Dir | Contents |
|-----|----------|
| `Helpers/` | Shared test utilities and scene setup factories |

## Assembly References

- `R8EOX.Vehicle` -- car, wheels, drivetrain, air physics
- `R8EOX.Input` -- input types (for future input injection)
- `R8EOX.Camera` -- camera controller
- `R8EOX.Debug` -- telemetry/debug types
- `R8EOX.Core` -- surface types

## Test Patterns

- **Programmatic scene setup:** Tests create ground + car from GameObjects (no scene files needed). See `ConformanceSceneSetup` helper.
- **No RCInput:** RCCar has null `_input`; throttle defaults to zero. Tests simulate driving by setting `RaycastWheel.MotorForceShare` and `IsBraking` directly.
- **Physics timing:** `yield return new WaitForFixedUpdate()` for physics steps; `WaitForSettle()` helper with timeout for convergence.
- **Layer isolation:** Car on layer 8, ground on layer 9; wheel raycasts exclude car layer via `GroundMask`.
- **Constants:** All physics values use `k_` prefixed named constants -- no bare literals.

## Conformance Test Coverage

Tests map to checks in `.ai/knowledge/architecture/audit-physics-conformance.md`:

| Test | Check ID | Scenario |
|------|----------|----------|
| `L1_AtRestOnFlat_AllForcesBalance_NoDrift` | L1 | Forces balance at rest, no creep |
| `L3_FullThrottleAndBrake_Decelerates` | L3 | Brake overpowers motor |
| `L5_JumpLanding_ImpactProportionalToHeight` | L5 | Impact velocity ~ sqrt(2gh) |
| `L7_GroundToAir_SmoothForceTransition` | L7 | No velocity spike at takeoff |
| `L8_AirToGround_DampedLanding` | L8 | Bounce peaks decrease (damped) |
| `L10_HighSpeedStraight_SpeedConvergesToMax` | L10 | Speed asymptotes below V_max |
| `D8_FreeFall_AccelerationEqualsGravity` | D8 | Free-fall delta_v = g*t |
| `L12_WeightTransferBraking_FrontLoadsUp` | L12 | Pitch or load shift under braking |

## Relevant Skills

- **`unity-testing-patterns`** -- Unity Test Framework patterns, TDD cycle
- **`unity-e2e-testing`** -- PlayMode testing, input simulation, CI integration
- **`clean-room-qa`** -- Black-box testing with domain-physics-derived assertions
- **`physics-conformance-audit`** -- Full 93-check conformance catalogue
