# Deterministic Physics

> Part of the `unity-physics-tuning` skill. See [SKILL.md](SKILL.md) for the overview.

## Deterministic Physics

### What Enhanced Determinism Does

When enabled (`Physics.enhancedDeterminism = true` or via Project Settings):
- Same inputs + same initial state = same simulation result **on the same machine, same build**
- PhysX processes bodies in a deterministic order
- Floating-point operations use consistent ordering

### What It Does NOT Guarantee

- **Cross-platform determinism** -- x86 vs ARM, different GPU drivers, or different Unity versions may diverge
- **Cross-build determinism** -- Debug vs Release, IL2CPP vs Mono may differ due to floating-point optimization flags
- **Scene load order independence** -- objects must be instantiated in the same order

### Requirements for Deterministic Replay

1. Enhanced Determinism ON
2. `SimulationMode.Script` -- manual `Physics.Simulate` calls
3. Fixed timestep (no `Time.deltaTime` in physics code)
4. Identical object creation order
5. No `Destroy()` during replay -- disable instead
6. Record/replay all external inputs (steering, throttle, collisions)

---

