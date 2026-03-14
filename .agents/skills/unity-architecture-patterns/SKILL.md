# Unity Architecture Patterns

> Comprehensive guide to C# best practices, design patterns, ScriptableObject architecture, YAML serialization, AI Navigation, and workflow optimization for Unity game development. Compiled from 22 official Unity sources.

---

## Section 1: C# Style & Formatting

### Naming Conventions

| Style | Usage | Examples |
|-------|-------|---------|
| `camelCase` | Local variables, parameters, private fields | `playerSpeed`, `maxHealth`, `_rigidbody` |
| `PascalCase` | Classes, public fields, methods, properties, enums, interfaces | `PlayerController`, `MoveSpeed`, `GameState` |
| `kebab-case` | UI Toolkit USS files only | `player-health-bar.uss` |
| `I` prefix | Interfaces | `IPlayerController`, `IDamageable` |
| `On` prefix | Event handlers | `OnPlayerDeath`, `OnCollisionEnter` |

**Rules:**
- Hungarian notation (`strName`, `iCount`) is discouraged
- Event handler naming: `Subject_EventName` pattern for UI callbacks (e.g., `Button_OnClicked()`)
- Prefer `System.Action` and `System.Action<T>` over custom delegate declarations
- Constants use `PascalCase`, not `SCREAMING_SNAKE_CASE`

```csharp
// Good: Action-based events
public event Action<int> OnHealthChanged;
public event Action OnPlayerDeath;

// Good: Subject_EventName for UI callbacks
private void StartButton_OnClicked() { }
private void VolumeSlider_OnValueChanged(float value) { }

// Avoid: custom delegate when Action works
// BAD: public delegate void HealthChangedDelegate(int newHealth);
```

### Formatting Rules

- **Brace style:** Allman/BSD -- opening brace on its own line
- **Indentation:** 4 spaces per level, spaces over tabs
- **Braces:** Never omit, even for single-line `if`/`for`/`while` blocks
- **Line length:** Aim for 120 characters max

```csharp
// Allman brace style
public class VehicleController : MonoBehaviour
{
    [SerializeField] private float _topSpeed = 25f;
    [Range(0f, 1f)]
    [SerializeField] private float _brakePower = 0.8f;

    [Serializable]
    public struct WheelSettings
    {
        public float Radius;
        public float SpringRate;
        public float DamperRate;
    }

    // Never omit braces
    public void ApplyBrake(float input)
    {
        if (input > 0f)
        {
            _currentBrakeForce = input * _brakePower;
        }
    }
}
```

### Inspector Attributes

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[SerializeField]` | Expose private field in Inspector | `[SerializeField] private float _speed;` |
| `[Range(min, max)]` | Numeric slider in Inspector | `[Range(0f, 100f)] private float _health;` |
| `[Serializable]` | Nested struct/class grouping in Inspector | `[Serializable] public struct Config { }` |
| `[Header("Section")]` | Visual grouping label | `[Header("Movement")]` |
| `[Tooltip("text")]` | Hover description in Inspector | `[Tooltip("Units per second")]` |
| `[Space(pixels)]` | Visual spacing | `[Space(10)]` |
| `[HideInInspector]` | Hide public field from Inspector | `[HideInInspector] public int internalId;` |

---

## Section 2: Workflow Optimization

### Enter Play Mode Settings

Fast iteration without full domain/scene reload:

1. **Edit > Project Settings > Editor**
2. Disable **Domain Reload** and/or **Scene Reload**
3. Play mode entry drops from seconds to near-instant

**Caveats:**
- Static variables retain values between plays -- reset in `[RuntimeInitializeOnLoadMethod]`
- Re-enable before making script structural changes
- Test with full reload periodically to catch stale-state bugs

```csharp
// Reset statics when Domain Reload is disabled
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }
}
```

### Assembly Definitions (.asmdef)

By default, all scripts compile into `Assembly-CSharp.dll` -- any change recompiles everything.

**Benefits of .asmdef files:**
- Faster compilation (only changed assemblies recompile)
- Enforced architectural boundaries (explicit dependencies)
- Platform-specific code isolation
- Reusable library packaging

**Recommended assembly structure:**

| Assembly | Contents |
|----------|----------|
| `Game.Core` | Interfaces, data types, constants |
| `Game.Runtime` | MonoBehaviours, game systems (references Core) |
| `Game.Editor` | Custom inspectors, editor tools (Editor-only platform) |
| `Game.Tests.EditMode` | Edit-mode tests (references Core + Runtime) |
| `Game.Tests.PlayMode` | Play-mode tests (references Core + Runtime) |

### Script Templates

Override Unity defaults per-project:

- **Default location:** `C:\Program Files\Unity\Editor\Data\Resources\ScriptTemplates`
- **Project override:** `Assets/ScriptTemplates/`
- **Variables:** `#SCRIPTNAME#`, `#NOTRIM#` (preserves whitespace)

