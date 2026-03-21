---
name: unity-testing-patterns
description: Unity Testing Patterns
---


# Unity Testing Patterns

Use this skill when writing unit tests, integration tests, or PlayMode tests using the Unity Test Framework and NUnit, following the project's TDD red-green-commit cycle.

## Minimum Coverage Requirements (MANDATORY)

> **Every public method/function MUST have at minimum 1 positive + 1 negative test.** This is non-negotiable.

| Level | What | Minimum | Where |
|-------|------|---------|-------|
| **Unit** | Every public method/function touched or added | **1 positive + 1 negative per method** (minimum 2) | `Assets/Tests/EditMode/` |
| **Integration** | Every cross-class/cross-system interaction | **1 per interaction path** | `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/` |
| **E2E (PlayMode)** | Every user-facing feature or behavior change | **1 per feature/behavior** | `Assets/Tests/PlayMode/` |

- **Positive test:** Verifies correct behavior with valid input (happy path)
- **Negative test:** Verifies correct handling of invalid/edge/boundary input (zero, null, out-of-range, NaN)
- **Test naming:** `MethodName_Scenario_ExpectedOutcome` — must read like a sentence
- **Pre-implementation:** Tests MUST be written by a separate black-box agent before implementation begins. See `.agents/skills/ask-first/SKILL.md` Phase 2.

## Parameterized Tests

```csharp
[TestCase(100, 30, 70)]
[TestCase(100, 100, 0)]
[TestCase(100, 0, 100)]
[TestCase(50, 50, 0)]
public void TakeDamage_ReducesCorrectAmount(int maxHp, int damage, int expected)
{
    var stats = new PlayerStats(maxHealth: maxHp);
    stats.TakeDamage(damage);
    Assert.AreEqual(expected, stats.CurrentHealth);
}

[TestCase("Sword", 10, 2f, 20f)]
[TestCase("Bow", 5, 3f, 15f)]
public void DPS_Calculation(string name, int damage, float rate, float expectedDps)
{
    var weapon = ScriptableObject.CreateInstance<WeaponData>();
    weapon.damage = damage;
    weapon.fireRate = rate;

    Assert.AreApproximatelyEqual(expectedDps, weapon.DPS, 0.01f);

    Object.DestroyImmediate(weapon);
}
```

## Testing Async and Coroutines

```csharp
[UnityTest]
public IEnumerator Ability_Cooldown_BlocksReuse()
{
    var ability = new GameObject().AddComponent<FireballAbility>();
    ability.cooldownDuration = 1f;

    // First use should succeed
    bool firstUse = ability.TryActivate();
    Assert.IsTrue(firstUse);

    // Immediate second use should fail (on cooldown)
    bool secondUse = ability.TryActivate();
    Assert.IsFalse(secondUse);

    // Wait for cooldown
    yield return new WaitForSeconds(1.1f);

    // Now it should work again
    bool thirdUse = ability.TryActivate();
    Assert.IsTrue(thirdUse);

    Object.Destroy(ability.gameObject);
}
```

## Code Coverage

Install: `com.unity.testtools.codecoverage`

Enable in Window > Analysis > Code Coverage:
- Enable Code Coverage recording
- Select assemblies to include (e.g., `Game.Runtime`)
- Run tests
- Generate HTML report

## TDD Cycle Summary

1. **Write a failing test** — describe the desired behavior
2. **Run the test** — confirm it fails (RED) for the right reason
3. **Implement** — write the minimum code to pass
4. **Run the test** — confirm it passes (GREEN)
5. **Refactor** — clean up while tests stay green
6. **Commit** — test + implementation together

Never skip running the tests. A test that was never executed proves nothing. An implementation that was never verified is not done.

## Common Pitfalls

| Issue | Solution |
|-------|----------|
| `MissingReferenceException` in teardown | Use `Object.Destroy`, not direct null. Check for destroyed objects with `obj == null` (Unity overloads `==`). |
| Test passes alone, fails in batch | Tests share state — ensure [SetUp]/[TearDown] clean everything. Static fields are dangerous. |
| Physics not working in Play Mode test | Wait for `WaitForFixedUpdate()`, not just `yield return null`. Physics runs in FixedUpdate. |
| `NullReferenceException` in `Awake()`/`Start()` | AddComponent triggers Awake immediately. Set dependencies before AddComponent or use Initialize pattern. |
| Coroutine tests hang | [UnityTest] required for coroutines. Regular [Test] cannot yield. Set reasonable timeouts. |
| Tests slow due to scene loading | Prefer constructing minimal test objects over loading full scenes. Reserve scene tests for integration. |
| Flaky timing tests | Use `yield return new WaitUntil(() => condition)` with a timeout instead of `WaitForSeconds`. |


## Topic Pages

- [Test Framework Setup](skill-test-framework-setup.md)
- [CI Integration](skill-ci-integration.md)

