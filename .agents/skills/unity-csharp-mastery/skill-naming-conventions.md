# Naming Conventions

> Part of the `unity-csharp-mastery` skill. See [SKILL.md](SKILL.md) for the overview.

## Naming Conventions

```csharp
// PascalCase: classes, methods, properties, events, enums, public fields
public class PlayerController : MonoBehaviour
{
    // camelCase with underscore prefix: private/protected fields
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private int _maxHealth = 100;

    private Rigidbody _rb;
    private bool _isGrounded;

    // PascalCase: properties
    public float MoveSpeed => _moveSpeed;
    public bool IsAlive { get; private set; }

    // PascalCase: methods
    public void TakeDamage(int amount) { }

    // camelCase: local variables, parameters
    private void CalculateVelocity(float deltaTime)
    {
        var currentSpeed = _rb.linearVelocity.magnitude;
    }
}

// PascalCase: enums and their members
public enum GameState { Menu, Loading, Playing, Paused, GameOver }

// Interfaces: prefix with I
public interface IDamageable { void TakeDamage(int amount); }

// Constants: PascalCase (NOT SCREAMING_SNAKE)
public const float MaxFallSpeed = -20f;
private const int PoolInitialSize = 10;
```

**Key rules:**
- `_camelCase` for all private/protected fields (with or without `[SerializeField]`)
- `PascalCase` for everything public-facing
- `var` for local variables when type is obvious from the right side
- No Hungarian notation (`strName`, `iCount`) -- Unity codebase does not use it

## MonoBehaviour Lifecycle

Execution order matters. Mistakes here cause race conditions and null references.

```
Awake()           -- Called once when object instantiates. Set up self-references.
  |                  Called even if script is disabled. Called before Start of ANY object.
OnEnable()        -- Called every time the object/component is enabled.
  |                  Subscribe to events here.
Start()           -- Called once before the first Update. Other objects' Awake() has run.
  |                  Safe to reference other objects here.
  |
  |-- [Physics loop] --
  |   FixedUpdate()   -- Fixed timestep (default 0.02s). Physics, raycasts, forces.
  |                      May run 0, 1, or multiple times per frame.
  |
  |-- [Game loop] --
  |   Update()        -- Once per frame. Input, game logic, timers.
  |   LateUpdate()    -- Once per frame, after all Update(). Camera follow, final adjustments.
  |
OnDisable()       -- Called when object/component is disabled. Unsubscribe events here.
OnDestroy()       -- Called when object is destroyed. Final cleanup.
```

**Critical rules:**
- `Awake` for self-init (`_rb = GetComponent<Rigidbody>()`). Never reference other objects.
- `Start` for cross-object setup. All `Awake` calls are guaranteed done.
- `OnEnable`/`OnDisable` are your subscribe/unsubscribe pair. Always symmetric.
- `FixedUpdate` for physics. Never apply forces in `Update`.
- `LateUpdate` for camera. Prevents jitter from reading transform after physics moves it.

```csharp
public class Enemy : MonoBehaviour
{
    private Health _health;
    private Rigidbody _rb;

    private void Awake()
    {
        // Self-references only
        _health = GetComponent<Health>();
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        _health.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        // Safe to reference singletons, managers, other objects
        GameManager.Instance.RegisterEnemy(this);
    }
}
```

## Coroutines vs Async/Await

### Coroutines (built-in, tied to MonoBehaviour)

```csharp
private IEnumerator FadeOut(CanvasGroup group, float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        group.alpha = 1f - (elapsed / duration);
        yield return null; // Wait one frame
    }
    group.alpha = 0f;
}

// Starting and stopping
private Coroutine _fadeCoroutine;

public void StartFade()
{
    if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
    _fadeCoroutine = StartCoroutine(FadeOut(_canvasGroup, 1f));
}

// Common yield instructions
yield return null;                              // Next frame
yield return new WaitForSeconds(2f);            // 2 seconds (affected by Time.timeScale)
yield return new WaitForSecondsRealtime(2f);    // 2 real seconds (ignores timeScale)
yield return new WaitForFixedUpdate();          // Next physics step
yield return new WaitUntil(() => _isReady);     // Until condition is true
yield return new WaitForEndOfFrame();           // End of frame (after rendering)
```

