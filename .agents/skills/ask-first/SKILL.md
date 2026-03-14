# Ask-First: Mandatory Pre-Implementation Workflow

Use this skill when starting any new dev task to clarify requirements and write failing tests before implementation. Covers the three-phase interrogate-test-implement cycle that every bug fix, feature, and refactor must follow.

Every dev task — bug fix, feature, refactor — follows three phases in strict order:

1. **Phase 1: Interrogate** — Understand before you act
2. **Phase 2: Test-First** — Prove your understanding with failing tests (black-box, separate agent)
3. **Phase 3: Implement** — Make the tests green via TDD

Skipping phases wastes time. Agents that skip Phase 1 misunderstand requirements. Agents that skip Phase 2 write tests biased by their implementation. This workflow prevents both failure modes.

---

## Phase 1: Interrogate (Ask Yourself First)

Before writing ANY code (including tests), complete every step below. Write your answers down — they form the basis for Phase 2.

### 1.1 State the Problem in One Sentence

If you cannot state the problem in a single clear sentence, you do not understand it yet. Vague statements like "fix the physics" or "improve input handling" are not acceptable. Be specific:

- BAD: "Fix suspension"
- GOOD: "Suspension returns negative force when spring is fully extended, causing the car to stick to the ground"

### 1.2 Search Memories

Check postmortems, feedback memories, and project memories for past failures on similar systems.

Ask yourself:
- Has an agent been burned by this before?
- What went wrong and how was it fixed?
- Are there known gotchas for this system?

### 1.3 Search Git History

Check for reverted commits or re-opened issues on the same files:

```bash
git log --all --oneline -- <files-you-plan-to-touch>
git log --all --oneline --grep="revert" -- <files-you-plan-to-touch>
```

If the same area has been fixed and reverted before, understand why before proceeding.

### 1.4 List Your Questions

Write out every question you need answered before starting. Be specific:

- What is the expected behavior?
- What is the actual behavior?
- What are the acceptance criteria?
- What systems does this touch?
- What are the inputs and outputs?
- What are the constraints (performance, compatibility, physics accuracy)?

### 1.5 Answer What You Can

For each question, try to answer it yourself:
- Read the relevant code (signatures and CLAUDE.md, not deep implementation)
- Read relevant skill files in `.agents/skills/`
- Read memories and postmortems
- Check console output via `read_console`
- Check the physics invariants table in root CLAUDE.md (for physics work)
- Check `.ai/knowledge/` for architecture docs and status

Cross off every question you can answer. The remaining unanswered questions are your unknowns.

### 1.6 Identify Guards and Invariants

What existing safeguards apply to this code?

- **Constants and enums** — named values that must not be changed carelessly
- **Assertions in code** — runtime checks that enforce invariants
- **Existing tests** — what's already tested? Run them to see current state
- **Physics invariants** — suspension >= 0, lateral force opposes velocity, differential conserves force, etc.
- **Architecture rules** — Signal Up/Call Down, value mutability tiers, DRY patterns

These guards are your allies. They tell you what must remain true after your change.

### 1.7 Form Your Hypothesis

For bugs: What is the root cause? Be specific and testable.
- BAD: "Something is wrong with the grip system"
- GOOD: "GripMath.ComputeLateralForce returns positive force when lateral velocity is positive, but it should return negative force to oppose the velocity"

For features: What is the minimal design?
- BAD: "Add weather effects"
- GOOD: "Add a WetSurfaceModifier that multiplies grip coefficients by a wetness factor (0.0-1.0), applied in the existing surface detection pipeline"

### 1.8 Verify the Full Contract (End-to-End Wiring)

Before you write any code, verify that you understand the FULL contract — not just the class you're editing, but everything it connects to. A perfectly implemented class that isn't wired into the game is worthless.

**For player-controlled objects/features:**

