# Unity Testing, Debugging & QA

Comprehensive guide to testing, debugging, and quality assurance for Unity games. Use this skill as the unified entry point when planning QA strategy, setting up test infrastructure, debugging issues, or integrating quality checks into CI/CD. For deep dives into specific areas, see the Related Skills section.

## Minimum Coverage Requirements (MANDATORY)

> **Every change MUST meet these minimum test requirements. No exceptions.**

| Level | What | Minimum | Where |
|-------|------|---------|-------|
| **Unit** | Every public method/function touched or added | **1 positive + 1 negative per method** (minimum 2) | `Assets/Tests/EditMode/` |
| **Integration** | Every cross-class/cross-system interaction | **1 per interaction path** | `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/` |
| **E2E (PlayMode)** | Every user-facing feature or behavior change | **1 per feature/behavior** | `Assets/Tests/PlayMode/` |

- **Positive test:** Valid input, correct output (happy path)
- **Negative test:** Invalid/edge/boundary input handled correctly (zero, null, out-of-range, NaN)
- **Test naming:** `MethodName_Scenario_ExpectedOutcome`
- **Pre-implementation:** Tests MUST be written by a separate black-box agent (no implementation knowledge) before implementation begins. See `.agents/skills/ask-first/SKILL.md` Phase 2.
- **Test Integrity Rule:** Implementing agents MUST NOT silently modify tests to make them pass. If a test assertion appears wrong, file it as a finding and discuss with the user.

## Testing Philosophy

### The Testing Pyramid

| Level | What to Test | Speed | Coverage Priority |
|-------|-------------|-------|-------------------|
| **Unit** | Pure functions, math, data, single components in isolation | Fast | Heavy |
| **Integration** | Component wiring, system interaction, data flow | Medium | Heavy |
| **Functional** | Feature behavior against requirements | Medium | Moderate |
| **E2E** | Full gameplay flows, boot-to-play journeys | Slow | Few but critical |
| **Performance** | Frame time, memory, load/stress/endurance | Varies | Targeted |

**Rule of thumb:** Heavy unit and integration coverage. Few but meaningful E2E tests covering critical user journeys. Performance tests targeted at known bottlenecks.

### When to Test

Testing is not a phase at the end of development. It is an ongoing process that underpins every stage:

- **Prototyping:** Validate core mechanics with unit tests as you build them
- **Production:** Integration and functional tests verify systems work together
- **Pre-ship:** E2E tests confirm critical user journeys, performance tests benchmark target hardware
- **Post-ship:** Regression tests ensure updates do not break existing functionality

Every code change should be accompanied by tests. Every bug fix starts with a failing test.

### QA Responsibility

- **Solo / small team:** Recruit a workgroup of friends for playtesting. Consider third-party QA providers for coverage you cannot do yourself (e.g., localization testing)
- **Larger teams:** Dedicated QA engineers run structured test plans. Developers still own unit and integration tests
- **Player testing:** Involve a diverse group representing your target audience. Vary experience levels, play styles, and accessibility needs

---

## Code Quality & Static Analysis

### Roslyn Analyzers

Roslyn analyzers use the .NET Compiler Platform APIs to analyze C# code in real-time. They complement IDE IntelliSense with deeper analysis, enforcing code standards and catching issues before they reach runtime.

#### Severity Levels

| Level | Indicator | Meaning |
|-------|-----------|---------|
| **Error** | Red squiggle | Code will not compile or has a critical issue |
| **Warning** | Green squiggle | Potential problem that should be addressed |
| **Suggestion** | Ellipsis (three dots) | Code style improvement recommended |
| **Silent/Hidden** | None visible | Rule is active but produces no visual indicator |

Each severity level is user-configurable per rule.

#### Unity-Specific Analyzers

Unity ships with `Microsoft.Unity.Analyzers` which catches Unity-specific anti-patterns:

- Using `string` methods for `CompareTag` instead of `gameObject.CompareTag()`
- Null coalescing (`??`) on Unity objects (which override `==` for destroyed objects)
- Empty `Update()` / `FixedUpdate()` methods that waste CPU cycles
- Incorrect `SerializeField` usage

#### Custom Analyzers

Install additional analyzers as NuGet packages:

- **StyleCop.Analyzers** — code style enforcement (naming, spacing, ordering)
- **Roslynator** — 500+ code analysis rules and refactorings
- **SonarAnalyzer** — security and reliability rules

