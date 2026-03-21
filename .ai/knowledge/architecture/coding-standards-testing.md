# Coding Standards — Testing, Error Handling & Git Rules

Part of the [Coding Standards](./coding-standards.md) reference.

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
```

Pattern: `MethodUnderTest_InputCondition_ExpectedOutput`

### Coverage Requirements

- **100% coverage on physics formulas** — suspension, grip, drivetrain, air physics math
- **100% coverage on state machines** — reverse ESC, trigger detection, tumble detection
- **Integration tests** for system wiring

### Test Rules

- **No `[SetUp]` methods with complex logic** — keep tests independent and self-contained
- **No mocks for physics** — test the actual formulas with known inputs → expected outputs
- **Mirror named constants** — test should reference same constants as implementation
- **Test edge cases explicitly** — zero input, max input, boundary values, sign flips

---

## 8. Error Handling

- **`Debug.Assert(condition, message)`** — for programmer errors (should never happen in production)
- **`Debug.LogError(message)`** — for unrecoverable runtime errors
- **`Debug.LogWarning(message)`** — for degraded but functional states
- **`Debug.Log(message)`** — for significant state changes during development
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
- **Commit test + implementation together**
- **Commit every file immediately** after creating or editing it
- **Never commit on main** — always use feature branches via worktrees
- **Never use `--no-verify`** — fix the hook failure instead

---

## 10. Documentation Rules

- **Every non-hidden directory** has a `CLAUDE.md` describing its contents
- **Update docs in the same commit** as code changes
- **Skills live in `.agents/skills/`** — reference them from CLAUDE.md, don't duplicate

### Progressive Disclosure

| Level | Location | Content | When to Read |
|-------|----------|---------|-------------|
| 0 | `CLAUDE.md` files | Quick summary + file listing | Always (auto-loaded) |
| 1 | `.ai/knowledge/architecture/` | System architecture, standards, ADRs | Understanding system design |
| 2 | `.ai/knowledge/plans/` | Step-by-step implementation plans | Building new features |
| 3 | `.agents/skills/` | Deep technology reference | Implementing specific patterns |

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
