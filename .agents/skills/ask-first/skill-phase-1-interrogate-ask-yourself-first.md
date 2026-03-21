# Phase 1: Interrogate (Ask Yourself First)

> Part of the `ask-first` skill. See [SKILL.md](SKILL.md) for the overview.

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

### 1.10 Audit All Call Sites

Search for EVERY other location that uses the same pattern, API, or function you are about to fix or modify. Do not assume the bug exists in only one place.

**Steps:**
1. **Search broadly:** Use `Grep` to find all callers, all implementations, all references to the pattern you are fixing
2. **List all found locations:** Write them down explicitly — file path and line
3. **Assess each location:** Could the same bug exist there? Is the same pattern used incorrectly?
4. **Include ALL affected locations in the test plan:** Every location with the same bug pattern gets a test case in Phase 2

**Rationale:** The phantom input bug (postmortem_phantom_trigger_input.md) required 5 PRs (#25, #34, #40, #44, #52) because each agent only fixed one code path. The bug existed in multiple locations using the same pattern, but each agent found and fixed only their entry point. Always audit ALL paths.

**Example searches:**
```bash
# Find all callers of the method you're fixing
rg "MethodName\(" --type cs

# Find all implementations of the same pattern
rg "pattern-you-are-fixing" --type cs

# Find all files that import/use the same API
rg "using SomeNamespace" --type cs
```

If you find the same bug in multiple locations, your fix MUST address ALL of them — not just the one reported. File separate test cases for each location.

### 1.11 Rate Your Confidence (1-5)

Be honest with yourself:

| Rating | Meaning | Action |
|--------|---------|--------|
| **5** | Crystal clear, well-documented area, similar work done before | Proceed to Phase 2 |
| **4** | Good understanding, minor unknowns | Proceed, note unknowns in plan |
| **3** | Reasonable plan but some unknowns remain | Proceed with caution, consider asking user |
| **2** | Significant uncertainty, multiple possible approaches | **MUST ask user at least one clarifying question** |
| **1** | Barely understand the problem | **MUST ask user, explain what's unclear, propose options** |

**If your confidence is below 3, STOP.** Do not proceed to Phase 2 until you've resolved your unknowns with the user. The cost of asking is low. The cost of building on a wrong assumption is high.

### 1.12 State Your Plan

Summarize your approach in 3-5 bullet points, incorporating lessons from steps 1.1-1.11. This plan is your contract with yourself. If you deviate significantly during implementation, come back to Phase 1.