Analyzers appear under Dependencies > Analyzers in the IDE solution explorer.

### .editorconfig

An `.editorconfig` file ships with the project and enforces code style across the team, overriding individual IDE settings:

```ini
# Top-level EditorConfig
root = true

[*.cs]
# Indentation
indent_style = space
indent_size = 4

# Naming conventions
dotnet_naming_rule.private_fields_underscore.severity = warning
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_prefix

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.underscore_prefix.required_prefix = _
dotnet_naming_style.underscore_prefix.capitalization = camel_case

# Code style
csharp_prefer_braces = true:suggestion
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion

# Analyzer severity overrides
dotnet_diagnostic.IDE0051.severity = warning  # Remove unused private members
dotnet_diagnostic.IDE0052.severity = warning  # Remove unread private members
dotnet_diagnostic.UNT0006.severity = error    # Incorrect message signature
```

**Nesting:** Place `.editorconfig` at the project root. Subdirectory `.editorconfig` files override the root for files in that subtree.

**Rider integration:** Rider fully supports `.editorconfig` and displays analyzer results inline. Configure additional inspections via Settings > Editor > Inspections.

---

## IDE Debugging

This project uses **JetBrains Rider** as the primary IDE. The debugging concepts below apply to all Unity-compatible IDEs (Rider, Visual Studio, VS Code).

### Attaching the Debugger to Unity

1. Open the Unity project and the solution in Rider
2. In Rider, click **Attach to Unity Process** (toolbar or Run > Attach to Unity Process)
3. Select the Unity Editor instance (or a running player build)
4. Rider attaches and breakpoints become active

**First-time setup:** Rider auto-detects Unity projects. Ensure the Unity plugin is enabled in Rider's plugin settings.

### Breakpoints

Set breakpoints by clicking the left gutter or pressing **Ctrl+F8** (Rider) / **F9** (VS/VS Code). A red dot appears indicating the breakpoint.

When execution hits a breakpoint, the IDE pauses and shows the current state.

#### Conditional Breakpoints

Right-click a breakpoint to add conditions:

- **Condition:** Only break when an expression is true (e.g., `health <= 0` or `frameCount > 1000`)
- **Hit count:** Only break after N hits (useful for loops)
- **Log message:** Print to debug output without pausing (non-breaking breakpoint / tracepoint)

Conditional breakpoints are invaluable for debugging issues that only occur under specific circumstances.

### Debugging Windows

| Window | Purpose | When to Use |
|--------|---------|-------------|
| **Locals** | Shows all local variables in the current scope | Inspect values at the current breakpoint |
| **Watch** | Monitor specific variables/expressions across steps | Track values that change over time or are out of scope |
| **Call Stack** | Shows the method call chain leading to the current point | Understand how code reached this point, navigate to callers |
| **Breakpoints** | Lists all breakpoints with enable/disable controls | Manage many breakpoints across files |
| **Evaluate Expression** | Execute arbitrary expressions at the current breakpoint | Test fixes, inspect complex expressions, call methods |

### Step Controls

| Action | Rider Shortcut | What It Does |
|--------|---------------|--------------|
| **Step Over** | F8 | Execute current line, move to next line |
| **Step Into** | F7 | Enter the method call on the current line |
| **Step Out** | Shift+F8 | Run until current method returns |
| **Resume** | F9 | Continue execution until next breakpoint |
| **Run to Cursor** | Alt+F9 | Continue to the line where cursor is placed |

### Debugging Builds

To debug standalone (non-Editor) builds:

1. In Unity: **File > Build Settings**
2. Enable **Development Build** and **Script Debugging**
3. Build and run the player
4. In Rider: **Run > Attach to Unity Process** > select the running player instance

All breakpoints, locals, watches, and call stacks work in development builds.

### Rider-Specific Tips

- **Unity Explorer** panel shows assets, packages, and scratches alongside the solution
- **Unity Log** panel mirrors the Unity Console with filtering and search
- **Performance indicators** show which methods allocate GC in the gutter
- **Find Usages** traces signal/event connections across the codebase
- **Decompile** external assemblies to step into Unity engine code
- **Heap Allocations Viewer** plugin highlights allocations in the editor

---

## Unity Test Framework