```csharp
// Example: Assets/ScriptTemplates/81-C# Script-NewBehaviourScript.cs.txt
using UnityEngine;

namespace MyGame
{
    /// <summary>
    /// TODO: Describe #SCRIPTNAME#
    /// </summary>
    public class #SCRIPTNAME# : MonoBehaviour
    {
        #NOTRIM#
    }
}
```

### IDE Tips (Rider / Visual Studio)

| Feature | How | Benefit |
|---------|-----|---------|
| Conditional breakpoints | Right-click breakpoint > set expression | Debug specific object instances |
| Roslyn analyzers | Install `Microsoft.Unity.Analyzers` NuGet | Catch `CompareTag`, `GetComponent` in Update, etc. |
| TODO/HACK tokens | View > Tool Windows > TODO (Rider) | Track technical debt |
| Bulk rename | Ctrl+R, Ctrl+R (VS) / Shift+F6 (Rider) | Rename with preview across solution |
| Attach to Unity | Run > Attach to Unity Process | Debug with breakpoints in play mode |

---

## Section 3: YAML Serialization

Unity uses a custom high-performance YAML subset for scene, prefab, and asset files. This is **not** full YAML spec -- comments are not supported.

### File Structure

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &6543210
GameObject:
  m_ObjectHideFlags: 0
  m_Name: PlayerCar
  m_Component:
  - component: {fileID: 6543211}
  - component: {fileID: 6543212}
--- !u!4 &6543211
Transform:
  m_Father: {fileID: 0}
  m_LocalPosition: {x: 0, y: 0.5, z: 0}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
```

### Key Concepts

| Element | Meaning | Example |
|---------|---------|---------|
| `!u!{CLASS_ID}` | Numeric object type | `!u!1` = GameObject, `!u!4` = Transform |
| `&{FILE_ID}` | Unique identifier within file | `&6543210` |
| `{fileID: N}` | Cross-reference to another object | `{fileID: 6543211}` |
| `{fileID: 0}` | Null reference | No parent, no target |
| `m_` prefix | Serialized property convention | `m_Name`, `m_LocalPosition` |

### Common Class IDs

| ID | Type | ID | Type |
|----|------|----|------|
| 1 | GameObject | 114 | MonoBehaviour |
| 4 | Transform | 120 | LineRenderer |
| 20 | Camera | 135 | SphereCollider |
| 23 | MeshRenderer | 136 | CapsuleCollider |
| 33 | MeshFilter | 143 | CharacterController |
| 54 | Rigidbody | 212 | SpriteRenderer |
| 65 | BoxCollider | 1001 | Prefab |

### Practical Uses

- **Search-and-replace** for animation track retactoring (rename animated property paths)
- **Merge conflict resolution** -- understanding fileID references helps resolve scene merge conflicts
- **Scripted asset manipulation** -- batch update serialized fields across many prefabs
- **Debugging** -- find broken references by searching for `{fileID: 0}` where values are expected

**WARNING:** Unity YAML is not designed for manual editing. Always:
1. Back up files before editing
2. Use version control
3. Validate by opening in Unity Editor after changes
4. Prefer Editor scripting over raw YAML manipulation when possible

---

## Section 4: Design Patterns

### Observer Pattern

Subjects broadcast state changes to subscribers via C# events. Decouples sender from receivers.

**When to use:** UI updates, objectives, death events, item collection, achievements, analytics.

```csharp
// Subject: broadcasts events
public class Health : MonoBehaviour
{
    public event Action<int> OnHealthChanged;
    public event Action OnDied;

