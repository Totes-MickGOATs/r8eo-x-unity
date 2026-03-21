# Coding Standards — Naming & Architecture Rules

Part of the [Coding Standards](./coding-standards.md) reference.

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
| Static fields | `s_PascalCase` | `s_Instance` (rare) |
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
- Applies to fields, properties, locals, and parameters: `_isDead`, `_hasStartedTurn`, `IsGameOver`
- Applies to methods returning `bool` too: `IsNewPosition()`, `HasReachedTarget()`, `CanAccelerate()`

### Event Timing Convention

- **Present participle = before it happens** (pre-event): `OnDoorOpening`, `OnLapStarting`
- **Past participle = after it happened** (post-event): `OnDoorOpened`, `OnLapCompleted`

### Forbidden Patterns

- **No Hungarian notation** — never `fSpeed`, `bIsActive`, `iCount`
- **No `m_` prefix** — use `_camelCase` for private fields
- **No abbreviations** unless universally understood (`rb` for Rigidbody is OK)
- **No single-letter variables** except `i`, `j`, `k` in loops and `x`, `y`, `z` for coordinates

---

## 2. Architecture Rules

### Signal Up, Call Down

- Children/subsystems **raise events** (C# `event Action`) to notify parents
- Parents/orchestrators **call methods** on children to command them
- **NEVER** reverse this: a child must not call methods on its parent or siblings directly

### Composition over Inheritance

- **Maximum inheritance depth: 2** (Base → Concrete). No deeper hierarchies.
- Prefer interfaces over abstract base classes
- Use component composition: attach multiple focused MonoBehaviours, not one god class

### Namespace Rules

Every C# file MUST declare a namespace matching its folder path:

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

- One `.asmdef` per major system folder
- Test assemblies: `R8EOX.Tests.EditMode.asmdef`, `R8EOX.Tests.PlayMode.asmdef`
- Assemblies declare explicit references — no implicit "reference everything"
- Editor-only code in assemblies with `Editor` platform constraint

### Class Responsibility Rules

| Class Type | Use For | NOT For |
|-----------|---------|---------|
| MonoBehaviour | Unity lifecycle, inspector fields, coroutines | Pure math, data models, algorithms |
| Plain C# class | Physics formulas, math helpers, state machines | Anything needing Update/Awake |
| ScriptableObject | Configuration data, presets, shared constants | Runtime mutable state |
| Struct | Small immutable data (< 5 fields), physics results | Large data, reference semantics |
| Static class | Pure utility functions, extension methods, constants | Mutable state, initialization |

### Forbidden Patterns

- **No singletons** — use serialized references or event-based communication
- **No static mutable state** — `static readonly` constants are OK; `static int counter` is NOT
- **No `Find()` / `FindObjectOfType()`** — use `[SerializeField]` references
- **No string-based anything** — no `Invoke("method")`, no `GameObject.Find("name")`
- **No `SendMessage()` / `BroadcastMessage()`** — use direct method calls or events
- **No god classes** — if a class does more than one thing, split it
