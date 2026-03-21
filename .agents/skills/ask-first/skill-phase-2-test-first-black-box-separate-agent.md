# Phase 2: Test-First (Black Box, Separate Agent)

> Part of the `ask-first` skill. See [SKILL.md](SKILL.md) for the overview.

## Phase 2: Test-First (Black Box, Separate Agent)

> **MANDATORY:** Tests MUST be written by a separate agent with NO knowledge of the implementation. This prevents implementation bias from infecting the tests.

### 2.1 Dispatch the Test-Writing Agent

Spawn a dedicated test-writing agent (as a research-only subagent that returns test code for you to commit, or using `bash scripts/tools/safe-worktree-init.sh <task>` if it needs to commit directly). Provide it with:

**GIVE the test agent:**
- Public API signatures only: `ClassName.MethodName(param: Type) -> ReturnType`
- Gameplay mechanics description (what the system does, not how)
- Acceptance criteria from Phase 1
- Physics invariants from root CLAUDE.md
- The `clean-room-qa` skill reference: `.agents/skills/clean-room-qa/SKILL.md`
- RC racing domain context (see Domain Expertise section below)

**DO NOT give the test agent:**
- Implementation code or method bodies
- Internal architecture details or private methods
- Your hypothesis about the root cause (for bug fixes)
- Comments explaining "how" the code works

### 2.2 Minimum Coverage Requirements

Every change MUST meet these minimum test requirements:

| Level | What | Minimum | Where |
|-------|------|---------|-------|
| **Unit** | Every public method/function touched or added | 1 positive + 1 negative per method (minimum 2) | `Assets/Tests/EditMode/` |
| **Integration** | Every cross-class/cross-system interaction | 1 per interaction path | `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/` |
| **E2E (PlayMode)** | Every user-facing feature or behavior change | 1 per feature/behavior | `Assets/Tests/PlayMode/` |

**Definitions:**

- **Positive test:** Verifies the method works correctly with valid, expected input (happy path). Confirms the right thing happens.
- **Negative test:** Verifies the method handles invalid, edge, or boundary input correctly. Confirms the wrong thing doesn't happen. Examples: zero input, null, out-of-range values, negative where positive expected, NaN, empty collections.
- **Integration test:** Verifies two or more classes/systems work together correctly. Tests the wiring, not just the logic. If class A calls class B, there must be a test that exercises A->B together.
- **E2E test:** Verifies a complete user-facing behavior from input to visible outcome, running in PlayMode with real game systems.

### 2.3 Additional Test Categories (Where Applicable)

Beyond the minimum positive/negative pair, add tests from these categories when they apply:

| Category | When to Use | Example |
|----------|-------------|---------|
| **Boundary** | Method has thresholds, limits, or ranges | Value at exactly the deadzone edge |
| **Conservation** | System transforms quantities that must be preserved | Differential: left + right = total |
| **Monotonicity** | More input should produce more output (or vice versa) | More compression -> more spring force |
| **Symmetry** | Left/right or positive/negative should be symmetric | Left steer mirrors right steer |
| **Independence** | Changing X should not affect unrelated Y | Changing grip doesn't affect suspension |
| **Idempotency** | Calling twice should equal calling once (where expected) | Setting a state, not toggling |
| **Temporal** | Behavior depends on frame rate or timestep | Physics at 50Hz vs 100Hz should converge |

### 2.4 Test Naming Convention

Every test MUST have a descriptive name that reads like a sentence:

```
MethodName_Scenario_ExpectedOutcome
```

Examples:
- `ComputeSuspensionForce_WhenFullyCompressed_NeverReturnsPullForce`
- `ApplyLateralGrip_WithPositiveLateralVelocity_ReturnsNegativeForce`
- `ProcessInput_WithZeroDeadzone_PassesThroughUnchanged`
- `ComputeDifferentialSplit_WithOneWheelAirborne_SendsAllForceToGroundedWheel`

BAD names:
- `TestSuspension1`
- `Test_Fix_Bug_42`
- `ItWorks`

