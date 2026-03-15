# feature-toggles/

Compile-time and runtime feature toggle patterns for gating partial features, debug tools, and experimental systems.

## Files

| File | Role |
|------|------|
| `SKILL.md` | Toggle tiers, decision matrix, implementation checklist, active registry, hygiene rules |

## When to Use

- Merging partial features that aren't player-visible yet
- Gating debug/development tools behind scripting define symbols
- Runtime feature flags via ScriptableObject for playtesting or demo builds

## Related Skills

- **`unity-project-foundations`** — Assembly definitions and `defineConstraints`
- **`unity-scriptable-objects`** — ScriptableObject patterns for runtime flags
- **`physics-conformance-audit`** — Canonical example of `R8EOX_AUDIT` compile-time toggle