    private int _currentHealth;
    private int _maxHealth = 100;

    public void TakeDamage(int amount)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth);

        if (_currentHealth <= 0)
        {
            OnDied?.Invoke();
        }
    }
}

// Observer: subscribes to events
public class HealthUI : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private Slider _healthBar;

    private void Awake()
    {
        _health.OnHealthChanged += HandleHealthChanged;
        _health.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        _health.OnHealthChanged -= HandleHealthChanged;
        _health.OnDied -= HandleDied;
    }

    private void HandleHealthChanged(int newHealth)
    {
        _healthBar.value = newHealth;
    }

    private void HandleDied()
    {
        gameObject.SetActive(false);
    }
}
```

**Critical rules:**
- Subscribe in `Awake()`, unsubscribe in `OnDestroy()` -- prevents memory leaks and null reference errors
- Use `?.Invoke()` for null safety (no subscribers = no crash)
- `UnityEvent` is Inspector-configurable but slower; `Action` is faster for code-only wiring

| Approach | Inspector | Performance | Use Case |
|----------|-----------|-------------|----------|
| `System.Action` | No | Fast | Code-wired events |
| `UnityEvent` | Yes | Slower (reflection) | Designer-configurable events |
| `UnityAction` | No | Fast | Callbacks for UnityEvent |

### State Pattern

Encapsulates each state as a separate class. Eliminates complex switch/if chains.

**When to use:** Player states, AI behaviors, game phases, menu navigation.

```csharp
// State interface
public interface IState
{
    void Enter();
    void Update();
    void Exit();
}

// Concrete state
public class IdleState : IState
{
    private readonly PlayerController _player;

    public IdleState(PlayerController player)
    {
        _player = player;
    }

    public void Enter()
    {
        _player.Animator.Play("Idle");
    }

    public void Update()
    {
        if (_player.MoveInput.magnitude > 0.1f)
        {
            _player.StateMachine.TransitionTo(_player.StateMachine.MovingState);
        }
    }

    public void Exit() { }
}

// State machine
public class StateMachine
{
    public event Action<IState> OnStateChanged;

    private IState _currentState;

    public IState IdleState { get; set; }
    public IState MovingState { get; set; }
    public IState JumpingState { get; set; }

    public void Initialize(IState startingState)
    {
        _currentState = startingState;
        _currentState.Enter();
    }

    public void TransitionTo(IState nextState)
    {
        _currentState.Exit();
        _currentState = nextState;
        _currentState.Enter();
        OnStateChanged?.Invoke(nextState);
    }

    public void Update()
    {
        _currentState?.Update();
    }
}
```

**Advantages over switch statements:**
- Each state is independent -- adding new states does not affect existing ones
- States manage their own transition conditions
- `OnStateChanged` event notifies external objects (UI, analytics) without coupling
- Easy to unit test individual states in isolation

### Factory Pattern

Encapsulates object creation behind an interface. Callers request products without knowing concrete types.

**When to use:** Spawning enemies, projectiles, power-ups, UI elements, level chunks.

```csharp
// Product interface
public interface IProjectile
{
    void Initialize(Vector3 position, Vector3 direction, float speed);
    void Launch();
}

// Concrete products
public class Bullet : MonoBehaviour, IProjectile
{
    public void Initialize(Vector3 position, Vector3 direction, float speed) { /* ... */ }
    public void Launch() { /* ... */ }
}

public class Missile : MonoBehaviour, IProjectile
{
    public void Initialize(Vector3 position, Vector3 direction, float speed) { /* ... */ }
    public void Launch() { /* ... */ }
}

// Factory
public abstract class ProjectileFactory : MonoBehaviour
{
    public abstract IProjectile Create(Vector3 position, Vector3 direction);
}

public class BulletFactory : ProjectileFactory
{
    [SerializeField] private Bullet _prefab;

