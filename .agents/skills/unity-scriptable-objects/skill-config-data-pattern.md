# Config Data Pattern

> Part of the `unity-scriptable-objects` skill. See [SKILL.md](SKILL.md) for the overview.

## Config Data Pattern

The simplest and most common use. Replace magic numbers with designer-editable assets.

```csharp
[CreateAssetMenu(menuName = "Game/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Health")]
    [Min(1)] public int MaxHealth = 100;
    [Range(0f, 1f)] public float ArmorReduction = 0.2f;

    [Header("Movement")]
    [Min(0)] public float MoveSpeed = 5f;
    [Min(0)] public float TurnSpeed = 120f;

    [Header("Combat")]
    [Min(0)] public float AttackRange = 2f;
    [Min(0)] public float AttackCooldown = 1.5f;
    [Min(1)] public int AttackDamage = 15;

    [Header("Detection")]
    [Min(0)] public float SightRange = 20f;
    [Range(0f, 360f)] public float SightAngle = 120f;
}

// Usage: multiple enemy prefabs can share or have unique configs
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyConfig _config;

    private int _currentHealth;

    private void Awake()
    {
        _currentHealth = _config.MaxHealth;
    }

    private void Update()
    {
        // Use _config.MoveSpeed, _config.SightRange, etc.
    }
}
```

**Key benefit:** Swap configs at runtime or in Inspector to create variants (fast zombie, tank zombie) without code changes or prefab duplication.

## Event Channel Pattern

ScriptableObjects as decoupled event buses. Publishers and subscribers reference the same SO asset -- neither needs to know about the other.

```csharp
// Base event channel (no arguments)
[CreateAssetMenu(menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private readonly List<Action> _listeners = new();

    public void Raise()
    {
        // Iterate backwards so listeners can safely unsubscribe during invocation
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i]?.Invoke();
        }
    }

    public void Subscribe(Action listener) => _listeners.Add(listener);
    public void Unsubscribe(Action listener) => _listeners.Remove(listener);
}

// Generic typed event channel
public class GameEvent<T> : ScriptableObject
{
    private readonly List<Action<T>> _listeners = new();

    public void Raise(T value)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            _listeners[i]?.Invoke(value);
        }
    }

    public void Subscribe(Action<T> listener) => _listeners.Add(listener);
    public void Unsubscribe(Action<T> listener) => _listeners.Remove(listener);
}

// Concrete typed events (needed for [CreateAssetMenu] -- generics can't have it)
[CreateAssetMenu(menuName = "Events/Int Event")]
public class IntEvent : GameEvent<int> { }

[CreateAssetMenu(menuName = "Events/Float Event")]
public class FloatEvent : GameEvent<float> { }

[CreateAssetMenu(menuName = "Events/String Event")]
public class StringEvent : GameEvent<string> { }
```

**Usage -- publisher side:**
```csharp
public class ScoreManager : MonoBehaviour
{
    [SerializeField] private IntEvent _onScoreChanged;  // Drag SO asset in Inspector

    private int _score;

    public void AddScore(int points)
    {
        _score += points;
        _onScoreChanged.Raise(_score);
    }
}
```

**Usage -- subscriber side:**
```csharp
public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private IntEvent _onScoreChanged;  // Same SO asset
    [SerializeField] private TMP_Text _scoreText;

    private void OnEnable() => _onScoreChanged.Subscribe(UpdateDisplay);
    private void OnDisable() => _onScoreChanged.Unsubscribe(UpdateDisplay);

    private void UpdateDisplay(int score)
    {
        _scoreText.text = $"Score: {score}";
    }
}
```

**Benefits over direct references:**
- ScoreManager does not know ScoreDisplay exists (and vice versa)
- Adding new listeners requires zero code changes to the publisher
- Works across scenes (SOs are project assets)
- Easy to test -- raise events manually in Inspector or tests

## Shared Variable Pattern

Runtime-writable state shared between systems without singletons or direct references.