The Unity Test Framework (UTF) integrates with NUnit to provide automated testing inside the Unity Editor and player builds.

### Setup

UTF is installed via the Package Manager (`com.unity.test-framework`). Tests require assembly definitions that reference the test runner:

```
Assets/
  Tests/
    EditMode/
      Game.Tests.EditMode.asmdef    <- includePlatforms: ["Editor"]
      MyEditModeTests.cs
    PlayMode/
      Game.Tests.PlayMode.asmdef    <- includePlatforms: [] (any platform)
      MyPlayModeTests.cs
```

For testing package code (e.g., Input System), add a `testables` section to `Packages/manifest.json`:

```json
"testables": [
    "com.unity.inputsystem"
]
```

### Edit Mode vs Play Mode

| Aspect | Edit Mode | Play Mode |
|--------|-----------|-----------|
| **Runs in** | Unity Editor (no Play mode) | Real game loop (Play mode) |
| **Speed** | Fast | Slower (full frame loop) |
| **Access** | Editor + game code | Game code at runtime |
| **Use for** | Pure logic, ScriptableObjects, math, data validation, Editor extensions | MonoBehaviours, physics, coroutines, scene interactions, input |
| **Attribute** | `[Test]` (prefer NUnit) | `[UnityTest]` (coroutine, multi-frame) |
| **Assembly** | `includePlatforms: ["Editor"]` | `includePlatforms: []` |

Use `[Test]` (NUnit) in Edit mode unless you need to yield special instructions. Use `[UnityTest]` for Play mode tests that span multiple frames.

### Key Attributes

| Attribute | Purpose |
|-----------|---------|
| `[Test]` | Synchronous single-frame test |
| `[UnityTest]` | Coroutine test (multi-frame, physics, scene loading) |
| `[SetUp]` / `[TearDown]` | Run before/after each test |
| `[OneTimeSetUp]` / `[OneTimeTearDown]` | Run once per test fixture |
| `[UnitySetUp]` / `[UnityTearDown]` | Async (coroutine) setup/teardown |
| `[TestCase(a, b, c)]` | Parameterized test with inline data |
| `[Category("E2E")]` | Tag for filtering in Test Runner or CI |
| `[Timeout(30000)]` | Fail if test exceeds N milliseconds |

### Additional UTF Attributes

Beyond the core attributes above, UTF provides specialized attributes for advanced scenarios:

| Attribute | Purpose |
|-----------|---------|
| `[ConditionalIgnore]` | Conditionally skip tests based on runtime conditions |
| `[PrebuildSetup]` | Execute setup logic before player builds (used with performance tests) |
| `[PostBuildCleanup]` | Execute cleanup logic after player builds |
| `[TestMustExpectAllLogs]` | Enforces that all `Debug.Log` calls are expected via `LogAssert.Expect()` |
| `[TestPlayerBuildModifier]` | Modifies player build configuration for test runs |
| `[TestRunCallback]` | Hooks into test execution lifecycle for custom reporting |
| `[UnityPlatform]` | Restricts test execution to specific platforms |

**Key recommendation:** Use NUnit's `[Test]` over `[UnityTest]` unless you specifically need custom yield instructions, frame-skipping, or time-waiting. `[Test]` runs faster and has simpler control flow.

### The AAA Pattern

Every test follows **Arrange, Act, Assert**:

```csharp
[Test]
public void TakeDamage_ReducesHealth()
{
    // Arrange — set up the system under test
    var stats = new PlayerStats(maxHealth: 100);

    // Act — perform the action being tested
    stats.TakeDamage(30);

    // Assert — verify the expected outcome
    Assert.AreEqual(70, stats.CurrentHealth);
}
```

### Input Simulation

The Input System package provides `InputTestFixture` for programmatic input in Play mode tests:

```csharp
public class MovementTests : InputTestFixture
{
    private Keyboard _keyboard;

    public override void Setup()
    {
        base.Setup(); // Creates isolated input state
        _keyboard = InputSystem.AddDevice<Keyboard>();
    }

    [UnityTest]
    public IEnumerator Player_MovesForward_OnWKey()
    {
        // Arrange
        SceneManager.LoadScene("TestScene");
        yield return new WaitForSeconds(0.5f);
        var player = GameObject.Find("Player");
        var startPos = player.transform.position;

        // Act
        Press(_keyboard.wKey);
        yield return new WaitForSeconds(1f);
        Release(_keyboard.wKey);
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.Greater(player.transform.position.z, startPos.z);
    }
}
```

