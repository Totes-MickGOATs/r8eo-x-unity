# Events and Delegates

> Part of the `unity-csharp-mastery` skill. See [SKILL.md](SKILL.md) for the overview.

## Events and Delegates

```csharp
// Pattern 1: C# event with Action (RECOMMENDED for code-to-code)
public class Health : MonoBehaviour
{
    public event Action<int> OnDamaged;        // int = damage amount
    public event Action OnDeath;

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;
        OnDamaged?.Invoke(amount);
        if (_currentHealth <= 0) OnDeath?.Invoke();
    }
}

// Pattern 2: UnityEvent (for Inspector wiring -- designers drag-drop handlers)
[System.Serializable]
public class DamageEvent : UnityEvent<int> { }

public class Health : MonoBehaviour
{
    [SerializeField] private DamageEvent _onDamaged; // Configurable in Inspector
    [SerializeField] private UnityEvent _onDeath;
}

// Pattern 3: Static events (global, no reference needed)
public static class GameEvents
{
    public static event Action<int> OnScoreChanged;
    public static event Action OnGameOver;

    public static void ScoreChanged(int newScore) => OnScoreChanged?.Invoke(newScore);
    public static void GameOver() => OnGameOver?.Invoke();
}
```

**When to use which:**
- `Action`/`event` -- code-to-code, best performance, compile-time safety
- `UnityEvent` -- when designers need Inspector wiring, or for prefab configuration
- Static events -- truly global events (score, game state), but harder to test

**Always unsubscribe** in `OnDisable` or `OnDestroy`:
```csharp
private void OnEnable() => _health.OnDeath += HandleDeath;
private void OnDisable() => _health.OnDeath -= HandleDeath;
```

