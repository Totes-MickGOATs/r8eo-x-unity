# R8EO-X Coding Standards

> **Every rule on this page is MANDATORY.** No exceptions, no "we'll fix it later."
> Violations must be fixed before commit. These rules exist to prevent churn and enable
> confident refactoring at any scale.

---

## 1. Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Private/protected fields | `_camelCase` (underscore prefix) | `_rigidbody`, `_springStrength` |
| Public properties | `PascalCase` | `ForwardSpeed`, `IsAirborne` |
| Public methods | `PascalCase` | `ApplyWheelPhysics()`, `GetSpeedKmh()` |
| Private methods | `PascalCase` | `ComputeSuspension()`, `ApplyGroundDrive()` |
| Local variables | `camelCase` (no prefix) | `springLen`, `gripFactor` |
| Parameters | `camelCase` | `engineForce`, `deltaTime` |
| Constants (`const`) | `k_PascalCase` | `k_AirborneThreshold`, `k_WheelRadius` |
| Static readonly | `k_PascalCase` | `k_MotorPresets`, `k_DiffStiffness` |
| Static fields | `s_PascalCase` | `s_Instance` (rare — see rule below) |
| Classes | `PascalCase` | `RCCar`, `RaycastWheel`, `Drivetrain` |
| Interfaces | `IPascalCase` | `IDamageable`, `IPhysicsBody` |
| Enums | `PascalCase` (singular) | `enum Surface { Dirt, Gravel, Tarmac }` |
| Enum members | `PascalCase` | `MotorPreset.Motor17_5T` |
| Events/Actions (after) | `On` + past participle | `OnLapCompleted`, `OnDoorOpened` |
| Events/Actions (before) | `On` + present participle | `OnLapStarting`, `OnDoorOpening` |
| Delegates | `PascalCase` + descriptive | `SpeedChangedHandler` |
| Namespaces | `R8EOX.SubSystem` | `R8EOX.Vehicle`, `R8EOX.Input` |
| Files | Match class name exactly | `RaycastWheel.cs` contains `class RaycastWheel` |
| Test classes | `{ClassName}Tests` | `RaycastWheelTests.cs` |
| Test methods | `Method_Condition_Expected` | `ComputeSuspension_FullCompression_ReturnsMaxForce` |
| Assemblies | `R8EOX.{System}` | `R8EOX.Vehicle`, `R8EOX.Tests.EditMode` |
| ScriptableObjects | `{Name}Config` or `{Name}Data` | `MotorPresetConfig`, `SurfaceData` |

### Bool Naming

- **Booleans MUST be prefixed with a verb:** `is`, `has`, `was`, `should`, `can`
- Applies to fields, properties, locals, and parameters: `_isDead`, `_hasStartedTurn`, `IsGameOver`, `CanJump`, `shouldReset`
- Applies to methods returning `bool` too: `IsNewPosition()`, `HasReachedTarget()`, `CanAccelerate()`

### Event Timing Convention

- **Present participle = before it happens** (pre-event): `OnDoorOpening`, `OnLapStarting`
- **Past participle = after it happened** (post-event): `OnDoorOpened`, `OnLapCompleted`
- Always keep the `On` prefix per the naming table above

### Forbidden Patterns

- **No Hungarian notation** — never `fSpeed`, `bIsActive`, `iCount`
- **No `m_` prefix** — use `_camelCase` for private fields
- **No abbreviations** unless universally understood (`rb` for Rigidbody is OK; `wp` for waypoint is NOT)
- **No single-letter variables** except `i`, `j`, `k` in loops and `x`, `y`, `z` for coordinates

---

## 2. Architecture Rules

### Signal Up, Call Down