`Press()` and `Release()` are provided by `InputTestFixture`. For gamepad input, use `Set(gamepad.leftStick, new Vector2(1f, 0f))`.

### Standalone Player Testing

Switch the **Run Location** dropdown in the Test Runner window to run tests in a standalone player build instead of the Editor. This validates behavior on target platforms.

For CI, split build and run using:

- `ITestPlayerBuildModifier` — customize build options (disable auto-run, change output path)
- `ITestRunCallback` — capture test results and write to `testresults.xml` at `Application.persistentDataPath`

> **Deep dive:** See `unity-testing-patterns` skill for full code examples, assertions reference, mocking patterns, and parameterized tests.

---

## Test-Driven Development (TDD)

### Red-Green-Refactor Cycle

1. **Write a failing test** — describe the desired behavior or reproduce the bug
2. **Run the test — confirm RED** — verify it fails for the expected reason
3. **Implement** — write the minimum code to make the test pass
4. **Run the test — confirm GREEN** — verify it passes
5. **Refactor** — clean up while tests stay green
6. **Commit** — test and implementation together

> **Non-negotiable:** Steps 2 and 4 require actually running the tests. A test that was never executed proves nothing.

### TDD in Game Development

TDD is less common in game development than in traditional software because gameplay is inherently exploratory and creative. However, TDD excels for:

- **Physics and math** — deterministic inputs and outputs
- **Game rules and scoring** — clearly defined logic
- **Data validation** — save/load, serialization, config parsing
- **Bug fixes** — write a test that reproduces the bug, then fix it

For gameplay prototyping, manual playtesting is often more appropriate. Once mechanics solidify, backfill with automated tests.

### Code Coverage

Package: `com.unity.testtools.codecoverage`

#### Setup

1. Install via Package Manager (Window > Package Manager > search "Code Coverage")
2. Open **Window > Analysis > Code Coverage**
3. Enable the Code Coverage checkbox (adds editor overhead while active)

#### Configuration

- **Included Assemblies** — select which assemblies to monitor (e.g., `Game.Runtime`)
- **HTML Report** — generate detailed HTML coverage reports
- **Report History** — track coverage trends over time
- **Auto Generate Report** — create reports automatically after test runs

#### Coverage Recording (Manual Testing)

Beyond test-driven coverage, you can record coverage during manual Play mode:

1. Open the Code Coverage window
2. Click "Start Recording"
3. Play through your game
4. Click "Stop Recording"
5. Reports auto-generate showing code paths exercised during gameplay

#### Key Metrics

| Metric | What It Measures |
|--------|-----------------|
| **Line Coverage** | Percentage of coverable lines executed |
| **Cyclomatic Complexity** | Number of code paths. Methods scoring above 15 warrant review and refactoring |
| **CRAP Score** (Change Risk Anti-Patterns) | Identifies risky code combining complexity and coverage. High scores indicate refactoring priority |

**Limitation:** Code Coverage shows which lines were reached, not which logic paths were taken. A line can be "covered" without all its branches being tested.

#### Report Formats

HTML, SonarQube, Cobertura, LCOV, SVG/PNG badges.

#### CI/CD Integration

```bash
Unity.exe -batchmode -enableCodeCoverage \
  -coverageResultsPath ./results \
  -coverageOptions "generateHtmlReport;generateAdditionalMetrics;assemblyFilters:+R8EOX.*,-*Tests*"
```

Key coverage options:

| Option | Purpose |
|--------|---------|
| `generateHtmlReport` | Creates HTML coverage report |
| `generateHtmlReportHistory` | Includes historical data in reports |
| `generateAdditionalReports` | Exports SonarQube, Cobertura, LCOV formats |
| `generateBadgeReport` | SVG/PNG coverage badges |
| `generateAdditionalMetrics` | Cyclomatic Complexity + CRAP Score |
| `dontClear` | Accumulates results across multiple runs |
| `verbosity` | Logging levels: verbose, info, warning, error, off |
| `assemblyFilters` | Include/exclude assemblies with `+`/`-` prefixes, supports wildcards |
| `pathFilters` | Include/exclude source files with glob patterns |

