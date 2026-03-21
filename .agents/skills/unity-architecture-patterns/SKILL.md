---
name: unity-architecture-patterns
description: Unity Design Patterns
---


# Unity Design Patterns

Use this skill when choosing, implementing, or combining design patterns in Unity. Covers SOLID principles and 11 patterns with RC racing examples, Unity-specific implementation details, advanced combination techniques, and guidance on when each pattern helps vs. when it is overkill.

---

## SOLID Principles

Design patterns are most effective when grounded in SOLID. These five principles guide when and how to apply patterns in Unity.

> "Don't force principles into your scripts for the sake of it. Let them organically work into place through necessity." Also keep KISS and DRY in mind -- apply SOLID where it reduces complexity, not where it adds ceremony.

### Single Responsibility

Each class/MonoBehaviour is responsible for one thing. A `RaceManager` manages race state -- it does not also handle UI updates or audio. If a class has multiple reasons to change, split it.

### Open-Closed

Classes open for extension, closed for modification. Use ScriptableObjects and interfaces to add new behavior without modifying existing code. Example: new surface types via `SurfaceData` SO assets, not by adding cases to a switch statement in the physics system.

### Liskov Substitution

Derived classes must be substitutable for their base. If `Vehicle` has a `Drive()` method, every subclass must honor that contract. Do not throw `NotImplementedException` in overrides -- that violates the contract callers depend on.

### Interface Segregation

Do not force classes to implement methods they do not use. Split large interfaces: `IDamageable`, `IRepairable`, `IResettable` -- not one `IVehicle` with 20 methods. Each interface should represent a single capability.

### Dependency Inversion

High-level modules depend on abstractions, not concrete implementations. Inject dependencies via constructor, `[SerializeField]`, or interfaces -- never `FindObjectOfType`. This is why our project forbids singletons and `Find()` calls (see coding standards).

---

## Pattern Catalog

| Pattern | Purpose | RC Racing Example |
|---------|---------|-------------------|
| Observer | Decouple event senders from receivers | `OnLapComplete`, `OnCheckpointReached` |
| State | Manage complex object states with transitions | Vehicle: Idle, Accelerating, Braking, Airborne, Crashed |
| Factory | Create objects without exposing concrete types | Track obstacle spawning, vehicle configuration |
| Command | Enable undo/redo, replay, queued actions | Track editor undo/redo for obstacle placement |
| MVP | Separate UI from game logic | Race HUD presenter wiring model to view |
| MVVM | Automatic data binding for complex UIs | Settings screen, garage/tuning UI |
| Strategy | Swap algorithms at runtime | Surface physics: grip/friction per terrain type |
| Flyweight | Share data across many similar instances | Track segments: same geometry, different positions |
| Dirty Flag | Skip expensive recalculations | Recalculate race standings only when positions change |
| Object Pooling | Reuse objects to avoid GC spikes | Tire smoke particles, dust effects, sound effects |
| Singleton | **ANTI-PATTERN** -- globally accessible single instance | Shown for reference; use alternatives below |

---

## Source References

| Resource | URL |
|----------|-----|
| Free e-book (100 pages) | `https://unity.com/resources/games/level-up-your-code-with-game-programming-patterns` |
| GitHub sample project | `https://github.com/Unity-Technologies/game-programming-patterns-demo` |
| Unity Learn course (Unity 6) | `https://learn.unity.com/course/design-patterns-unity-6` |
| SOLID talk (Unite Austin 2017) | `https://www.youtube.com/watch?v=eIf3-aDTOOA` |

---

## Related Skills

| Skill | Relationship |
|-------|-------------|
| `unity-csharp-mastery` | C# lifecycle, attributes, naming conventions, anti-patterns |
| `unity-scriptable-objects` | Extended SO patterns: event channels, runtime sets, SO-based enums, delegate objects |
| `unity-state-machines` | Advanced FSM: hierarchical states, Animator integration |
| `unity-composition` | Component architecture, dependency injection, interfaces |
| `unity-performance-optimization` | Profiling, batching, GC reduction, LOD |
| `unity-testing-patterns` | Unit testing patterns for these architectures |
| `unity-project-foundations` | .asmdef setup, folder structure, YAML serialization, workflow optimization |
| `unity-3d-world-building` | NavMesh, terrain, level design, AI navigation |
| `unity-editor-scripting` | Custom inspectors for SO-heavy architectures |
| `unity-ui-toolkit` | UXML/USS for MVC/MVVM data binding |


## Topic Pages

- [Observer Pattern](skill-observer-pattern.md)
- [Quick Reference: Which Pattern Do I Need?](skill-quick-reference-which-pattern-do-i-need.md)