    public override IProjectile Create(Vector3 position, Vector3 direction)
    {
        Bullet bullet = Instantiate(_prefab, position, Quaternion.LookRotation(direction));
        bullet.Initialize(position, direction, 50f);
        return bullet;
    }
}
```

**Adaptations:**
- **Dictionary lookup:** Map enum/string keys to prefabs for data-driven spawning
- **Static factory:** Single class with static methods for simple cases
- **Pool integration:** Factory pulls from ObjectPool instead of calling Instantiate

### Command Pattern

Encapsulates actions as objects. Enables undo/redo, replay, queuing, and macro recording.

**When to use:** Turn-based games, strategy games, puzzle undo/redo, editor tools, input replay.

```csharp
// Command interface
public interface ICommand
{
    void Execute();
    void Undo();
}

// Concrete command
public class MoveCommand : ICommand
{
    private readonly Transform _transform;
    private readonly Vector3 _direction;

    public MoveCommand(Transform transform, Vector3 direction)
    {
        _transform = transform;
        _direction = direction;
    }

    public void Execute()
    {
        _transform.position += _direction;
    }

    public void Undo()
    {
        _transform.position -= _direction; // Inverse operation
    }
}

// Invoker with undo/redo stacks
public class CommandInvoker
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // New action invalidates redo history
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        ICommand command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        ICommand command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
    }
}
```

**Key insight:** Undo is the inverse operation. For movement, negate the vector. For property changes, store the previous value. For creation, destroy on undo.

### MVC / MVP Pattern

Separates data (Model), presentation (View), and logic (Controller/Presenter).

**MVP is preferred in Unity** because the Presenter acts as a testable intermediary between Model and View, avoiding direct coupling.

```csharp
// Model: pure data, no Unity dependencies
public class HealthModel
{
    public event Action<int> OnHealthChanged;

    private int _currentHealth;
    public int MaxHealth { get; }

    public HealthModel(int maxHealth)
    {
        MaxHealth = maxHealth;
        _currentHealth = maxHealth;
    }

    public int CurrentHealth
    {
        get => _currentHealth;
        set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            OnHealthChanged?.Invoke(_currentHealth);
        }
    }
}

// View: handles UI only, no game logic
public class HealthView : MonoBehaviour
{
    [SerializeField] private Slider _healthBar;
    [SerializeField] private TextMeshProUGUI _healthText;

    public void UpdateHealth(int current, int max)
    {
        _healthBar.value = (float)current / max;
        _healthText.text = $"{current}/{max}";
    }
}

// Presenter: wires Model and View together
public class HealthPresenter : MonoBehaviour
{
    [SerializeField] private HealthView _view;

    private HealthModel _model;

    private void Awake()
    {
        _model = new HealthModel(100);
        _model.OnHealthChanged += HandleHealthChanged;
        _view.UpdateHealth(_model.CurrentHealth, _model.MaxHealth);
    }

    private void OnDestroy()
    {
        _model.OnHealthChanged -= HandleHealthChanged;
    }

    public void ApplyDamage(int amount)
    {
        _model.CurrentHealth -= amount;
    }

    private void HandleHealthChanged(int newHealth)
    {
        _view.UpdateHealth(newHealth, _model.MaxHealth);
    }
}
```

**When to use:** Large teams, long-maintained projects, UI-heavy applications.
**Caution:** Overkill for simple scripts. Not everything needs MVC.

### MVVM Pattern

Model-View-ViewModel with automatic data binding. Eliminates manual UI update code.

| Component | Responsibility |
|-----------|---------------|
| Model | Game data and business logic |
| View | Visual presentation (UI Builder or Canvas) |
| ViewModel | Exposes bindable properties, transforms data for UI |

**Data binding approaches:**
- **UI Builder (visual):** Bind in UXML inspector directly to ViewModel properties
- **C# scripting:** Register property change callbacks manually
- **Data converters:** Transform model data to UI-compatible formats (e.g., `float` to formatted `string`)

**Best for:** Complex UIs with many elements dependent on shared state (inventory, settings, HUD).

### Strategy Pattern

Swap algorithms at runtime through a shared interface. ScriptableObject-based strategies are Inspector-assignable.

```csharp
// Strategy base
public abstract class AttackStrategySO : ScriptableObject
{
    public abstract void Execute(GameObject attacker, GameObject target);
}