**Important:** Unity 2020.1+ requires **Debug mode** for accurate coverage data. Release/IL2CPP builds strip information needed for coverage instrumentation.

---

## Testing Categories

### Unit Testing

Test individual units or components in isolation. The fastest and most granular level of testing.

**Manual unit testing:** Add test conditions with `Debug.Log` and observe in Play mode. Useful for quick checks but not repeatable or automatable.

**Automated unit testing:** Write NUnit tests in Edit mode for pure logic, or Play mode for MonoBehaviour behavior. Tests run in the Test Runner window and can be automated in CI.

```csharp
[TestCase(100, 30, 70)]
[TestCase(100, 100, 0)]
[TestCase(50, 50, 0)]
public void TakeDamage_ReducesCorrectAmount(int maxHp, int damage, int expected)
{
    var stats = new PlayerStats(maxHealth: maxHp);
    stats.TakeDamage(damage);
    Assert.AreEqual(expected, stats.CurrentHealth);
}
```

### Integration Testing

Test how multiple components work together. Catches bugs in data flow, signal connections, and system wiring that unit tests miss.

```csharp
// Example: weapon fires bullet -> bullet hits enemy -> enemy takes damage -> score updates
[UnityTest]
public IEnumerator WeaponFire_KillsEnemy_AwardsPoints()
{
    // Arrange: spawn player, enemy, set up scoring
    // Act: simulate weapon fire, wait for physics
    // Assert: enemy health == 0, score increased
}
```

Use `[SetUp]` and `[TearDown]` to create and clean up test environments. Simulate player input using the Command Pattern or `InputTestFixture`.

### Regression Testing

Verify that changes to the codebase do not introduce new bugs or re-introduce old ones.

- **When to use:** Large, complex projects with frequent updates. Live games with ongoing patches.
- **Strategy:** Automate wherever possible. Run in parallel with feature development. Every bug fix should add a regression test.
- **Cost management:** Regression test suites grow over time. Prioritize tests by risk and impact. Remove tests for deprecated features.

### Functional Testing

Evaluate features against their design requirements using "when this, then that" acceptance criteria:

> *When the player presses spacebar, the character should jump.*

This statement serves as both an instruction to the developer and an acceptance criterion for the tester.

- **Black-box testing:** Test the system without knowledge of internal workings (inputs and expected outputs only)
- **White-box testing:** Test with knowledge of internal structure (verify code paths, edge cases)

Functional tests are most powerful when requirements are clearly defined and scoped.

### Performance Testing

Evaluate whether the game runs at acceptable levels of performance, frame rate, responsiveness, and stability.

#### Test Types

| Type | Purpose | Example |
|------|---------|---------|
| **Load** | Behavior under heavy workload | 100 active physics bodies, 1000 particles |
| **Stress** | Behavior under extreme/unexpected conditions | Sudden spike in spawned entities |
| **Endurance** | Behavior over extended play sessions | Memory growth over 30 minutes of gameplay |

#### Unity Profiler

Open via **Window > Analysis > Profiler** (Ctrl+7). Key modules:

| Module | What to Watch |
|--------|--------------|
| **CPU Usage** | Frame time spikes, GC.Alloc markers, script time |
| **Memory** | Managed heap growth, native allocations, GC frequency |
| **Rendering** | Draw calls, batches, set pass calls |
| **Physics** | Active bodies, contacts, solver time |

#### Performance Testing Extension

Package: `com.unity.test-framework.performance` (v3.0.3)

```csharp
using Unity.PerformanceTesting;

[Test, Performance]
public void PhysicsStep_Under2ms()
{
    Measure.Method(() =>
    {
        Physics.Simulate(Time.fixedDeltaTime);
    })
    .WarmupCount(5)
    .MeasurementCount(50)
    .Run();
}
```

##### Measurement APIs

| API | Use Case |
|-----|----------|
| `Measure.Method(() => { code }).Run()` | Samples a method's execution time |
| `Measure.Frames()` | Records time per frame (Play Mode only, good for physics) |
| `Measure.Scope()` | Measurement context for custom metrics and profiler markers |
| `Measure.Custom(sampleGroup, value)` | Custom measurements with defined units |

##### Configuration Chaining

