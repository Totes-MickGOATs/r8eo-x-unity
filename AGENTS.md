# AGENTS.md — R8EO-X RC Racing Simulator (Unity)

Realistic RC buggy simulation. Unity + C# (R8EOX.* assemblies) + NUnit EditMode tests (327 tests).

## Branch Workflow

Never commit to `main`. All changes via feature branches + PRs.

```bash
git checkout -b feat/<task>              # branch from origin/main
git add <files> && git commit -m "type: short description"
git push -u origin feat/<task>
gh pr create --base main                 # CI runs automatically
gh run watch                             # wait for Lint & Preflight green
gh pr view --json state -q .state        # confirm MERGED
```

Commit types: `feat` `fix` `refactor` `test` `docs` `chore` `ci` `perf`

**Done when:** PR merged + `Lint & Preflight` CI green + local tests pass.

## Architecture

| Assembly | Directory | Key Files |
|---|---|---|
| `R8EOX.Vehicle` | `Assets/Scripts/Vehicle/` | `RCCar.cs`, `RaycastWheel.cs`, `Drivetrain.cs`, `RCAirPhysics.cs` |
| `R8EOX.Vehicle.Physics` | `Assets/Scripts/Vehicle/Physics/` | `SuspensionMath`, `GripMath`, `DrivetrainMath`, `AirPhysicsMath`, `GyroscopicMath`, `ESCMath`, `TumbleMath` |
| `R8EOX.Vehicle.Config` | `Assets/Scripts/Vehicle/Config/` | `MotorPresetConfig`, `SuspensionConfig`, `TractionConfig`, `WheelInertiaConfig` |
| `R8EOX.Input` | `Assets/Scripts/Input/` | `RCInput.cs`, `InputMath.cs`, `IVehicleInput.cs`, `TriggerDetector.cs` |
| `R8EOX.Core` | `Assets/Scripts/Core/` | `SurfaceType.cs`, `SurfaceConfig.cs` |
| `R8EOX.Debug` | `Assets/Scripts/Debug/` | `TelemetryHUD.cs`, `ContractDebugger.cs`, `WheelTerrainDiagnostics.cs`, `DebugBootstrap.cs` |
| `R8EOX.GameFlow` | `Assets/Scripts/GameFlow/` | `GameFlowManager.cs`, `GameFlowStateMachine.cs`, `NavigationStack.cs`, `SceneRegistry.cs` |
| `R8EOX.Camera` | `Assets/Scripts/Camera/` | `CameraController.cs`, `CameraMode.cs`, `ChaseCamera.cs`, `TracksideAnchor.cs` |
| `R8EOX.Editor` | `Assets/Scripts/Editor/` | `SceneSetup.cs`, `OutpostTrackSetup.cs`, `RCCarEditor.cs`, `DrivetrainEditor.cs`, `AddBuggyMaterials.cs` |
| Tests | `Assets/Tests/EditMode/` | 327 NUnit tests |

**Pattern:** MonoBehaviours (`RCCar`, `RaycastWheel`) delegate math to static `Physics/` classes. Signal Up, Call Down. Constants as `const float k_...`, no bare literals.

Namespaces: `R8EOX.Vehicle` · `R8EOX.Vehicle.Physics` · `R8EOX.Vehicle.Config` · `R8EOX.Input` · `R8EOX.Core` · `R8EOX.Debug` · `R8EOX.GameFlow` · `R8EOX.Editor`

## Physics Domain (1/1 scale = ×10 RC)

Mass 15 kg · `k_SphereCastRadius = 0.15f` · Spring front 700 N/m / rear 350 N/m · Damping 4.25 N·s/m · Curve-sampled grip (NOT Pacejka)
Invariants: `suspensionForce >= 0` · no grip without normal load · `Drivetrain` conserves total axle force

## TDD (Mandatory)

Test naming: `MethodName_Scenario_ExpectedOutcome`. **Write failing test first, then implement.**

```bash
# Run EditMode tests via Unity CLI
Unity -batchmode -runTests -testPlatform EditMode -projectPath .
# Or in Unity Editor: Window > General > Test Runner > EditMode > Run All
```

## System Registry

Every source file must be registered in `resources/manifests/<system>.json`:

```json
{ "name": "vehicle", "status": "ACTIVE", "files": ["Assets/Scripts/Vehicle/RCCar.cs"], "dependencies": ["input", "core"], "tests": { "editmode": ["TuningApiTests"] } }
```

Statuses: `ACTIVE` · `DEPRECATED` (set `replaced_by`) · `EXPERIMENTAL`

## ScriptableObject Config Pattern

```csharp
[CreateAssetMenu(menuName = "R8EOX/My Config")]
public class MyConfig : ScriptableObject {
    [SerializeField] private float _value = 1f;
    public float Value => _value;
}
```

Create asset: `Assets > Create > R8EOX > My Config`. Assign to `RCCar` or `RaycastWheel` in inspector (inline fallback defaults always present).

## MCP Servers

`.mcp.json`: `UnityMCP` (editor automation) · `fathom` (`http://localhost:19876/mcp`)

After C# changes: wait 10-15s for domain reload. Avoid `Physics`/`UnityEngine.Physics` namespace collisions.

## Key Files

- `CLAUDE.md` — primary AI workflow rules
- `resources/manifests/` — system file registry (`just validate-registry`)
- `Assets/Tests/EditMode/` — 327 NUnit unit tests
- `Packages/manifest.json` — Unity packages (`com.unity.inputsystem@1.8.2`)
- `.coverage-baseline.json` — coverage baseline (327 tests across Vehicle/Physics, Input, GameFlow, BlackBox)
- `CI_LEARNINGS.md` — accumulated CI failure patterns and fixes