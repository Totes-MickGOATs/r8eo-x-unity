---
name: unity-csharp-mastery
description: Unity C# Mastery
---


# Unity C# Mastery

Use this skill when writing C# in Unity and you need guidance on naming conventions, memory management, async patterns, serialization, or Unity-specific C# pitfalls.

## The Null Check Trap

Unity overrides `== null` for Object-derived types. A destroyed object is "fake null" -- the C# reference still exists but Unity considers it null.

```csharp
// Setup
Destroy(gameObject);

// Next frame...
// obj is "fake null" -- C# reference exists, Unity says it's destroyed

if (obj == null) { }       // TRUE -- Unity's overloaded == catches destroyed objects
if (obj != null) { }       // FALSE -- correct
if (obj is null) { }       // FALSE! -- bypasses Unity's == operator, checks C# reference only
if (obj is not null) { }   // TRUE! -- WRONG, thinks destroyed object is alive
if (obj?.DoThing()) { }    // NullReferenceException! ?. bypasses Unity's null check

// CORRECT patterns
if (obj) { }               // Best -- implicit bool operator, handles destroyed objects
if (!obj) { }              // Best -- negated

// WRONG patterns (bypass Unity null check)
obj?.Method();             // Dangerous -- calls Method on destroyed object
obj ??= fallback;          // Dangerous -- never triggers for destroyed objects
var x = obj ?? fallback;   // Dangerous
```

**Rule:** For any Unity Object (GameObject, Component, ScriptableObject), use `if (obj)` / `if (!obj)`. Reserve `?.` and `??` for pure C# types (strings, structs, POCOs).

## readonly vs const

```csharp
// const: compile-time only, inlined at call sites, limited to primitives and strings
private const float Gravity = -9.81f;
private const string PlayerTag = "Player";

// readonly: runtime immutable, can be any type, set in declaration or constructor
private readonly List<Enemy> _pool = new();
private readonly Color _highlightColor = new(1f, 0.8f, 0f);

// static readonly: shared across instances, set once
private static readonly int AnimSpeedHash = Animator.StringToHash("Speed");
private static readonly WaitForSeconds WaitHalf = new(0.5f);
```

**Rule:** Use `const` for primitive literals that will never change. Use `static readonly` for computed values, object instances, or values that might change between builds.

**Gotcha:** `const` values are baked into consuming assemblies at compile time. If you change a `const` in Assembly A, Assembly B still sees the old value until recompiled. `static readonly` does not have this problem.

## Attribute Quick Reference

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[SerializeField]` | Expose private field to Inspector | `[SerializeField] private float _speed;` |
| `[RequireComponent]` | Auto-add component dependency | `[RequireComponent(typeof(Rigidbody))]` |
| `[DisallowMultipleComponent]` | Prevent duplicates | Class-level attribute |
| `[ExecuteInEditMode]` | Run in editor (legacy) | Use `[ExecuteAlways]` instead |
| `[CreateAssetMenu]` | Add SO to Create menu | See ScriptableObjects skill |
| `[AddComponentMenu]` | Custom component menu path | `[AddComponentMenu("Game/Player")]` |
| `[DefaultExecutionOrder]` | Script execution priority | `[DefaultExecutionOrder(-100)]` |
| `[SelectionBase]` | Click selects this in hierarchy | Class-level, useful for root objects |
| `[ContextMenu]` | Add right-click action in Inspector | `[ContextMenu("Reset Stats")]` |


## Topic Pages

- [Naming Conventions](skill-naming-conventions.md)
- [Events and Delegates](skill-events-and-delegates.md)
- [Common Anti-Patterns](skill-common-anti-patterns.md)

