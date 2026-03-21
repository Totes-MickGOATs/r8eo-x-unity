# Coding Standards — Lifecycle, Physics & Code Organization

Part of the [Coding Standards](./coding-standards.md) reference.

---

## 3. MonoBehaviour Lifecycle Order

Methods MUST appear in this order within the class body:

```csharp
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
                    // Runs AFTER all Update() calls

void OnDisable()    // Unsubscribe from ALL events subscribed in OnEnable
                    // Prevents memory leaks and null reference exceptions

void OnDestroy()    // Final cleanup: dispose unmanaged resources, remove from registries
```

### Lifecycle Rules

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
| **Const** | `const` / `static readonly` | Algorithm logic, physics math, layer IDs | `k_WheelRadius = 0.166f` |
| **Config** | `[SerializeField]` or ScriptableObject | Per-instance tuning set in editor | `_springStrength`, motor presets |
| **Settings** | Settings manager / PlayerPrefs | User preferences persisted to disk | Graphics quality, audio volume |
| **Dynamic** | Runtime variable | Computed or changed every frame/event | `_forwardSpeed`, `_smoothThrottle` |

### Rules

- **NEVER use bare numeric literals in physics code** — extract to named `const` or `static readonly`
- **Group related constants** in dedicated static classes:
  - `PhysicsConstants` — wheel radius, mass, spring defaults
  - `CollisionLayers` — layer mask constants
  - `SurfaceTypes` — surface type enum and friction coefficients
- **Magic number test:** Only exceptions are `0`, `1`, `-1`, `0.5f`, `2f`, `Mathf.PI`.

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

- **No flag parameters** — don't pass a `bool` that changes a method's behavior. Create two named methods instead.
- **No side effects** — a method must do only what its name advertises. `GetSpeed()` must not modify state.

### Documentation Requirements

- **All public methods** MUST have XML doc comments (`/// <summary>`)
- **All `[SerializeField]` fields** MUST have `[Tooltip("description")]`
- **All classes** MUST have a `/// <summary>` doc comment explaining their role
- **Complex algorithms** MUST have inline comments explaining the physics/math reasoning
- Comments explain **WHY**, not WHAT

### Code Style

- **Guard clauses first** — validate inputs and return early, don't nest the happy path
- **One blank line** between methods; **Two blank lines** between logical sections
- **`#region`** ONLY for collapsing Unity lifecycle groups — never for hiding complexity
- **No commented-out code** — delete it, git has history
- **No TODO comments without an issue number** — `// TODO(#42): implement surface detection`

### File Structure Order

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
> ALL physics in FixedUpdate(). NEVER manipulate velocity directly — use `AddForce()`, `AddForceAtPosition()`, `AddTorque()`.
> Use `Time.fixedDeltaTime` in all FixedUpdate code. Exception: teleportation/reset may set velocity to zero.
> Forces via `Rigidbody.AddForceAtPosition(force, worldPoint)` for wheel forces. Document units: Newtons (N), Newton-metres (N·m).
> Center of mass set explicitly — never rely on Unity's auto-calculation.

### Suspension Model

- **Hooke's law:** `F_spring = stiffness × compression`
- **Damping:** `F_damp = damping × (previousLength - currentLength) / deltaTime`
- **Total:** `F_suspension = max(F_spring + F_damp, 0)` — suspension NEVER pulls
- **Bump stop:** `springLength = max(springLength, minSpringLength)` — prevents chassis clip-through

### Tire Grip Model

- **Curve-sampled slip ratio** — use `AnimationCurve` to map slip → grip factor
- **NOT Pacejka** — curve sampling gives enough control for 1/10th scale RC
- **Lateral force:** `F = -sideDirection × lateralVelocity × gripFactor × gripCoeff × gripLoad`

### Ground Detection

- **Raycasts from wheel anchors** — not colliders, not WheelCollider
- **Validate contact normals** — reject normals pointing downward (`normal.y < 0`)
- **Airborne threshold** — require N consecutive frames off-ground before declaring airborne