// Concrete strategies as ScriptableObject assets
[CreateAssetMenu(menuName = "Strategies/Melee Attack")]
public class MeleeAttackSO : AttackStrategySO
{
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _range = 2f;

    public override void Execute(GameObject attacker, GameObject target)
    {
        float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
        if (distance <= _range)
        {
            target.GetComponent<Health>()?.TakeDamage((int)_damage);
        }
    }
}

// Client swaps strategies at runtime
public class Fighter : MonoBehaviour
{
    [SerializeField] private AttackStrategySO _currentAttack;

    public void SetStrategy(AttackStrategySO newStrategy)
    {
        _currentAttack = newStrategy;
    }

    public void Attack(GameObject target)
    {
        _currentAttack.Execute(gameObject, target);
    }
}
```

**Use cases:** Ability systems, AI behaviors, combat modes, navigation algorithms, difficulty scaling.

### Flyweight Pattern

Separate shared (intrinsic) data from unique (extrinsic) instance data to minimize memory.

```csharp
// Shared data (flyweight) -- one asset, many references
[CreateAssetMenu(menuName = "Data/Unit Stats")]
public class UnitStatsSO : ScriptableObject
{
    public string UnitName;
    public Sprite Icon;
    public int BaseHealth;
    public float MoveSpeed;
    public GameObject Prefab;
}

// Instance data (extrinsic) -- unique per unit
public class UnitInstance : MonoBehaviour
{
    [SerializeField] private UnitStatsSO _stats; // Shared reference

    // Unique per instance
    private int _currentHealth;
    private Vector3 _position;
    private float _statusTimer;

    private void Awake()
    {
        _currentHealth = _stats.BaseHealth;
    }
}
```

**Sweet spot:** Hundreds or more similar objects (crowds, swarms, particle-like entities). Use Memory Profiler to validate the savings justify the pattern's complexity.

### Dirty Flag Pattern

Track whether state has changed since last processing. Skip expensive recalculations when nothing changed.

```csharp
public class TransformHierarchy : MonoBehaviour
{
    private bool _isDirty = true;
    private Matrix4x4 _worldMatrix;

    public Vector3 LocalPosition
    {
        get => transform.localPosition;
        set
        {
            transform.localPosition = value;
            _isDirty = true;
        }
    }

    public Matrix4x4 GetWorldMatrix()
    {
        if (_isDirty)
        {
            _worldMatrix = CalculateWorldMatrix();
            _isDirty = false;
        }
        return _worldMatrix;
    }

    private Matrix4x4 CalculateWorldMatrix() { /* expensive calculation */ return Matrix4x4.identity; }
}
```

**Use cases:** Transform hierarchies, physics state, pathfinding, procedural generation, UI layouts, LOD in open worlds.

### Object Pooling

Pre-instantiate and reuse objects instead of Instantiate/Destroy to avoid GC spikes.

**Built-in API:** `UnityEngine.Pool.ObjectPool<T>` (Unity 2021+)

```csharp
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Projectile _prefab;
    [SerializeField] private int _defaultCapacity = 20;
    [SerializeField] private int _maxSize = 100;

    private IObjectPool<Projectile> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<Projectile>(
            createFunc: () =>
            {
                Projectile obj = Instantiate(_prefab);
                obj.SetPool(_pool);
                return obj;
            },
            actionOnGet: obj =>
            {
                obj.gameObject.SetActive(true);
            },
            actionOnRelease: obj =>
            {
                obj.gameObject.SetActive(false);
                obj.ResetState(); // CRITICAL: always reset
            },
            actionOnDestroy: obj =>
            {
                Destroy(obj.gameObject);
            },
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize
        );
    }

    public Projectile Get(Vector3 position, Quaternion rotation)
    {
        Projectile projectile = _pool.Get();
        projectile.transform.SetPositionAndRotation(position, rotation);
        return projectile;
    }
}