- Children/subsystems **raise events** (C# `event Action`) to notify parents
- Parents/orchestrators **call methods** on children to command them
- **NEVER** reverse this: a child must not call methods on its parent or siblings directly
- Use serialized references or events for cross-system communication

### Composition over Inheritance

- **Maximum inheritance depth: 2** (Base → Concrete). No deeper hierarchies.
- Prefer interfaces (`IResettable`, `IPhysicsBody`) over abstract base classes
- Use component composition: attach multiple focused MonoBehaviours, not one god class
- If two classes share behavior, extract a shared component or utility class — don't inherit

### Namespace Rules

Every C# file MUST declare a namespace matching its folder path under `Assets/Scripts/`:

| Folder | Namespace |
|--------|-----------|
| `Scripts/Vehicle/` | `R8EOX.Vehicle` |
| `Scripts/Input/` | `R8EOX.Input` |
| `Scripts/Camera/` | `R8EOX.Camera` |
| `Scripts/Core/` | `R8EOX.Core` |
| `Scripts/Debug/` | `R8EOX.Debug` |
| `Scripts/UI/` | `R8EOX.UI` |
| `Scripts/Track/` | `R8EOX.Track` |
| `Scripts/Audio/` | `R8EOX.Audio` |
| `Scripts/Effects/` | `R8EOX.Effects` |
| `Scripts/Editor/` | `R8EOX.Editor` |
| `Tests/EditMode/` | `R8EOX.Tests.EditMode` |
| `Tests/PlayMode/` | `R8EOX.Tests.PlayMode` |

### Assembly Definitions

- One Assembly Definition (`.asmdef`) per major system folder
- Test assemblies: `R8EOX.Tests.EditMode.asmdef`, `R8EOX.Tests.PlayMode.asmdef`
- Assemblies declare explicit references — no implicit "reference everything"
- Editor-only code in assemblies with `Editor` platform constraint

### Class Responsibility Rules

| Class Type | Use For | NOT For |
|-----------|---------|---------|
| MonoBehaviour | Unity lifecycle (Awake/Update/FixedUpdate), inspector fields, coroutines | Pure math, data models, algorithms |
| Plain C# class | Physics formulas, math helpers, data processing, state machines | Anything needing Update/Awake |
| ScriptableObject | Configuration data, presets, shared constants, asset references | Runtime mutable state |
| Struct | Small immutable data (< 5 fields), physics results, math tuples | Large data, reference semantics |
| Static class | Pure utility functions, extension methods, constants | Mutable state, initialization |

### Forbidden Patterns

- **No singletons** — use serialized references in the scene or event-based communication
- **No static mutable state** — `static readonly` constants are OK; `static int counter` is NOT
- **No `Find()` / `FindObjectOfType()` / `FindObjectsOfType()`** — use `[SerializeField]` references
- **No string-based anything** — no `Invoke("method")`, no `GameObject.Find("name")`, no `CompareTag("string")` with literals (use `const` string fields)
- **No `SendMessage()` / `BroadcastMessage()`** — use direct method calls or events
- **No god classes** — if a class does more than one thing, split it

---

## 3. MonoBehaviour Lifecycle Order

Methods MUST appear in this order within the class body. Each method has a strict purpose:

```csharp
// ---- Lifecycle (in execution order) ----

void Awake()        // Self-initialization ONLY: GetComponent on self, cache own references
                    // NEVER access other GameObjects — they may not be initialized yet

void OnEnable()     // Subscribe to events and delegates
                    // Pair with OnDisable — every += here needs a -= there

void Start()        // Cross-object initialization: access other GameObjects, inject dependencies
                    // All Awake() calls are guaranteed complete

void FixedUpdate()  // ALL physics: forces, raycasts, vehicle simulation, collision responses
                    // Use Time.fixedDeltaTime — NEVER Time.deltaTime here

void Update()       // Input polling, non-physics game logic, animation triggers
                    // Use Time.deltaTime here

void LateUpdate()   // Camera follow, UI updates, visual-only adjustments
                    // Runs AFTER all Update() calls — use for things that depend on Update results

void OnDisable()    // Unsubscribe from ALL events subscribed in OnEnable
                    // Prevents memory leaks and null reference exceptions

void OnDestroy()    // Final cleanup: dispose unmanaged resources, remove from registries
```

### Rules

- **NEVER put physics in Update()** — forces, raycasts, and velocity reads go in FixedUpdate()
- **NEVER put input polling in FixedUpdate()** — input is frame-based, not physics-based
- **NEVER access Time.deltaTime in FixedUpdate()** — use `Time.fixedDeltaTime`
- **ALWAYS pair OnEnable/OnDisable** — every event subscription needs an unsubscription
- **Camera logic goes in LateUpdate()** — ensures it runs after all movement is resolved
- Use `[RequireComponent(typeof(Rigidbody))]` when a script depends on a sibling component

---

## 4. Value Mutability Tiers

| Tier | Mechanism | When to Use | Example |
|------|-----------|-------------|---------|
| **Const** | `const` / `static readonly` | Algorithm logic, physics math, layer IDs — never changes at runtime | `k_WheelRadius = 0.166f`, `k_AirborneThreshold = 5` |
| **Config** | `[SerializeField]` or ScriptableObject | Per-instance tuning set in editor — varies between prefabs but fixed at runtime | `_springStrength`, motor presets |
| **Settings** | Settings manager / PlayerPrefs | User preferences persisted to disk — changed via Options menu | Graphics quality, audio volume, input bindings |
| **Dynamic** | Runtime variable | Computed or changed every frame/event — driven by gameplay | `_forwardSpeed`, `_smoothThrottle`, `_tumbleFactor` |

### Rules

- **NEVER use bare numeric literals in physics code** — extract to named `const` or `static readonly`
- **EVERY tunable value MUST be Config (serialized) or Setting** — never hardcode something that might need adjustment
- **Group related constants** in dedicated static classes:
  - `PhysicsConstants` — wheel radius, mass, spring defaults
  - `CollisionLayers` — layer mask constants
  - `SurfaceTypes` — surface type enum and friction coefficients
- **Magic number test:** If a number appears in code without a name, it MUST be given one. The only exceptions are `0`, `1`, `-1`, `0.5f`, `2f`, and `Mathf.PI`.

---

## 5. Code Organization Rules

### Size Limits

| Metric | Maximum | Action When Exceeded |
|--------|---------|---------------------|
| Method length | 30 lines | Extract helper methods |
| Class length | 300 lines | Extract subsystems or components |
| Parameter count | 4 | Use a struct or parameter object |
| Nesting depth | 3 levels | Use early returns or extract methods |
| `if/else` chains | 3 branches | Use switch, lookup table, or strategy pattern |

### Method Purity Rules

- **No flag parameters** — don't pass a `bool` that fundamentally changes a method's behavior. Instead, create two clearly-named methods.
  - Bad: `GetAngle(bool inRadians)` — Good: `GetAngleInDegrees()` and `GetAngleInRadians()`
  - Bad: `SetActive(bool withAnimation)` — Good: `Activate()` and `ActivateWithAnimation()`
- **No side effects** — a method must do only what its name advertises. A `GetSpeed()` method must not modify state. A `Calculate()` method must not trigger events. Query methods read; command methods write. Never both in one method.

### Documentation Requirements

- **All public methods** MUST have XML doc comments (`/// <summary>`)
- **All `[SerializeField]` fields** MUST have `[Tooltip("description")]`
- **All classes** MUST have a `/// <summary>` doc comment explaining their role
- **Complex algorithms** MUST have inline comments explaining the physics/math reasoning
- Comments explain **WHY**, not WHAT — don't write `// increment counter` for `count++`

### Code Style

- **Guard clauses first** — validate inputs and return early, don't nest the happy path
- **One blank line** between methods
- **Two blank lines** between logical sections (Fields / Lifecycle / Physics / Public API)
- **`#region`** ONLY for collapsing Unity lifecycle groups — never for hiding complexity
- **No commented-out code** — delete it, git has history
- **No TODO comments without an issue number** — `// TODO(#42): implement surface detection`

### File Structure Order

Every C# file follows this order:

```csharp
// 1. Using statements
// 2. Namespace declaration
// 3. Class doc comment
// 4. Class declaration
//    a. Constants (const, static readonly)
//    b. Enums and nested types
//    c. Serialized fields ([SerializeField], [Header])
//    d. Public properties
//    e. Private fields
//    f. Unity lifecycle methods (Awake → OnDestroy, in execution order)
//    g. Public methods (API surface)
//    h. Private methods (implementation details)
```

---

## 6. Physics Code Rules

> **R8EO-X is a physics simulation.** These rules are non-negotiable.

### Execution Context

- **ALL physics in FixedUpdate()** — forces, raycasts, velocity queries, state updates
- **NEVER manipulate velocity directly** — use `AddForce()`, `AddForceAtPosition()`, `AddTorque()`
- **Exception:** Teleportation/reset operations (like flip recovery) may set velocity to zero
- **Use `Time.fixedDeltaTime`** in all FixedUpdate code — never `Time.deltaTime`

### Force Application

- Forces via `Rigidbody.AddForceAtPosition(force, worldPoint)` for wheel forces
- Torques via `Rigidbody.AddTorque(torque)` for air physics
- **Document force units** in comments: Newtons (N) for forces, Newton-metres (N·m) for torques
- **Center of mass** set explicitly — never rely on Unity's auto-calculation

### Suspension Model

- **Hooke's law:** `F_spring = stiffness × compression`
- **Damping:** `F_damp = damping × (previousLength - currentLength) / deltaTime`
- **Total:** `F_suspension = max(F_spring + F_damp, 0)` — suspension NEVER pulls (no tension)
- **Bump stop:** `springLength = max(springLength, minSpringLength)` — prevents chassis clip-through

### Tire Grip Model

- **Curve-sampled slip ratio** — use `AnimationCurve` to map slip → grip factor
- **NOT Pacejka** — Pacejka is overcomplicated for 1/10th scale RC; curve sampling gives enough control
- **Lateral force:** `F = -sideDirection × lateralVelocity × gripFactor × gripCoeff × gripLoad`
- **gripLoad** derived from spring force, clamped to `maxSpringForce`

### Ground Detection

- **Raycasts from wheel anchors** — not colliders, not WheelCollider
- **Validate contact normals** — reject normals pointing downward (`normal.y < 0`)
- **Airborne threshold** — require N consecutive frames off-ground before declaring airborne (prevents terrain undulation pops)

### Determinism

- All physics reads from `Rigidbody` state — never cache position/velocity across frames unless intentional
- Physics materials managed programmatically, not via inspector (enables tumble blending)

---

## 7. Testing Rules

### TDD Cycle (MANDATORY — No Steps May Be Skipped)

1. **Hypothesize** — identify the behavior to implement or bug to reproduce
2. **Write a failing test** — test specifies the expected behavior
3. **Run → confirm RED** — test must fail for the expected reason
4. **Implement** — minimum code to make the test pass
5. **Run → confirm GREEN** — test must now pass
6. **Commit** — test + implementation together

> A test that was never run proves nothing. An implementation never verified against a test is not done.

### Test Organization

| Assembly | Location | Purpose | Runs |
|----------|----------|---------|------|
| `R8EOX.Tests.EditMode` | `Assets/Tests/EditMode/` | Pure logic: math, formulas, state machines | In editor, no Play mode |
| `R8EOX.Tests.PlayMode` | `Assets/Tests/PlayMode/` | Runtime behavior: MonoBehaviour, scenes, signals | Requires Play mode |

### Test Naming

```csharp
[Test]
public void ComputeSuspension_FullCompression_ReturnsMaxForce() { }

[Test]
public void ApplyGroundDrive_ThrottleAtMaxSpeed_ReturnsZeroForce() { }

[Test]
public void Distribute_OneWheelAirborne_AllForceToGroundedWheel() { }
```

Pattern: `MethodUnderTest_InputCondition_ExpectedOutput`

### Coverage Requirements

- **100% coverage on physics formulas** — suspension, grip, drivetrain, air physics math
- **100% coverage on state machines** — reverse ESC, trigger detection, tumble detection
- **Integration tests** for system wiring — verify that systems connect and communicate correctly
- When in doubt, write both unit AND integration tests

### Test Rules

- **No `[SetUp]` methods with complex logic** — keep tests independent and self-contained
- **No mocks for physics** — test the actual formulas with known inputs → expected outputs
- **Mirror named constants** — if code uses `k_AirborneThreshold = 5`, the test should reference the same constant
- **Test edge cases explicitly** — zero input, max input, boundary values, sign flips

---

## 8. Error Handling

- **`Debug.Assert(condition, message)`** — for programmer errors (should never happen in production)
- **`Debug.LogError(message)`** — for unrecoverable runtime errors (missing references, corrupt state)
- **`Debug.LogWarning(message)`** — for degraded but functional states (fallback behavior triggered)
- **`Debug.Log(message)`** — for significant state changes during development (motor preset applied, trigger mode detected)
- **NEVER silently swallow exceptions** — if you catch, you must log
- **Validate inspector references in Awake()** with null checks and clear error messages
- **Use `[RequireComponent]`** when a script depends on a sibling component
- **Prefix log messages** with `[ClassName]` for filtering: `Debug.Log("[RCCar] Motor=17.5T")`

---

## 9. Git & Commit Rules

### Commit Format

```
type: short description (imperative mood, lowercase)
```

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`, `perf`, `style`, `build`

### Rules

- **One logical change per commit** — don't mix a bugfix with a refactor
- **Commit test + implementation together** — or test first if it's independent
- **Commit every file immediately** after creating or editing it — never leave uncommitted changes
- **Never commit on main** — always use feature branches via worktrees
- **Never use `--no-verify`** — fix the hook failure instead

---

## 10. Documentation Rules

### CLAUDE.md Files

- **Every non-hidden directory** has a `CLAUDE.md` describing its contents
- **Update docs in the same commit** as code changes — if you add a file, add it to the directory's CLAUDE.md
- **Remove entries** for deleted files — no "removed" comments
- **Skills live in `.agents/skills/`** — reference them from CLAUDE.md, don't duplicate content

### Progressive Disclosure

Documentation is layered for efficient context loading:

| Level | Location | Content | When to Read |
|-------|----------|---------|-------------|
| 0 | `CLAUDE.md` files | Quick summary + file listing | Always (auto-loaded) |
| 1 | `.ai/knowledge/architecture/` | System architecture, standards, ADRs | When understanding system design |
| 2 | `.ai/knowledge/plans/` | Step-by-step implementation plans | When building new features |
| 3 | `.agents/skills/` | Deep technology reference | When implementing specific patterns |

---

## 11. DRY / Declarative Patterns

### When to Extract

- **3+ instances** of the same pattern → extract a helper
- **200+ lines** of switch/if chains → use data-driven approach
- **10+ signal connections** → use a wiring table
- **5+ setup methods** → consider a subsystem registry

### Adding new items should require 1 data entry, not touching multiple files

| Pattern | When to Use |
|---------|-------------|
| ScriptableObject → Renderer | Configuration-driven UI or behavior |
| Lookup Dictionary | Multi-target property routing |
| Event Wiring Table | Connecting many events between systems |
| Subsystem Registry | Orchestrators that init many subsystems |

---

## Quick Reference Card

```
Naming:     _private, Public, k_Constant, s_Static, IInterface
Bools:      Verb-prefixed — is/has/was/should/can (fields, props, locals, return types)
Events:     OnDoorOpening (before), OnDoorOpened (after)
Files:      One class per file, name matches class
Namespace:  R8EOX.{Folder} — always declared
Lifecycle:  Awake(self) → Start(cross) → FixedUpdate(physics) → Update(input) → LateUpdate(camera)
Physics:    FixedUpdate only, AddForce only, document units (N, N·m)
Testing:    TDD mandatory, RED → GREEN → COMMIT, 100% physics coverage
Constants:  No bare numbers, group in static classes
Methods:    ≤30 lines, ≤4 params, ≤3 nesting, guard clauses first, no flag params, no side effects
Classes:    ≤300 lines, one responsibility, composition over inheritance
Docs:       XML comments on public API, [Tooltip] on [SerializeField]
Git:        Conventional commits, one change per commit, never on main
```