- **Input contract:** What controller inputs drive this? Keyboard? Gamepad? Both? Are the Input Actions defined in the Input Action Asset? Do they fire the correct action type (Button, Value, PassThrough)? Are they bound to the correct control scheme?
- **Input registration:** Do the input callbacks actually get registered? Is `Enable()`/`Disable()` called on the action map? Is there an `OnEnable`/`OnDisable` that subscribes/unsubscribes? What happens when input is zero vs. absent?
- **Signal chain:** Does the input signal flow all the way through? Input Action -> Input Handler -> Game System -> Visual/Audio Feedback. Trace every link. A broken link anywhere means the feature silently fails.
- **Controller compatibility:** Does this work with gamepad AND keyboard? Do combined axes behave correctly? (Remember: Xbox combined trigger axis rests at -1.0, not 0.0 — see postmortem_phantom_trigger_input.md)
- **Rebinding:** If input is rebindable, does the new binding persist? Does the UI reflect the rebind?

**For static/world objects the player interacts with:**

- **Visibility contract:** When does this object appear? Is it in the scene hierarchy at startup, or spawned at runtime? If spawned, what triggers the spawn? Is there a pool?
- **Collision contract:** Does it have the correct collider type and size? Is it on the right physics layer? Does the layer matrix allow collision with the objects it needs to interact with? Is it a trigger or a solid collider?
- **Interaction contract:** How does the player interact with it? Collision enter? Trigger enter? Raycast? Proximity? Is the interaction handler attached and enabled? Does it check the correct tag/layer?
- **Lifecycle contract:** When is it created? When is it destroyed? What happens if it's destroyed while the player is interacting with it? Does it clean up its event subscriptions?

**For any game system:**

- **Scene wiring:** Is the system's GameObject/component present in every scene where it's needed? Is it a singleton, a scene object, or a prefab instance? What's the initialization order relative to other systems?
- **Dependency contract:** What other systems does this depend on? Are they guaranteed to be initialized first? What happens if a dependency is missing? Does it fail loudly (error log) or silently (null reference later)?
- **Output contract:** What does this system produce that other systems consume? Signals? Events? Shared state? Are the consumers actually listening? Can you trace the output to a visible/audible result?
- **Configuration contract:** What inspector values, ScriptableObjects, or settings drive this system's behavior? Are reasonable defaults set? What happens with zero/null/extreme values?
- **Cleanup contract:** Does this system clean up after itself? Unsubscribe from events, return pooled objects, stop coroutines, release resources?

**Contract verification checklist (add to your Phase 1 notes):**

```
## Contract Verification
- [ ] Input: Actions defined, bound, enabled, callbacks registered
- [ ] Signal chain: Input -> Handler -> System -> Feedback (every link traced)
- [ ] Collisions: Correct layer, layer matrix allows interaction, collider type/size correct
- [ ] Scene wiring: Component present in scene, initialization order correct
- [ ] Dependencies: All required systems present and initialized first
- [ ] Output: Consumers are listening, output produces visible/audible result
- [ ] Configuration: Defaults set, edge cases handled (zero, null, extreme)
- [ ] Cleanup: Events unsubscribed, resources released, pools returned
```

> **The goal:** By the time you finish Phase 1, you should be able to describe the complete journey from "player presses button" to "something visible happens on screen" — every link in the chain. If you can't trace the full path, you have unknowns that will become bugs.

### 1.9 Adversarial Thinking: "What Would Break?"

Before you touch anything, think about what could go wrong:

- What other systems call into or depend on the code you're changing?
- What assumptions are you making? Are they documented or just hopeful?
- What edge cases exist? (zero input, maximum values, negative values, NaN, null)
- Could your change cause a regression in an unrelated system?
- What happens if this code runs at a different frame rate? Different physics timestep?
- For physics: does your change violate any invariant? (conservation, sign, bounds)

Write these down. They become test cases in Phase 2.

### 1.10 Rate Your Confidence (1-5)

