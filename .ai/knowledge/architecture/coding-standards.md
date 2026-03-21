# R8EO-X Coding Standards

> **Every rule in these pages is MANDATORY.** No exceptions, no "we'll fix it later."
> Violations must be fixed before commit.

---

## Topic Pages

| Page | Sections |
|------|---------|
| [Naming & Architecture](./coding-standards-naming.md) | §1 Naming conventions, §2 Architecture rules (Signal Up/Call Down, namespaces, assemblies, forbidden patterns) |
| [Lifecycle, Physics & Code Organization](./coding-standards-lifecycle.md) | §3 MonoBehaviour lifecycle, §4 Value mutability tiers, §5 Code organization, §6 Physics code rules |
| [Testing, Error Handling & Git](./coding-standards-testing.md) | §7 TDD cycle, §8 Error handling, §9 Git rules, §10 Documentation rules, §11 DRY patterns |

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

---

## Key Constraints Summary

| Constraint | Limit |
|-----------|-------|
| Method length | 30 lines max |
| Class length | 300 lines max |
| Parameter count | 4 max |
| Nesting depth | 3 levels max |
| Inheritance depth | 2 max (Base → Concrete) |
| Physics in | FixedUpdate only |
| Input in | Update only |

## Forbidden in Runtime Assemblies

| Forbidden | Use Instead |
|-----------|------------|
| `Debug.Log*` | `RuntimeLog.Log/LogWarning/LogError` from `R8EOX.Shared` |
| `FindObjectOfType()` / `GameObject.Find()` | `[SerializeField]` or `Initialize()` injection |
| Singletons | Serialized references or event-based communication |
| Static mutable state | `static readonly` constants only |
| `SendMessage()` / `BroadcastMessage()` | Direct method calls or events |