```csharp
// Base: a ScriptableObject that holds a single value
[CreateAssetMenu(menuName = "Variables/Float Variable")]
public class FloatVariable : ScriptableObject
{
    [SerializeField] private float _initialValue;

    [System.NonSerialized] private float _runtimeValue;

    public float Value
    {
        get => _runtimeValue;
        set
        {
            if (Mathf.Approximately(_runtimeValue, value)) return;
            _runtimeValue = value;
            OnValueChanged?.Invoke(_runtimeValue);
        }
    }

    public event Action<float> OnValueChanged;

    private void OnEnable()
    {
        // Reset to initial value when entering play mode or loading
        _runtimeValue = _initialValue;
    }

#if UNITY_EDITOR
    // Allow editing in Inspector during play mode for debugging
    [SerializeField] private float _debugValue;

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            Value = _debugValue;
        }
    }
#endif
}

// Int variant
[CreateAssetMenu(menuName = "Variables/Int Variable")]
public class IntVariable : ScriptableObject
{
    [SerializeField] private int _initialValue;
    [System.NonSerialized] private int _runtimeValue;

    public int Value
    {
        get => _runtimeValue;
        set
        {
            if (_runtimeValue == value) return;
            _runtimeValue = value;
            OnValueChanged?.Invoke(_runtimeValue);
        }
    }

    public event Action<int> OnValueChanged;

    private void OnEnable() => _runtimeValue = _initialValue;
}
```

**Usage:**
```csharp
// Health system writes the variable
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private FloatVariable _playerHealthVar;
    [SerializeField] private float _maxHealth = 100f;

    private void Awake() => _playerHealthVar.Value = _maxHealth;

    public void TakeDamage(float amount)
    {
        _playerHealthVar.Value = Mathf.Max(0f, _playerHealthVar.Value - amount);
    }
}

// UI reads the variable (no reference to PlayerHealth)
public class HealthBar : MonoBehaviour
{
    [SerializeField] private FloatVariable _playerHealthVar;
    [SerializeField] private Image _fillImage;

    private void OnEnable() => _playerHealthVar.OnValueChanged += UpdateBar;
    private void OnDisable() => _playerHealthVar.OnValueChanged -= UpdateBar;

    private void UpdateBar(float health)
    {
        _fillImage.fillAmount = health / 100f;
    }
}
```

## Runtime Set Pattern

Track active instances without `FindObjectsOfType` or singletons.

```csharp
// Generic runtime set
public abstract class RuntimeSet<T> : ScriptableObject
{
    private readonly List<T> _items = new();

    public IReadOnlyList<T> Items => _items;
    public int Count => _items.Count;

    public void Add(T item)
    {
        if (!_items.Contains(item))
            _items.Add(item);
    }

    public void Remove(T item) => _items.Remove(item);

    private void OnDisable()
    {
        // Clear when exiting play mode to prevent stale references
        _items.Clear();
    }
}

// Concrete set for enemies
[CreateAssetMenu(menuName = "Runtime Sets/Enemy Set")]
public class EnemyRuntimeSet : RuntimeSet<Enemy> { }

// Enemies register/unregister themselves
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyRuntimeSet _enemySet;

    private void OnEnable() => _enemySet.Add(this);
    private void OnDisable() => _enemySet.Remove(this);
}

// Anything can query the set without knowing about individual enemies
public class EnemyRadar : MonoBehaviour
{
    [SerializeField] private EnemyRuntimeSet _enemySet;

    private void Update()
    {
        foreach (var enemy in _enemySet.Items)
        {
            // Draw radar blip for each active enemy
        }
    }
}
```

## Enum Replacement Pattern

Use SOs instead of enums when you need extensibility without recompilation.

```csharp
// Instead of: enum DamageType { Physical, Fire, Ice, Lightning }
[CreateAssetMenu(menuName = "Game/Damage Type")]
public class DamageType : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private Color _color = Color.white;
    [SerializeField] private GameObject _hitEffect;
    [SerializeField] private AudioClip _hitSound;

    public string DisplayName => _displayName;
    public Color Color => _color;
    public GameObject HitEffect => _hitEffect;
    public AudioClip HitSound => _hitSound;
}

// Usage -- weapon references a DamageType asset
public class Weapon : MonoBehaviour
{
    [SerializeField] private DamageType _damageType;
    [SerializeField] private int _baseDamage;

    public void Hit(IDamageable target)
    {
        target.TakeDamage(_baseDamage, _damageType);
    }
}

// Resistance system -- reference the same SO assets
public class Armor : MonoBehaviour
{
    [System.Serializable]
    public struct Resistance
    {
        public DamageType Type;
        [Range(0f, 1f)] public float Reduction;
    }

    [SerializeField] private Resistance[] _resistances;

    public float GetReduction(DamageType type)
    {
        foreach (var r in _resistances)
        {
            if (r.Type == type) return r.Reduction; // Reference comparison -- fast
        }
        return 0f;
    }
}
```

**Benefits over enums:** Adding a new damage type = create a new asset. No code changes, no recompilation, no switch statement updates.