Be honest with yourself:

| Rating | Meaning | Action |
|--------|---------|--------|
| **5** | Crystal clear, well-documented area, similar work done before | Proceed to Phase 2 |
| **4** | Good understanding, minor unknowns | Proceed, note unknowns in plan |
| **3** | Reasonable plan but some unknowns remain | Proceed with caution, consider asking user |
| **2** | Significant uncertainty, multiple possible approaches | **MUST ask user at least one clarifying question** |
| **1** | Barely understand the problem | **MUST ask user, explain what's unclear, propose options** |

**If your confidence is below 3, STOP.** Do not proceed to Phase 2 until you've resolved your unknowns with the user. The cost of asking is low. The cost of building on a wrong assumption is high.

### 1.11 State Your Plan

Summarize your approach in 3-5 bullet points, incorporating lessons from steps 1.1-1.9. This plan is your contract with yourself. If you deviate significantly during implementation, come back to Phase 1.

---

## Phase 2: Test-First (Black Box, Separate Agent)

> **MANDATORY:** Tests MUST be written by a separate agent with NO knowledge of the implementation. This prevents implementation bias from infecting the tests.

### 2.1 Dispatch the Test-Writing Agent

Spawn a dedicated test-writing agent (using `isolation: "worktree"` or as a research-only subagent that returns test code for you to commit). Provide it with:

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

## When to Skip (Almost Never)

| Change Type | Phase 1 | Phase 2 | Phase 3 |
|-------------|---------|---------|---------|
| Bug fix | REQUIRED | REQUIRED | REQUIRED |
| New feature | REQUIRED | REQUIRED | REQUIRED |
| Refactor | REQUIRED | REQUIRED (verify existing tests cover it) | REQUIRED |
| Performance optimization | REQUIRED | REQUIRED (benchmark tests) | REQUIRED |
| Pure documentation | Recommended | Skip | Skip |
| Formatting/lint fix | Skip | Skip | Skip |
| CLAUDE.md update | Skip | Skip | Skip |

---

## Quick Reference Checklist

Copy this into your working notes at the start of every task:

```
## Ask-First Checklist

### Phase 1: Interrogate
- [ ] Problem stated in one sentence
- [ ] Memories searched (postmortems, feedback, project)
- [ ] Git history checked for reverts/re-opens
- [ ] Questions listed
- [ ] Questions answered (code, docs, memories, console)
- [ ] Guards and invariants identified
- [ ] Hypothesis formed (specific, testable)
- [ ] Full contract verified (input, signals, collisions, scene wiring, dependencies, output, cleanup)
- [ ] "What would break?" adversarial analysis done
- [ ] Confidence rated (1-5). If < 3, asked user for clarification
- [ ] Plan stated (3-5 bullets)

### Phase 2: Test-First (Black Box)
- [ ] Test agent dispatched with signatures + domain context only
- [ ] Unit tests: >= 1 positive + 1 negative per method
- [ ] Integration tests: 1 per cross-class interaction
- [ ] E2E tests: 1 per user-facing feature/behavior
- [ ] All tests run and confirmed RED
- [ ] Test names are descriptive sentences

### Phase 3: Implement
- [ ] Each test made GREEN with minimum code
- [ ] All new tests GREEN together
- [ ] Related existing tests still pass (no regressions)
- [ ] Committed and pushed
```

---

## Related Skills

- **`clean-room-qa`** — Black-box testing methodology (the foundation for Phase 2)
- **`reverse-engineering`** — Chain-of-custody debugging (useful during Phase 1 for bugs)
- **`unity-testing-patterns`** — UTF code examples, assertions, mocking patterns
- **`unity-e2e-testing`** — PlayMode testing, InputTestFixture, visual testing
- **`unity-testing-debugging-qa`** — Master QA reference, testing pyramid, CI integration
- **`debug-system`** — Structured logging and overlays for runtime diagnosis
