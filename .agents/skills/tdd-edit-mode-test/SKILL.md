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

## Topic Pages

- [Instructions](skill-instructions.md)

