---
name: tdd-edit-mode-test
description: Writes NUnit EditMode tests in Assets/Tests/EditMode/ following MethodName_Scenario_ExpectedOutcome naming. Use when implementing any public method (TDD: write RED test first). Trigger phrases: 'write test', 'add unit test', 'test this', 'failing test first', 'red-green'. Key capabilities: pure static class tests (InputMath, SuspensionMath, GripMath), MonoBehaviour testing via new GameObject + DestroyImmediate teardown, LogAssert usage, [SetUp]/[TearDown] patterns. Do NOT use for PlayMode tests or integration tests that require scene loading.
---
# TDD EditMode Test

## Critical

- **RED first, always.** Write the test, confirm it fails (`just test`), then implement. Never write test after implementation.
- **Never use `Object.Destroy()`** in EditMode — always `Object.DestroyImmediate()` in `[TearDown]`.
- **`LogAssert.Expect(...)` must be called BEFORE the code that logs** — not after.
- **Do not add `R8EOX.Camera` or `R8EOX.Track` to `.asmdef` references** unless the class under test lives in those assemblies — check `R8EOX.Tests.EditMode.asmdef` first.
- All tests live in `Assets/Tests/EditMode/`, namespace `R8EOX.Tests.EditMode`.

## Instructions

### Step 1 — Identify the test type

| Subject | Type | Template |
|---|---|---|
| Static math class (`InputMath`, `SuspensionMath`, `GripMath`, `DrivetrainMath`) | Pure static | No `[TestFixture]`, no `[SetUp]` |
| Non-MonoBehaviour class with state (`GameFlowStateMachine`, `NavigationStack`) | Stateful | `[TestFixture]`, `[SetUp]`, no `[TearDown]` |
| MonoBehaviour component (`DebugBootstrap`, `RCCar`) | MonoBehaviour | `[TestFixture]`, `[SetUp]` + `[TearDown]` with `DestroyImmediate` |

Verify the subject class exists and its assembly is referenced in `Assets/Tests/EditMode/R8EOX.Tests.EditMode.asmdef` before proceeding.

### Step 2 — Name the test method

Format: `MethodName_Scenario_ExpectedOutcome`

Examples from codebase:
- `ApplyDeadzone_MidRange_RemapsCorrectly`
- `ComputeSuspensionForceWithDamping_CompressingVelocity_AddsDamping`
- `FullMenuFlow_BootToPlaying`
- `Attach_WhenRCCarExists_LogsBootstrapMessage`

### Step 3 — Write the test file

**Pure static template** (`Assets/Tests/EditMode/InputMathTests.cs`):
```csharp
using NUnit.Framework;
using R8EOX.Input;          // swap for actual assembly namespace

namespace R8EOX.Tests.EditMode
{
    public class InputMathTests
    {
        const float k_Tolerance = 0.0001f;

        [Test]
        public void ApplyDeadzone_MidRange_RemapsCorrectly()
        {
            // raw=0.575, dz=0.15 → (0.575-0.15)/(1-0.15) = 0.5
            float result = InputMath.ApplyDeadzone(0.575f, 0.15f);
            Assert.AreEqual(0.5f, result, k_Tolerance);
        }

        [Test]
        public void ApplyDeadzone_BelowDeadzone_ReturnsZero()
        {
            float result = InputMath.ApplyDeadzone(0.10f, 0.15f);
            Assert.AreEqual(0f, result, k_Tolerance);
        }
    }
}
```

**Stateful non-MonoBehaviour template**:
```csharp
using NUnit.Framework;
using R8EOX.GameFlow;

namespace R8EOX.Tests.EditMode
{
    [TestFixture]
    public sealed class GameFlowStateMachineTests
    {
        private GameFlowStateMachine _sm;

        [SetUp]
        public void SetUp()
        {
            _sm = new GameFlowStateMachine();
        }

        [Test]
        public void InitialState_AfterConstruction_IsBoot()
        {
            Assert.AreEqual(GameState.Boot, _sm.CurrentState);
        }
    }
}
```

**MonoBehaviour template**:
```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Debug;
using R8EOX.Vehicle;

namespace R8EOX.Tests.EditMode
{
    [TestFixture]
    public class DebugBootstrapTests
    {
#if UNITY_EDITOR || DEBUG
        private GameObject _carGo;

        [SetUp]
        public void SetUp()
        {
            _carGo = new GameObject("TestCar");
            _carGo.AddComponent<Rigidbody>();
            _carGo.AddComponent<RCCar>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_carGo != null)
                Object.DestroyImmediate(_carGo);
        }

        [Test]
        public void Attach_WhenRCCarExists_LogsBootstrapMessage()
        {
            LogAssert.Expect(LogType.Log,
                new System.Text.RegularExpressions.Regex(@"\[DebugBootstrap\] Attached"));
            DebugBootstrap.AttachTo(_carGo);
        }
#endif
    }
}
```

### Step 4 — Use named constants, not bare literals

Declare physics constants as `const float k_...` matching production values:
```csharp
const float k_SpringStrength = 700f;   // front axle, N/m
const float k_SpringDamping  = 4.25f;  // N·s/m
const float k_DefaultDt      = 0.008333f; // 120 Hz
```

### Step 5 — Run RED, then implement

```bash
just test   # confirm test fails before implementing
# implement the method
just test   # confirm GREEN
```

Verify both RED and GREEN before committing.

## Examples

**User says:** "Write a failing test for `SuspensionMath.ComputeSuspensionForce` — positive spring compression returns positive force."

**Actions:**
1. Identify type: static math class → pure static template
2. Check `R8EOX.Vehicle.Physics` is in `.asmdef` references ✓
3. Name: `ComputeSuspensionForce_Compressed_ReturnsPositiveForce`
4. Write test with `Assert.Greater(force, 0f)` and physics constants
5. Run `just test` → RED confirmed
6. Implement `ComputeSuspensionForce` → run `just test` → GREEN

**Result:**
```csharp
[Test]
public void ComputeSuspensionForce_Compressed_ReturnsPositiveForce()
{
    // spring compressed: current < rest
    float force = SuspensionMath.ComputeSuspensionForce(
        k_SpringStrength, k_RestDistance, currentLength: 0.15f);
    Assert.Greater(force, 0f);
}
```

## Common Issues

**`CS0246: The type or namespace 'R8EOX.Vehicle.Physics' could not be found`**
The assembly is missing from `.asmdef`. Open `Assets/Tests/EditMode/R8EOX.Tests.EditMode.asmdef`, add `"R8EOX.Vehicle.Physics"` to `"references"`, then reimport.

**`LogAssert.NoUnexpectedReceived` fails after test**
You called `LogAssert.Expect(...)` but the code didn't log, or the regex didn't match. Verify the exact log string with `read_console` MCP tool, then tighten the regex.

**`DestroyImmediate` called on null crashes the test**
Always guard: `if (_carGo != null) Object.DestroyImmediate(_carGo);`

**Test passes when it should fail (false GREEN)**
You implemented before running RED. Delete the implementation, run `just test`, confirm failure, then re-implement.

**`#if UNITY_EDITOR || DEBUG` wraps entire class body — test not discovered**
The `[TestFixture]` attribute must be outside the `#if` block. Only the fields and methods go inside:
```csharp
[TestFixture]
public class MyTests
{
#if UNITY_EDITOR || DEBUG
    // fields, SetUp, TearDown, [Test] methods
#endif
}
```