| Method | Purpose | Default |
|--------|---------|---------|
| `.MeasurementCount(n)` | Number of measurement iterations | 7 (recommended: 20) |
| `.WarmupCount(n)` | Pre-recording executions to eliminate init overhead | 0 |
| `.IterationsPerMeasurement(n)` | Repetitions within each measurement | 1 |
| `.DontRecordFrametime()` | Disables frame time recording | Enabled |
| `.ProfilerMarkers(string)` | Targets specific profiler markers | None |

##### Custom SampleGroups

Define custom metrics with specific units for domain-relevant measurements:

```csharp
var allocated = new SampleGroup("TotalAllocatedMemory", SampleUnit.Megabyte);
Measure.Custom(allocated, value);
```

##### Performance Test Best Practices

1. Ensure standard deviation remains below 5% across measurements
2. Avoid sub-millisecond measurements (sensitive to background processes and OS scheduling)
3. Isolate operations into separate tests — one concern per test method
4. Use `[SetUp]`/`[TearDown]` to clean up GameObjects between runs
5. Disable background applications for local testing
6. Maintain a single Quality level for consistent configuration
7. Disable VSync in Quality settings to remove frame-rate capping
8. Remove cameras when not measuring rendering; use `-batchmode` for headless runs

Tests use the `[Performance]` attribute (often combined with `[PrebuildSetup()]` for build-specific setup).

#### Frame Budget Reference

| Target | Budget | Notes |
|--------|--------|-------|
| 30 fps | 33.33 ms/frame | Desktop/console baseline |
| 60 fps | 16.66 ms/frame | Smooth gameplay target |
| Mobile 30 fps | ~22 ms/frame | Reserve 35% for thermal management |
| Mobile 60 fps | ~10.83 ms/frame | Reserve 35% for thermal management |

A single frame that exceeds the target budget will cause visible hitches.

**Three-phase profiling workflow:**
1. **Establish baseline** — profile before making major changes
2. **Track during development** — monitor against frame budgets continuously
3. **Validate improvements** — profile after changes, prove they had the desired effect

> **Deep dive:** See `unity-debugging-profiling` for Profiler details, custom markers, Frame Debugger, and Memory Profiler. See `unity-performance-optimization` for optimization techniques.

---

## CI/CD Integration

### Batch Mode Test Execution

Run tests headlessly for CI pipelines:

```bash
# Edit Mode tests (fast, no GPU required)
Unity -batchmode -nographics -runTests \
  -testPlatform EditMode \
  -testResults ./results/editmode-results.xml \
  -projectPath . \
  -logFile ./results/unity.log

# Play Mode tests (may need GPU for rendering tests)
Unity -batchmode -runTests \
  -testPlatform PlayMode \
  -testResults ./results/playmode-results.xml \
  -projectPath . \
  -logFile ./results/unity.log

# Filter by category
Unity -batchmode -runTests \
  -testPlatform PlayMode \
  -testCategory "E2E" \
  -projectPath .
```

Exit code 0 = all passed, non-zero = failures.

### GameCI (GitHub Actions)

```yaml
- name: Run Edit Mode Tests
  uses: game-ci/unity-test-runner@v4
  env:
    UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
  with:
    testMode: EditMode
    projectPath: .
    artifactsPath: ./test-results
    githubToken: ${{ secrets.GITHUB_TOKEN }}
```

### Split Build and Run

For capturing test results from player builds in CI:

1. **`ITestPlayerBuildModifier`** — override `ModifyOptions()` to disable auto-run and redirect build output
2. **`ITestRunCallback`** — override `RunFinished()` to serialize results to `testresults.xml`

This separates the build step from the run step, enabling custom result reporting and CI integration.

### Headless Limitations

| Flag | GPU | Use Case |
|------|-----|----------|
| `-batchmode -nographics` | No GPU | Logic tests, physics, data validation |
| `-batchmode` (no `-nographics`) | Needs GPU | Rendering tests, screenshot comparisons |

For visual tests on Linux CI, use Xvfb or a real GPU.

---

## Diagnostics & Crash Reporting

### Cloud Diagnostics Advanced (Backtrace)

Unity's crash reporting tool captures:

- Environment snapshot at time of crash
- Call stack, heap state, register values
- Automatic analysis to determine root cause

**Deduplication:** A clustering algorithm groups crashes by root cause, helping prioritize which errors to fix first for maximum stability improvement.

