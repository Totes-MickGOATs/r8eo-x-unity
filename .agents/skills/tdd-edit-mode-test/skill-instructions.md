# Instructions

> Part of the `tdd-edit-mode-test` skill. See [SKILL.md](SKILL.md) for the overview.

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

