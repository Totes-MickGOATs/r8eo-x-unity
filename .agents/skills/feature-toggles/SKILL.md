---
name: feature-toggles
description: Feature Toggles
---

# Feature Toggles

Control feature visibility and availability through compile-time and runtime toggles. Use this skill when merging partial features, gating debug tools, or enabling gradual rollout.

## When to Use

- Merging partial features that aren't player-visible yet
- Gating debug/development tools behind defines
- A/B testing or gradual rollout (future)

---

## Two Tiers of Toggles

### Tier 1: Compile-Time (Scripting Define Symbols)

Compile-time toggles use C# preprocessor directives to include or exclude code at build time.

**Pattern:**

```csharp
#if R8EOX_FEATURENAME
// Feature code here — only compiled when the define is active
#endif
```

**Registration:**
- **Gate an entire assembly:** Add the define to the assembly definition's `defineConstraints` array. The assembly is only compiled when all constraints are satisfied.
- **Gate individual code blocks:** Use `#if` / `#endif` guards around specific sections within any script.

**Enable:** Project Settings > Player > Scripting Define Symbols. Add the define symbol (e.g., `R8EOX_AUDIT`) and click Apply.

**Existing example:** `R8EOX_AUDIT` — gates the entire `R8EOX.Debug.Audit` assembly via `defineConstraints` in `Assets/Scripts/Debug/Audit/R8EOX.Debug.Audit.asmdef`.

**Naming convention:** `R8EOX_<FEATURE_NAME>` (all caps, underscore separated).

**Best for:** Entire subsystems, debug tools, experimental systems.

| Pros | Cons |
|------|------|
| Zero runtime cost | Requires recompilation to toggle |
| Dead code elimination by compiler | Cannot toggle at runtime |
| Clean separation of experimental code | Must rebuild to test both paths |

### Tier 2: Runtime (ScriptableObject Flags)

Runtime toggles use a ScriptableObject asset with boolean fields, accessible via a singleton pattern.

**Pattern:**

```csharp
// Access a flag at runtime
if (FeatureFlags.Instance.EnableReplaySystem)
{
    // Feature code here — always compiled, conditionally executed
}
```

**Access:** `FeatureFlags.Instance.<FlagName>` via singleton pattern.

**Best for:** Features that need runtime toggling, playtesting, demo builds.

| Pros | Cons |
|------|------|
| Toggle without recompilation | Minor runtime cost (branch per check) |
| Per-build configuration via SO assets | Code is always compiled (no dead code elimination) |
| Editable in Unity Inspector | Requires ScriptableObject infrastructure |

---

## Decision Matrix

| Criteria | Compile-Time | Runtime |
|----------|-------------|---------|
| Gate entire assembly | Yes | No |
| Toggle during play | No | Yes |
| Zero runtime cost | Yes | No |
| Per-build config | Via defines | Via SO asset |
| Dead code elimination | Yes | No |
| Inspector editable | No | Yes |

---

## Implementation Checklist

When adding a new feature toggle:

1. **Choose tier** based on decision matrix above
2. **Follow naming convention:** `R8EOX_<NAME>` (all caps, underscore separated)
3. **For compile-time:** Add `#if` guards in code or `defineConstraints` in assembly definition
4. **For runtime:** Add a `bool` field to the `FeatureFlags` ScriptableObject
5. **Document the toggle** in the Active Toggles Registry below
6. **Set toggle to OFF by default** for new features
7. **Plan removal** — set a target version or date for toggle removal once the feature is stable

---

## Active Toggles Registry

| Toggle | Tier | Purpose | Status |
|--------|------|---------|--------|
| `R8EOX_AUDIT` | Compile-Time | Gates physics conformance audit assembly | Active |

---

## Toggle Hygiene

- **Toggles are temporary** — every toggle should have a planned removal date or target version
- **Review toggles quarterly** — remove any that have been "on" in all builds for more than one release cycle
- **Never nest toggles** — no `#if A` inside `#if B`; this creates exponential test paths
- **Test both paths** — verify the feature works when toggled ON and that nothing breaks when toggled OFF
- **Remove promptly** — once a feature is stable and fully integrated, delete the toggle and its guards (toggle debt accumulates like tech debt)

---

## Related Skills

| Skill | How It Integrates |
|-------|-------------------|
| `unity-project-foundations` | Assembly definitions and `defineConstraints` for compile-time gating |
| `unity-scriptable-objects` | ScriptableObject patterns for runtime flag assets |
| `physics-conformance-audit` | Uses `R8EOX_AUDIT` compile-time toggle as the canonical example |
| `branch-workflow` | Feature branches for toggle introduction and removal PRs |