**Analytics:** Trend and pattern analysis over time reveals systemic issues and the impact of fixes.

---

## Cross-Platform Testing

### Device Coverage Strategy

Test on a range of devices within your target platforms, especially for mobile:

- **Operating systems:** Different OS versions (Android API levels, iOS versions)
- **Screen sizes:** Phone, tablet, different aspect ratios
- **Hardware tiers:** Lowest-spec supported device, mid-range, flagship
- **Input methods:** Touch, gamepad, keyboard+mouse

### Mobile Considerations

Beyond functionality, test for:

- **Battery drainage** — sustained gameplay power consumption
- **Thermal throttling** — performance under heat (endurance tests)
- **Memory pressure** — low-memory device behavior, background app kills

Revisit device benchmarks when adding features. A change that is fine on flagship hardware may break minimum-spec devices.

---

## Case Study: Phantom Input on Windows

This case study illustrates the diagnostic pattern and TDD discipline described above. It took 4 PRs to fully resolve because testing was not comprehensive from the start.

### Symptom

Vehicle accelerated and braked with no controller connected on Windows. The Legacy Input Manager reported `-1.0` on the `CombinedTriggers` axis — a constant phantom value from Windows platform behavior, not a Unity bug.

### Diagnostic Signal

**Constant value = phantom input.** Attaching an `InputDiagnostics` MonoBehaviour to the vehicle and logging axis values every 30 frames revealed that trigger values showed zero variance across hundreds of frames. Real human input always shows micro-jitter. A value that never changes is stuck/phantom.

### Resolution

Three-layer defense: variance-based TriggerDetector (jitter < 0.02 = stuck), mode gating (zero output during Detecting/None modes), and deadzones (0.15 triggers, 0.2 steering). The critical insight was that the detection phase must **observe, never drive** — reading raw axes during detection leaked phantom values as real input.

### Lesson

Write the full test matrix first: ALL modes (Detecting, Separate, Combined, None) x ALL axes (throttle, brake, steering). Fixing one axis at a time without comprehensive tests caused 4 rounds of fixes instead of 1. The test matrix IS the specification.

> **Deep dive:** See `unity-input-debugging` skill for the complete guide.

---

## Related Skills

| Skill | When to Use |
|-------|-------------|
| **`unity-input-debugging`** | Deep dive: Phantom input on Windows, variance-based detection, three-layer defense, input TDD matrix, diagnostic MonoBehaviour |
| **`unity-testing-patterns`** | Deep dive: UTF code examples, assertions reference, mocking, parameterized tests, setup/teardown patterns |
| **`unity-debugging-profiling`** | Deep dive: Unity Profiler, Frame Debugger, Memory Profiler, Gizmos, custom debug tools, logging |
| **`unity-e2e-testing`** | Deep dive: E2E automation, InputTestFixture, visual testing, AltTester, third-party tools, CI integration |
| **`unity-performance-optimization`** | Deep dive: batching, GC reduction, object pooling, LOD, shader optimization |
| **`clean-room-qa`** | Black-box testing with zero implementation knowledge — derive tests from function signatures and domain physics |
| **`reverse-engineering`** | Systematic debugging methodology — chain of custody from symptom to root cause |
| **`debug-system`** | Debug overlay architecture — structured logging, F-key overlays, runtime inspection |

---

## Quick Reference: What Testing Do I Need?

```
Is this a bug fix?
  YES -> Write a failing test that reproduces it (TDD), then fix
  NO  |
      v
Is this pure logic / math / data?
  YES -> Unit test (Edit Mode, [Test])
  NO  |
      v
Does it involve multiple systems interacting?
  YES -> Integration test (Play Mode, [UnityTest])
  NO  |
      v
Is it a full user journey (boot -> menu -> play)?
  YES -> E2E test (Play Mode, InputTestFixture, [Category("E2E")])
  NO  |
      v
Is it a performance concern?
  YES -> Performance test (Profiler + Performance Testing Extension)
  NO  |
      v
Is it a visual/rendering concern?
  YES -> Screenshot test (Graphics Test Framework) -- needs GPU in CI
  NO  |
      v
Is it a design requirement ("when X, then Y")?
  YES -> Functional test (black-box or white-box)
  NO  |
      v
Did code just change and might have broken things?
  YES -> Run regression suite, add tests for changed areas
```