### 2.5 Verify Tests Are RED

Before proceeding to Phase 3, ALL new tests MUST be run and confirmed failing:

1. Run the tests: `just test-fast "TestClassName"` or the appropriate test command
2. Verify each test fails for the **expected reason** (not a compile error, not a missing reference)
3. If a test passes before implementation exists, it's either:
   - Testing the wrong thing (fix the test)
   - The feature already works (the "bug" might not exist — investigate)
   - Testing something trivially true (make the test more specific)

### 2.6 Test Integrity Rule

> **CRITICAL:** After Phase 3 implementation, if the implementing agent believes a test assertion is wrong (testing the wrong behavior), they MUST:
> 1. File it as a finding with explanation
> 2. Discuss with the user before modifying
> 3. NEVER silently change a test to make it pass
>
> This prevents the common failure mode where an agent "fixes" the test instead of fixing the code.

---

## Phase 3: Implement Against Tests (TDD Green)

Now — and ONLY now — write implementation code.

### 3.1 Standard TDD Cycle

For each failing test:
1. Write the minimum code to make it pass
2. Run the test -> confirm GREEN
3. Run ALL new tests to check for regressions
4. Commit (test + implementation together, or implementation alone if tests were committed earlier)

### 3.2 Regression Check

After all new tests are green:
1. Run related existing tests for the same system
2. If any existing test breaks, your implementation has a regression — fix it before proceeding
3. Do NOT disable or modify existing tests to accommodate your change without user approval

### 3.3 Reassessment Gate

If a test cannot be made to pass with a reasonable implementation:
1. STOP implementation
2. Return to Phase 1.7 (hypothesis)
3. Reassess: Is your hypothesis wrong? Is the test wrong? Is the design flawed?
4. If the test is wrong, follow the Test Integrity Rule (2.6) — discuss with user

### 3.4 Completion

After all tests are green and regressions are clear:
1. Follow the Definition of Done in root CLAUDE.md
2. Commit all changes
3. Push, create PR, verify CI

---

## Domain Expertise: RC Racing Simulator

The test-writing agent must understand these domain concepts to write meaningful tests. This is what "domain expertise" means for this project.

### What Makes a Good RC Racing Simulator

- **Physics feel:** The car should behave like a real 1/10 scale RC buggy (1.5 kg, 0.166m wheel radius)
- **Suspension:** Progressive, no pull forces, realistic compression/rebound
- **Tires:** Grip depends on load, surface, and slip angle. No grip without weight on the tire.
- **Drivetrain:** Differential types (open, ball, spool) each feel different. Conservation of force is mandatory.
- **Air physics:** Throttle pitches nose up, brake pitches down, steering causes roll. Gyroscopic stabilization from wheel spin.
- **Input:** Precise, responsive, configurable deadzone and curves. No phantom inputs.
- **Performance:** Consistent frame rate, no physics jitter, deterministic at fixed timestep

### What to Test For (Quality Signals)

- **Smoothness:** No sudden jumps in force, velocity, or position. Interpolation where needed.
- **Determinism:** Same input -> same output, regardless of frame rate (within physics timestep tolerance)
- **Stability:** System doesn't explode, oscillate, or produce NaN under any input combination
- **Responsiveness:** Input changes produce immediate, proportional response
- **Conservation:** Energy, force, and mass are neither created nor destroyed
- **Physical correctness:** Forces point the right direction, magnitudes are in the right range for 1/10 scale

### What Would a Tester Check in a Real RC Racing Game?

- Does the car feel planted on the track? (Suspension + grip)
- Does it slide predictably when pushed too hard? (Tire model)
- Does it fly correctly off jumps? (Air physics)
- Can you feel the difference between surfaces? (Surface detection)
- Is the steering precise at low speeds and stable at high speeds? (Steering curve)
- Does the differential actually affect handling? (Drivetrain)
- Are there any dead zones or jumps in the controls? (Input processing)
- Does the car behave the same on different hardware? (Determinism)

---