public class Projectile : MonoBehaviour
{
    private IObjectPool<Projectile> _pool;
    private Rigidbody _rb;

    public void SetPool(IObjectPool<Projectile> pool) => _pool = pool;

    public void ResetState()
    {
        // CRITICAL: Reset ALL state
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    public void ReturnToPool()
    {
        _pool.Release(this);
    }
}
```

**Critical rules:**
- Always reset object state on release (velocities, animations, transforms, timers)
- Profile before implementing -- do not pool without evidence of GC pressure
- `defaultCapacity` is the initial collection size, `maxSize` caps total instances

---

## Section 5: ScriptableObject Architecture Patterns

### Data/Logic Separation

ScriptableObjects store configuration data separate from runtime behavior. They persist on disk, are referenced across scenes, and provide designer-friendly Inspector editing.

```csharp
[CreateAssetMenu(fileName = "New Vehicle", menuName = "Config/Vehicle Data")]
public class VehicleDataSO : ScriptableObject
{
    [Header("Performance")]
    public float TopSpeed = 25f;
    public float Acceleration = 12f;
    public AnimationCurve TorqueCurve;

    [Header("Handling")]
    public float SteeringAngle = 30f;
    public float GripFactor = 1.0f;

    [Header("Visual")]
    public GameObject BodyPrefab;
    public Material PaintMaterial;
}

// Runtime behavior references the data asset
public class VehicleController : MonoBehaviour
{
    [SerializeField] private VehicleDataSO _data;

    private void FixedUpdate()
    {
        float torque = _data.TorqueCurve.Evaluate(_currentRpm);
        // Use _data.TopSpeed, _data.SteeringAngle, etc.
    }
}
```

### Event Channels

ScriptableObject-based pub/sub messaging. Decouples systems without singletons or direct references.

```csharp
// Base event channel
[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannelSO : ScriptableObject
{
    private Action _onEventRaised;

    public void RaiseEvent()
    {
        _onEventRaised?.Invoke();
    }

    public void Subscribe(Action listener)
    {
        _onEventRaised += listener;
    }

    public void Unsubscribe(Action listener)
    {
        _onEventRaised -= listener;
    }
}

// Typed event channel
[CreateAssetMenu(menuName = "Events/Int Event Channel")]
public class IntEventChannelSO : ScriptableObject
{
    private Action<int> _onEventRaised;

    public void RaiseEvent(int value)
    {
        _onEventRaised?.Invoke(value);
    }

    public void Subscribe(Action<int> listener)
    {
        _onEventRaised += listener;
    }

    public void Unsubscribe(Action<int> listener)
    {
        _onEventRaised -= listener;
    }
}

// Publisher
public class ScoreManager : MonoBehaviour
{
    [SerializeField] private IntEventChannelSO _scoreChangedChannel;

    public void AddScore(int points)
    {
        _totalScore += points;
        _scoreChangedChannel.RaiseEvent(_totalScore);
    }
}

// Subscriber
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private IntEventChannelSO _scoreChangedChannel;

    private void OnEnable() => _scoreChangedChannel.Subscribe(UpdateDisplay);
    private void OnDisable() => _scoreChangedChannel.Unsubscribe(UpdateDisplay);

    private void UpdateDisplay(int score)
    {
        _scoreText.text = score.ToString();
    }
}
```

**Key rules:**
- Subscribe in `OnEnable()`, unsubscribe in `OnDisable()` for SO event channels
- Both publisher and subscriber reference the same SO asset via Inspector
- No direct coupling -- either side can be swapped without touching the other

### SO-Based Enums

Replace C# enums with empty ScriptableObject subclasses for safe refactoring and Inspector-friendly usage.

```csharp
// Base type
[CreateAssetMenu(menuName = "Enums/Surface Type")]
public class SurfaceTypeSO : ScriptableObject { }

// Create assets: Dirt.asset, Gravel.asset, Asphalt.asset, Grass.asset
// Compare by reference (== operator), not by integer value
// Renaming assets does NOT break serialized references (unlike C# enum reordering)

public class Wheel : MonoBehaviour
{
    [SerializeField] private SurfaceTypeSO _currentSurface;
    [SerializeField] private SurfaceTypeSO _dirtSurface; // Drag asset reference

