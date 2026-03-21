# PhysX Project Settings

> Part of the `unity-physics-tuning` skill. See [SKILL.md](SKILL.md) for the overview.

## PhysX Project Settings

Configure in **Edit > Project Settings > Physics**.

| Setting | Recommended Value | Why |
|---------|------------------|-----|
| Solver Type | **TGS (Temporal Gauss-Seidel)** | Better convergence for vehicle constraints than PGS |
| Default Solver Iterations | **10-12** | Higher = more stable joints/contacts. Below 8 causes jitter on suspensions |
| Default Solver Velocity Iterations | **4-6** | Controls velocity-level correction. 4 is minimum for stable wheel contact |
| Bounce Threshold | **0.5-1.0** | Velocities below this won't bounce. Prevents micro-bouncing on track surface |
| Default Contact Offset | **0.005-0.01** | Smaller than default (0.01) for 1/10 scale. Too small = missed contacts |
| Sleep Threshold | **0.001-0.005** | Lower than default (0.005) so small RC cars don't sleep prematurely |
| Default Max Angular Speed | **50** | RC cars spin fast; default 7 rad/s clips wheel rotation |
| Auto Sync Transforms | **Off** | Manual sync for performance; call `Physics.SyncTransforms()` when needed |
| Reuse Collision Callbacks | **On** | Reduces GC allocation from collision events |
| Enhanced Determinism | **On** (if replay/ghosts needed) | See Deterministic Physics section |

### Fixed Timestep

Set in **Edit > Project Settings > Time > Fixed Timestep**.

| Timestep | Hz | Use Case |
|----------|----|----------|
| 0.02 | 50 Hz | **Too coarse for vehicles.** Suspension oscillates, wheels tunnel through thin colliders |
| 0.01 | 100 Hz | **Minimum acceptable.** Stable for casual RC sim |
| 0.005 | 200 Hz | **Preferred for RC racing.** Smooth suspension, accurate tire contact, no tunneling |
| 0.002 | 500 Hz | Overkill for most cases. Only if doing sub-mm precision sim |

**Why 50 Hz fails for vehicles:** At 50 Hz each physics step moves a vehicle traveling 20 m/s by 0.4m. A thin curb collider (2cm) is completely skipped. Suspension springs with high stiffness overshoot in a single step, causing oscillation. RC cars at 1/10 scale compound this -- colliders are 10x thinner than full-scale.

```csharp
// Set via script if needed
Time.fixedDeltaTime = 0.005f; // 200 Hz
```

---

