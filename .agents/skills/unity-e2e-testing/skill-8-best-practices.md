# 8. Best Practices

> Part of the `unity-e2e-testing` skill. See [SKILL.md](SKILL.md) for the overview.

## 8. Best Practices

### Deterministic Testing

```csharp
[UnitySetUp]
public IEnumerator SetupDeterministic()
{
    // Fixed random seed
    Random.InitState(42);

    // Fixed frame rate
    Application.targetFrameRate = 60;
    QualitySettings.vSyncCount = 0;

    // Fixed physics timestep
    Time.fixedDeltaTime = 1f / 60f;

    yield return null;
}
```

### Flaky Test Mitigation

| Cause | Fix |
|-------|-----|
| **Timing** | `WaitUntil(condition)` with timeout, never bare `WaitForSeconds` |
| **Physics** | Wait for velocity < threshold, not fixed delay |
| **Async ops** | `yield return` the operation, check completion flag |
| **Shared state** | Reset singletons in `[TearDown]`, use `InputTestFixture` |
| **Scene leaks** | Destroy objects in `[TearDown]`, load empty scene between tests |
| **Render diff** | Perceptual diff with threshold, pin GPU/driver in CI |

### Test Scene Design

- Create **minimal test scenes** with only needed systems
- Avoid full production scenes (slow, brittle, hard to maintain)
- Use prefab-based setup: instantiate only what the test needs
- Scene validation tests: verify required components exist

### What NOT to E2E Test

- Pure math (unit test instead)
- Individual component behavior (integration test instead)
- Platform-specific rendering (visual test with per-platform baselines)
- Network latency tolerance (mock the network layer)

---