    private void CheckSurface()
    {
        if (_currentSurface == _dirtSurface)
        {
            ApplyDirtPhysics();
        }
    }
}
```

**Advantages over C# enums:**
- Renaming an asset does not break serialized references
- Adding/removing values does not shift integer indices
- Inspector drag-and-drop assignment
- Can add data fields to the base class later without refactoring consumers

### Delegate Objects

ScriptableObjects as pluggable behavior containers. The Strategy pattern applied to Unity assets.

```csharp
// Delegate base
public abstract class DamageProcessorSO : ScriptableObject
{
    public abstract int Process(int baseDamage, GameObject target);
}

// Concrete delegates
[CreateAssetMenu(menuName = "Damage/Armor Reduction")]
public class ArmorReductionSO : DamageProcessorSO
{
    [SerializeField] private float _reductionPercent = 0.2f;

    public override int Process(int baseDamage, GameObject target)
    {
        return Mathf.RoundToInt(baseDamage * (1f - _reductionPercent));
    }
}

// Client uses delegate chain
public class DamageSystem : MonoBehaviour
{
    [SerializeField] private DamageProcessorSO[] _processors;

    public int CalculateFinalDamage(int baseDamage, GameObject target)
    {
        int damage = baseDamage;
        foreach (var processor in _processors)
        {
            damage = processor.Process(damage, target);
        }
        return damage;
    }
}
```

Designers create new processor assets and add them to the chain without touching code.

### Runtime Sets

ScriptableObject-based collections that track GameObjects at runtime. Self-registering.

```csharp
// Generic runtime set
public abstract class RuntimeSetSO<T> : ScriptableObject
{
    [HideInInspector] // Inspector cannot serialize scene objects
    public List<T> Items = new();

    public void Add(T item)
    {
        if (!Items.Contains(item))
        {
            Items.Add(item);
        }
    }

    public void Remove(T item)
    {
        Items.Remove(item);
    }
}

// Concrete set
[CreateAssetMenu(menuName = "Runtime Sets/Enemy Set")]
public class EnemyRuntimeSetSO : RuntimeSetSO<EnemyController> { }

// Self-registration
public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyRuntimeSetSO _enemySet;

    private void OnEnable() => _enemySet.Add(this);
    private void OnDisable() => _enemySet.Remove(this);
}

// Consumer queries the set without knowing about specific enemies
public class EnemyRadar : MonoBehaviour
{
    [SerializeField] private EnemyRuntimeSetSO _enemySet;