**Coroutine pitfalls:**
- Dies when the GameObject is disabled or destroyed -- no cleanup callback
- Cannot return values
- Error handling is awkward (no try/catch across yields)
- Allocates garbage with `new WaitForSeconds` (cache them)

### UniTask (async/await, recommended for new projects)

```csharp
using Cysharp.Threading.Tasks;

private async UniTaskVoid FadeOutAsync(CanvasGroup group, float duration,
    CancellationToken ct = default)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        ct.ThrowIfCancellationRequested();
        elapsed += Time.deltaTime;
        group.alpha = 1f - (elapsed / duration);
        await UniTask.Yield(ct);
    }
    group.alpha = 0f;
}

// Cancellation via CancellationTokenSource
private CancellationTokenSource _cts;

public void StartFade()
{
    _cts?.Cancel();
    _cts = new CancellationTokenSource();
    FadeOutAsync(_canvasGroup, 1f, _cts.Token).Forget();
}

private void OnDestroy() => _cts?.Cancel();
```

**When to use which:**
- Simple delays, visual tweens -> coroutine is fine
- Anything needing cancellation, error handling, return values -> UniTask
- Chaining multiple async operations -> UniTask (avoids callback hell)

## LINQ and GC

```csharp
// SAFE: in Awake, Start, event handlers, infrequent calls
var activeEnemies = _enemies.Where(e => e.IsAlive).ToList();
var closestItem = _items.OrderBy(i => Vector3.Distance(transform.position, i.Position)).First();

// DANGEROUS: in Update, FixedUpdate, or any per-frame path
// Each LINQ call allocates an enumerator + closure. GC spike in hot paths.
private void Update()
{
    // BAD -- allocates every frame
    var nearest = _targets.Where(t => t.IsActive).OrderBy(t => DistanceTo(t)).FirstOrDefault();

    // GOOD -- manual loop, zero allocation
    Transform nearest = null;
    float nearestDist = float.MaxValue;
    for (int i = 0; i < _targets.Count; i++)
    {
        if (!_targets[i].IsActive) continue;
        float dist = Vector3.SqrMagnitude(_targets[i].position - transform.position);
        if (dist < nearestDist)
        {
            nearestDist = dist;
            nearest = _targets[i];
        }
    }
}
```

**GC-safe hot path rules:**
- No LINQ in Update/FixedUpdate/LateUpdate
- No `foreach` on non-List collections (some allocate enumerators -- use `for` loops)
- No string concatenation (`+`) in hot paths -- use `StringBuilder` or `string.Create`
- Cache `WaitForSeconds` instances: `private static readonly WaitForSeconds Wait1s = new(1f);`
- Use `SqrMagnitude` instead of `Distance` (avoids sqrt)

## Serialization Attributes

```csharp
public class WeaponConfig : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Base damage per hit before modifiers")]
    [SerializeField] private int _baseDamage = 10;

    [Range(0.1f, 5f)]
    [SerializeField] private float _fireRate = 1f;

    [Space(10)]
    [Header("Audio")]
    [SerializeField] private AudioClip _fireSound;

    [HideInInspector]
    public int RuntimeComboMultiplier; // Serialized but hidden from Inspector

    [System.NonSerialized]
    public float TemporaryBoost; // Not serialized at all, not shown in Inspector

    [TextArea(3, 10)]
    [SerializeField] private string _description;

    [Min(0)]
    [SerializeField] private float _cooldown = 0.5f;
}
```

**Key attributes:**
- `[SerializeField]` -- expose private fields to Inspector (prefer over `public`)
- `[HideInInspector]` -- hide a public/serialized field from Inspector
- `[Header("X")]` -- section label in Inspector
- `[Range(min, max)]` -- slider in Inspector
- `[Tooltip("X")]` -- hover text in Inspector
- `[Min(0)]` -- clamp minimum value
- `[TextArea]` -- multiline text box
- `[Space(px)]` -- visual spacing in Inspector
- `[FormerlySerializedAs("oldName")]` -- rename fields without losing data

