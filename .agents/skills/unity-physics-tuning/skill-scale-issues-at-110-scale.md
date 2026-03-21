# Scale Issues at 1/10 Scale

> Part of the `unity-physics-tuning` skill. See [SKILL.md](SKILL.md) for the overview.

## Scale Issues at 1/10 Scale

Unity PhysX is tuned for human-scale (1 unit = 1 meter). At 1/10 scale, several defaults break.

### Problems and Fixes

| Problem | Cause | Fix |
|---------|-------|-----|
| Objects fall through floor | Contact offset too large relative to collider | Reduce `Physics.defaultContactOffset` to 0.005 |
| Vehicle sleeps while moving slowly | Sleep threshold too high for small velocities | Set `Rigidbody.sleepThreshold = 0.001` |
| Suspension oscillates wildly | Spring forces overshoot at default timestep | Use 200Hz timestep (0.005s) |
| Wheels clip through thin curbs | Discrete collision misses thin geometry | Use `ContinuousSpeculative` + SphereCast |
| Unrealistic bounce | Bounce threshold too low | Set `Physics.bounceThreshold = 0.5` |
| Angular velocity clamped | Default `maxAngularVelocity = 7` too low | Set to 50 on vehicle Rigidbody |

### Contact Offset

```csharp
// Global default
Physics.defaultContactOffset = 0.005f; // Half the default

// Per-collider override
collider.contactOffset = 0.003f; // Even smaller for wheel contact
```

---