    public EnemyController FindClosest(Vector3 position)
    {
        return _enemySet.Items
            .OrderBy(e => Vector3.Distance(e.transform.position, position))
            .FirstOrDefault();
    }
}
```

**Limitation:** Inspector cannot serialize references to scene objects. Use `[HideInInspector]` on the Items list. The set is populated at runtime via self-registration.

---

## Section 6: AI Navigation

### Setup

AI Navigation is a **separate package** (not included by default since Unity 2022+).

1. **Window > Package Manager** > search "AI Navigation" > Install
2. **Create NavMesh:** Select a ground object > Add Component > NavMesh Surface
3. **Bake:** Click "Bake" on the NavMesh Surface component
4. **Add Agent:** Add Component > NavMesh Agent to moving characters

**Breaking change (2022+):** "Navigation Static" checkbox is removed. Use NavMesh Modifier components instead.

### Key Components

| Component | Purpose | Key Setting |
|-----------|---------|-------------|
| NavMesh Surface | Bakes NavMesh for an area | Agent Type (determines walkable dimensions) |
| NavMesh Modifier | Include/exclude objects from baking | Override Area checkbox |
| NavMesh Obstacle | Dynamic holes in NavMesh | Carve checkbox (cuts real holes vs. avoidance) |
| NavMesh Link | Jump/drop points between surfaces | Start/End points, width, bidirectional toggle |

### Object Collection Options

NavMesh Surface > Object Collection:

| Option | Behavior |
|--------|----------|
| All GameObjects | Bakes everything in scene (default) |
| Volume | Constrains baking to a box area |
| Current Object Hierarchy | Only the surface's own children |
| NavMeshModifier Component Only | Only objects with NavMesh Modifier |

### Multiple Agent Types

Different-sized agents need separate NavMeshes:

1. **Window > AI > Navigation** > Agents tab > create agent types (e.g., Small, Medium, Large)
2. **Create one NavMesh Surface per agent type**
3. Configure per-agent: Step Height, Max Slope, Drop Height, Radius, Height
4. Enable "Generate Links" for auto-detected jump/drop points

```
Example:
- Small Agent: Radius 0.3, Height 0.8, Step Height 0.2, Max Slope 45
- Large Agent: Radius 1.0, Height 2.0, Step Height 0.4, Max Slope 30
```

### Clean NavMesh Tips

| Goal | Setting |
|------|---------|
| Ground-only (no stairs) | Step Height = 0, Max Slope = 0 |
| Separate stair mesh | Create second surface with Step Height > 0 |
| Constrain area | Use Volume collection, adjust box size |
| Exclude decorations | Add NavMesh Modifier > Not Walkable |

### Upgrading from 2021 LTS

If migrating a project that used the legacy baked NavMesh:

1. **Window > AI > NavMesh Updater**
2. Select objects to convert
3. Click "Convert" -- this:
   - Creates NavMesh Surface components on objects that had baked data
   - Replaces "Navigation Static" flags with NavMesh Modifier components
   - Preserves NavMesh Agent and Obstacle settings

---

## Section 7: Related Skills

| Skill | Relationship |
|-------|-------------|
| `unity-csharp-mastery` | Deeper C# lifecycle, attributes, anti-patterns |
| `unity-scriptable-objects` | Extended SO patterns, runtime sets, event systems |
| `unity-state-machines` | Advanced FSM: hierarchical states, Animator integration |
| `unity-composition` | Component architecture, dependency injection, interfaces |
| `unity-performance-optimization` | Profiling, batching, GC reduction, LOD |
| `unity-testing-patterns` | Unit testing patterns for these architectures |
| `unity-project-foundations` | .asmdef setup, folder structure, .gitignore |
| `unity-3d-world-building` | NavMesh in context of terrain and level design |
| `unity-editor-scripting` | Custom inspectors for SO-heavy architectures |
| `unity-ui-toolkit` | UXML/USS for MVC/MVVM data binding |

---

## Section 8: Quick Reference -- Pattern Selection

Use this decision tree when choosing a pattern:

```
Need to decouple event sender from receivers?
  --> Observer Pattern (Action events or SO Event Channels)

Need to manage complex object states with transitions?
  --> State Pattern (IState + StateMachine)

Need to create objects without exposing concrete types?
  --> Factory Pattern (abstract factory or dictionary lookup)

Need undo/redo, replay, or queued actions?
  --> Command Pattern (ICommand + invoker stacks)

Need to separate UI from game logic cleanly?
  --> MVP Pattern (Model + View + Presenter)
  --> MVVM Pattern (if using UI Toolkit data binding)

Need to swap algorithms/behaviors at runtime?
  --> Strategy Pattern (SO-based for Inspector assignment)

Need hundreds of similar objects with shared data?
  --> Flyweight Pattern (SO holds shared, instances hold unique)

Need to skip expensive recalculations?
  --> Dirty Flag Pattern (bool + lazy recompute)

Need to avoid GC spikes from frequent spawn/destroy?
  --> Object Pooling (ObjectPool<T> built-in API)
```

### Pattern Compatibility Matrix

Patterns frequently combine:

| Combination | Example |
|-------------|---------|
| Factory + Pool | Factory pulls from pool instead of Instantiate |
| Observer + MVP | Model emits events, Presenter updates View |
| State + Command | Each state generates commands for undo history |
| Strategy + SO | Strategies as ScriptableObject assets |
| Flyweight + SO | Shared data in SO, unique data in MonoBehaviour |
| Dirty Flag + Observer | Flag set by event, checked on demand |
| Command + Pool | Reuse command objects to avoid allocation |
