# Unity Design Patterns

Use this skill when choosing, implementing, or combining design patterns in Unity. Covers 11 patterns with RC racing examples, Unity-specific implementation details, and guidance on when each pattern helps vs. when it is overkill.

---

## SOLID Principles

Design patterns are most effective when grounded in SOLID. These five principles guide when and how to apply patterns in Unity.

> "Don't force principles into your scripts for the sake of it. Let them organically work into place through necessity." Also keep KISS and DRY in mind -- apply SOLID where it reduces complexity, not where it adds ceremony.

### Single Responsibility

Each class/MonoBehaviour is responsible for one thing. A `RaceManager` manages race state -- it does not also handle UI updates or audio. If a class has multiple reasons to change, split it.

### Open-Closed

Classes open for extension, closed for modification. Use ScriptableObjects and interfaces to add new behavior without modifying existing code. Example: new surface types via `SurfaceData` SO assets, not by adding cases to a switch statement in the physics system.

### Liskov Substitution

Derived classes must be substitutable for their base. If `Vehicle` has a `Drive()` method, every subclass must honor that contract. Do not throw `NotImplementedException` in overrides -- that violates the contract callers depend on.

### Interface Segregation

Do not force classes to implement methods they do not use. Split large interfaces: `IDamageable`, `IRepairable`, `IResettable` -- not one `IVehicle` with 20 methods. Each interface should represent a single capability.

### Dependency Inversion

High-level modules depend on abstractions, not concrete implementations. Inject dependencies via constructor, `[SerializeField]`, or interfaces -- never `FindObjectOfType`. This is why our project forbids singletons and `Find()` calls (see coding standards).

---

## Pattern Catalog

| Pattern | Purpose | RC Racing Example |
|---------|---------|-------------------|
| Observer | Decouple event senders from receivers | `OnLapComplete`, `OnCheckpointReached` |
| State | Manage complex object states with transitions | Vehicle: Idle, Accelerating, Braking, Airborne, Crashed |
| Factory | Create objects without exposing concrete types | Track obstacle spawning, vehicle configuration |
| Command | Enable undo/redo, replay, queued actions | Track editor undo/redo for obstacle placement |
| MVP | Separate UI from game logic | Race HUD presenter wiring model to view |
| MVVM | Automatic data binding for complex UIs | Settings screen, garage/tuning UI |
| Strategy | Swap algorithms at runtime | Surface physics: grip/friction per terrain type |
| Flyweight | Share data across many similar instances | Track segments: same geometry, different positions |
| Dirty Flag | Skip expensive recalculations | Recalculate race standings only when positions change |
| Object Pooling | Reuse objects to avoid GC spikes | Tire smoke particles, dust effects, sound effects |
| Singleton | **ANTI-PATTERN** -- globally accessible single instance | Shown for reference; use alternatives below |

---

## Observer Pattern

### Concept

Subjects broadcast state changes to subscribers via C# events. Decouples sender from receivers so neither side knows about the other.

### RC Racing Example

Race events: `OnLapComplete`, `OnCheckpointReached`, `OnRaceFinished`. The race manager raises events; the HUD, audio system, and telemetry recorder each subscribe independently.

### Code Example

```csharp
// Subject: broadcasts race events
public class RaceManager : MonoBehaviour
{
    public event Action<int> OnLapComplete;        // lap number
    public event Action<int> OnCheckpointReached;  // checkpoint index
    public event Action OnRaceFinished;

    private int _currentLap;
    private int _totalLaps = 5;

    public void ReachCheckpoint(int index)
    {
        OnCheckpointReached?.Invoke(index);
    }

    public void CompleteLap()
    {
        _currentLap++;
        OnLapComplete?.Invoke(_currentLap);

        if (_currentLap >= _totalLaps)
        {
            OnRaceFinished?.Invoke();
        }
    }
}

// Observer: subscribes to events
public class RaceHUD : MonoBehaviour
{
    [SerializeField] private RaceManager _raceManager;
    [SerializeField] private TextMeshProUGUI _lapText;

    // C# events: subscribe in Awake, unsubscribe in OnDestroy
    private void Awake()
    {
        _raceManager.OnLapComplete += HandleLapComplete;
        _raceManager.OnRaceFinished += HandleRaceFinished;
    }

    private void OnDestroy()
    {
        _raceManager.OnLapComplete -= HandleLapComplete;
        _raceManager.OnRaceFinished -= HandleRaceFinished;
    }

    private void HandleLapComplete(int lap) => _lapText.text = $"Lap {lap}";
    private void HandleRaceFinished() => _lapText.text = "FINISHED";
}
```

**Subscription lifetime rules:**

| Event Type | Subscribe In | Unsubscribe In | Why |
|------------|-------------|----------------|-----|
| C# events (`Action`) | `Awake()` | `OnDestroy()` | Lifetime matches the object; survives enable/disable toggles |
| SO Event Channels | `OnEnable()` | `OnDisable()` | SO outlives scenes; must disconnect when object is inactive to prevent stale callbacks |

**Static EventManager pattern:** For truly global events where no SO asset is desired, a static class with `static event Action<T>` fields works. Subscribers must still unsubscribe in `OnDestroy()` to avoid leaks. Prefer SO Event Channels over static events for testability and Inspector visibility.

**`ObservableCollection<T>`:** When observers need to react to list changes (items added/removed), use `System.Collections.ObjectModel.ObservableCollection<T>` instead of plain `List<T>`.

| Approach | Inspector | Performance | Use Case |
|----------|-----------|-------------|----------|
| `System.Action` | No | Fast | Code-wired events |
| `UnityEvent` | Yes | Slower (reflection) | Designer-configurable events |
| SO Event Channel | Yes (asset ref) | Fast | Cross-scene decoupling |

### When to Use

- Multiple systems need to react to the same event (lap complete updates HUD, plays sound, logs telemetry)
- You want to add new reactions without modifying the event source
- Cross-scene communication via SO Event Channels

### When NOT to Use

- Only one receiver exists and will never change -- a direct method call is simpler
- High-frequency per-frame data (every `FixedUpdate` tick) -- polling or direct reference is cheaper than event overhead
- When subscription order matters -- events do not guarantee invocation order

---

## State Pattern

### Concept

Encapsulates each state as a separate class. Eliminates complex switch/if chains. Each state manages its own behavior and transition conditions.

### RC Racing Example

Vehicle states: Idle (parked), Accelerating (throttle applied), Braking (brake applied), Airborne (all wheels off ground), Crashed (collision detected). Each state has different physics behavior, audio, and visual effects.

### Code Example

```csharp
// State interface
public interface IVehicleState
{
    void Enter(VehicleController vehicle);
    void Update(VehicleController vehicle);
    void FixedUpdate(VehicleController vehicle);
    void Exit(VehicleController vehicle);
}

// Concrete state: Accelerating
public class AcceleratingState : IVehicleState
{
    public void Enter(VehicleController vehicle)
    {
        vehicle.EngineAudio.Play();
    }

    public void Update(VehicleController vehicle) { }

    public void FixedUpdate(VehicleController vehicle)
    {
        vehicle.ApplyThrottle();

        if (vehicle.ThrottleInput <= 0f)
        {
            vehicle.StateMachine.TransitionTo(vehicle.StateMachine.IdleState);
        }
        else if (!vehicle.IsGrounded)
        {
            vehicle.StateMachine.TransitionTo(vehicle.StateMachine.AirborneState);
        }
    }

    public void Exit(VehicleController vehicle) { }
}

// Concrete state: Airborne
public class AirborneState : IVehicleState
{
    public void Enter(VehicleController vehicle)
    {
        vehicle.DisableTractionControl();
    }

    public void Update(VehicleController vehicle) { }

    public void FixedUpdate(VehicleController vehicle)
    {
        vehicle.ApplyAirDrag();

        if (vehicle.IsGrounded)
        {
            vehicle.StateMachine.TransitionTo(vehicle.StateMachine.AcceleratingState);
        }
    }

    public void Exit(VehicleController vehicle)
    {
        vehicle.EnableTractionControl();
    }
}

// State machine
public class VehicleStateMachine
{
    public event Action<IVehicleState> OnStateChanged;

    private IVehicleState _currentState;

    public IVehicleState IdleState { get; set; }
    public IVehicleState AcceleratingState { get; set; }
    public IVehicleState BrakingState { get; set; }
    public IVehicleState AirborneState { get; set; }
    public IVehicleState CrashedState { get; set; }

    public void Initialize(IVehicleState startingState)
    {
        _currentState = startingState;
        _currentState.Enter(null); // Pass vehicle ref in real implementation
    }

    public void TransitionTo(IVehicleState nextState)
    {
        _currentState.Exit(null);
        _currentState = nextState;
        _currentState.Enter(null);
        OnStateChanged?.Invoke(nextState);
    }

    public void Update() => _currentState?.Update(null);
    public void FixedUpdate() => _currentState?.FixedUpdate(null);
}
```

### When to Use

- Object has 3+ distinct behavioral modes with different logic per mode
- State transitions have entry/exit side effects (play sound, enable physics, change visuals)
- You want to add new states without modifying existing ones

### When NOT to Use

- Only 2 states (e.g., on/off) -- a simple bool is clearer
- States have no meaningful entry/exit behavior -- a switch statement is fine
- State transitions are trivial and do not need to be tracked or observed

---

## Factory Pattern

### Concept

Encapsulates object creation behind an interface. Callers request products without knowing concrete types or creation details.

### RC Racing Example

Track obstacle spawning: a factory creates different obstacle types (ramps, barriers, cones) based on track configuration data. Vehicle configuration creation: a factory assembles vehicles from chassis + motor + tire combinations.

### Code Example

```csharp
// Product interface
public interface ITrackObstacle
{
    void Place(Vector3 position, Quaternion rotation);
    ObstacleType Type { get; }
}

// Factory with dictionary lookup for data-driven spawning
public class TrackObstacleFactory : MonoBehaviour
{
    [SerializeField] private ObstaclePrefabEntry[] _prefabEntries;

    private Dictionary<ObstacleType, GameObject> _prefabMap;

    private void Awake()
    {
        _prefabMap = new Dictionary<ObstacleType, GameObject>();
        foreach (ObstaclePrefabEntry entry in _prefabEntries)
        {
            _prefabMap[entry.Type] = entry.Prefab;
        }
    }

    public ITrackObstacle Create(ObstacleType type, Vector3 position, Quaternion rotation)
    {
        if (!_prefabMap.TryGetValue(type, out GameObject prefab))
        {
            Debug.LogError($"No prefab registered for obstacle type: {type}");
            return null;
        }

        GameObject instance = Instantiate(prefab, position, rotation);
        return instance.GetComponent<ITrackObstacle>();
    }

    [Serializable]
    public struct ObstaclePrefabEntry
    {
        public ObstacleType Type;
        public GameObject Prefab;
    }
}
```

**Adaptations:**
- **Pool integration:** Factory pulls from ObjectPool instead of calling Instantiate
- **SO-based config:** Factory reads a ScriptableObject to determine what to spawn
- **Abstract factory:** Multiple factory implementations for different track themes

### When to Use

- Creation logic is complex (multi-step assembly, conditional components)
- You need to swap product types without changing callers (different track themes)
- Creation must be testable in isolation

### When NOT to Use

- Only one product type exists and will not change -- `Instantiate()` directly is simpler
- No conditional logic in creation -- adding a factory layer adds indirection for no benefit
- Object creation is a one-liner with no setup steps

---

## Command Pattern

### Concept

Encapsulates actions as objects. Enables undo/redo, replay, queuing, and macro recording.

### RC Racing Example

Track editor undo/redo: each placement action (adding a ramp, barrier, or cone to the track) is stored as a command. Undoing the action removes the placed object; redoing re-places it. The command stack enables full edit history.

> **Note:** Do NOT use Command pattern for ghost car replay in a PhysX-based game. PhysX is non-deterministic -- input replay diverges within seconds. Ghost systems must use state recording (position/rotation snapshots). See `unity-replay-ghost` for the correct approach.

### Code Example

```csharp
// Command interface
public interface IEditorCommand
{
    void Execute();
    void Undo();
}

// Place a track obstacle in the editor
public class PlaceObstacleCommand : IEditorCommand
{
    private readonly TrackEditor _editor;
    private readonly ObstacleType _type;
    private readonly Vector3 _position;
    private readonly Quaternion _rotation;
    private GameObject _placed;

    public PlaceObstacleCommand(TrackEditor editor, ObstacleType type,
        Vector3 position, Quaternion rotation)
    {
        _editor = editor;
        _type = type;
        _position = position;
        _rotation = rotation;
    }

    public void Execute()
    {
        _placed = _editor.SpawnObstacle(_type, _position, _rotation);
    }

    public void Undo()
    {
        if (_placed != null)
        {
            _editor.RemoveObstacle(_placed);
            _placed = null;
        }
    }
}

// Command history with undo/redo stacks
public class CommandHistory
{
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();

    public void Execute(IEditorCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // New action invalidates redo history
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
}
```

### When to Use

- Undo/redo is required (level editor, vehicle setup, track editor)
- Actions need to be serialized and sent over the network
- Macro recording or action batching is needed

### When NOT to Use

- Ghost car replay -- use state recording instead (see `unity-replay-ghost`)
- Actions are fire-and-forget with no need for history or replay
- The overhead of creating command objects per frame is not justified by the feature set
- Simple input handling that maps directly to behavior -- adding a command layer is unnecessary indirection

---

## MVP Pattern (Model-View-Presenter)

### Concept

Separates data (Model), presentation (View), and logic (Presenter). The Presenter acts as a testable intermediary, keeping the View passive and the Model pure.

### RC Racing Example

Race HUD: the Model tracks lap times, positions, and speed. The View is a set of UI elements. The Presenter formats model data for display and handles input events from the View.

### Code Example

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Model: pure data, no Unity dependencies
public class RaceModel
{
    public event Action<float> OnSpeedChanged;
    public event Action<int, int> OnLapChanged; // current, total

    private float _speed;
    private int _currentLap;

    public int TotalLaps { get; }

    public RaceModel(int totalLaps)
    {
        TotalLaps = totalLaps;
        _currentLap = 1;
    }

    public float Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            OnSpeedChanged?.Invoke(_speed);
        }
    }

    public int CurrentLap
    {
        get => _currentLap;
        set
        {
            _currentLap = Mathf.Clamp(value, 1, TotalLaps);
            OnLapChanged?.Invoke(_currentLap, TotalLaps);
        }
    }
}

// View: handles UI only, no game logic
public class RaceHUDView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _speedText;
    [SerializeField] private TextMeshProUGUI _lapText;
    [SerializeField] private Slider _speedBar;

    public void UpdateSpeed(string text, float normalizedValue)
    {
        _speedText.text = text;
        _speedBar.value = normalizedValue;
    }

    public void UpdateLap(string text)
    {
        _lapText.text = text;
    }
}

// Presenter: wires Model and View together
public class RaceHUDPresenter : MonoBehaviour
{
    [SerializeField] private RaceHUDView _view;
    [SerializeField] private float _maxDisplaySpeed = 120f;

    private RaceModel _model;

    private void Awake()
    {
        _model = new RaceModel(totalLaps: 5);
        _model.OnSpeedChanged += HandleSpeedChanged;
        _model.OnLapChanged += HandleLapChanged;
    }

    private void OnDestroy()
    {
        _model.OnSpeedChanged -= HandleSpeedChanged;
        _model.OnLapChanged -= HandleLapChanged;
    }

    private void HandleSpeedChanged(float speed)
    {
        string text = $"{speed:F0} km/h";
        float normalized = Mathf.Clamp01(speed / _maxDisplaySpeed);
        _view.UpdateSpeed(text, normalized);
    }

    private void HandleLapChanged(int current, int total)
    {
        _view.UpdateLap($"Lap {current}/{total}");
    }
}
```

### When to Use

- UI is complex with many elements dependent on shared state
- You want to unit test presentation logic without running Unity
- Multiple views need to display the same model data differently

### When NOT to Use

- Simple UI with 1-2 elements -- a single MonoBehaviour with direct references is clearer
- Prototype or jam code where iteration speed matters more than architecture
- The "View" is not visual (e.g., audio-only feedback) -- Observer alone is sufficient

---

## MVVM Pattern (Model-View-ViewModel)

### Concept

Model-View-ViewModel with automatic data binding. Eliminates manual UI update code by binding View elements directly to ViewModel properties.

### RC Racing Example

Garage/tuning UI: sliders bound to vehicle tuning parameters. Changing a slider automatically updates the ViewModel property, which updates the Model, which persists to the vehicle config. No manual wiring code per slider.

### Code Example

```csharp
// ViewModel exposes bindable properties
public class VehicleTuningViewModel : MonoBehaviour
{
    // UI Toolkit data binding uses these properties directly
    public float SuspensionStiffness { get; set; }
    public float GearRatio { get; set; }
    public float CamberAngle { get; set; }

    // For C# scripting binding (without UI Toolkit):
    public event Action<string> OnPropertyChanged;

    private float _suspensionStiffness;

    public float BoundSuspensionStiffness
    {
        get => _suspensionStiffness;
        set
        {
            _suspensionStiffness = value;
            OnPropertyChanged?.Invoke(nameof(BoundSuspensionStiffness));
        }
    }
}
```

**Data binding approaches:**
- **UI Toolkit (visual):** Bind in UXML inspector directly to ViewModel properties
- **C# scripting:** Register property change callbacks with `INotifyPropertyChanged` or custom events
- **Data converters and ConverterGroups:** Transform model data to UI-compatible formats (e.g., `float` to formatted `string`). `ConverterGroups` chain multiple converters together.
- **BindingMode:** `TwoWay` (default for input controls), `ToTarget` (read-only display), `ToSource` (write-only from UI)

### When to Use

- Complex UIs with many elements bound to shared state (settings screens, inventory, garage tuning)
- Using UI Toolkit, which has native data binding support
- UI updates are frequent and manual wiring would be tedious

### When NOT to Use

- Using legacy Canvas/UGUI -- MVVM binding requires manual implementation that may not justify the effort
- Simple HUD with a few text fields -- MVP or direct Observer is simpler
- Game logic is tightly coupled to display timing (e.g., animation-driven UI) -- data binding adds latency

---

## Strategy Pattern

### Concept

Swap algorithms at runtime through a shared interface. ScriptableObject-based strategies are Inspector-assignable and support hot-swapping.

### RC Racing Example

Surface physics: different grip/friction strategies per terrain type. Asphalt has high grip, dirt has low grip with oversteer, gravel has loose traction. The vehicle swaps its surface strategy based on what the wheels are touching.

### Code Example

```csharp
// Strategy base as ScriptableObject
public abstract class SurfacePhysicsSO : ScriptableObject
{
    public abstract float GetGrip(float speed, float slipAngle);
    public abstract float GetRollingResistance(float speed);
    public abstract bool AllowsDrifting { get; }
}

// Concrete strategies as assets
[CreateAssetMenu(menuName = "RC/Surface Physics/Asphalt")]
public class AsphaltPhysicsSO : SurfacePhysicsSO
{
    [SerializeField] private float _gripMultiplier = 1.0f;
    [SerializeField] private float _rollingResistance = 0.01f;

    public override bool AllowsDrifting => false;

    public override float GetGrip(float speed, float slipAngle)
    {
        return _gripMultiplier * Mathf.Clamp01(1f - slipAngle / 90f);
    }

    public override float GetRollingResistance(float speed)
    {
        return _rollingResistance * speed;
    }
}

[CreateAssetMenu(menuName = "RC/Surface Physics/Dirt")]
public class DirtPhysicsSO : SurfacePhysicsSO
{
    [SerializeField] private float _gripMultiplier = 0.6f;
    [SerializeField] private float _looseThreshold = 15f;

    public override bool AllowsDrifting => true;

    public override float GetGrip(float speed, float slipAngle)
    {
        float baseGrip = _gripMultiplier * Mathf.Clamp01(1f - slipAngle / 45f);
        return slipAngle > _looseThreshold ? baseGrip * 0.5f : baseGrip;
    }

    public override float GetRollingResistance(float speed)
    {
        return 0.03f * speed; // Higher resistance on dirt
    }
}

// Client swaps strategies based on terrain
public class WheelSurfaceHandler : MonoBehaviour
{
    [SerializeField] private SurfacePhysicsSO _currentSurface;

    public void SetSurface(SurfacePhysicsSO surface)
    {
        _currentSurface = surface;
    }

    public float GetCurrentGrip(float speed, float slipAngle)
    {
        return _currentSurface.GetGrip(speed, slipAngle);
    }
}
```

**Runtime SO creation:** For strategies that need unique runtime instances (e.g., modified copies of a base strategy), use `ScriptableObject.CreateInstance<T>()`. Note that runtime-created SOs are not saved to disk and must be managed manually.

### When to Use

- Multiple interchangeable algorithms exist for the same operation
- Algorithms need to be swapped at runtime (surface changes, difficulty scaling)
- Designers need to create new behaviors without touching code (Inspector-assignable SO assets)

### When NOT to Use

- Only one algorithm exists with no foreseeable variants -- a direct method is clearer
- The "strategy" is a single line of code -- the interface overhead is not justified
- Algorithms differ only in a numeric parameter -- use a configurable field instead of separate classes

---

## Flyweight Pattern

### Concept

Separate shared (intrinsic) data from unique (extrinsic) instance data to minimize memory. In Unity, ScriptableObjects naturally serve as flyweights: one SO asset, many MonoBehaviour references.

### RC Racing Example

Track segment shared data: all instances of a "hairpin turn" segment share the same geometry, surface type, and width from a single SO asset. Each placed instance only stores its own position, rotation, and runtime state (e.g., tire marks applied).

### Code Example

```csharp
// Shared data (flyweight) -- one asset per segment type
[CreateAssetMenu(menuName = "RC/Track Segment Data")]
public class TrackSegmentDataSO : ScriptableObject
{
    [Header("Geometry")]
    public Mesh SegmentMesh;
    public Material SurfaceMaterial;

    [Header("Properties")]
    public float TrackWidth = 2.5f;
    public SurfacePhysicsSO SurfacePhysics;

    [Header("Racing Line")]
    public AnimationCurve IdealSpeedCurve;
}

// Instance data (extrinsic) -- unique per placed segment
public class TrackSegmentInstance : MonoBehaviour
{
    [SerializeField] private TrackSegmentDataSO _segmentData; // Shared reference

    // Unique per instance
    private int _tireMarkCount;
    private float _surfaceDegradation;

    public TrackSegmentDataSO Data => _segmentData;

    public void ApplyTireMark()
    {
        _tireMarkCount++;
        _surfaceDegradation = Mathf.Min(1f, _tireMarkCount * 0.01f);
    }
}
```

### When to Use

- Hundreds or more similar objects that share common read-only data (crowds, track segments, tree instances)
- Memory profiling confirms significant duplication of identical data
- Shared data is truly read-only at runtime

### When NOT to Use

- Fewer than ~50 instances -- memory savings do not justify the pattern
- Each instance needs unique copies of the "shared" data (they modify it at runtime)
- Data is already small (a few floats) -- the SO reference overhead may exceed the savings

---

## Dirty Flag Pattern

### Concept

Track whether state has changed since last processing. Skip expensive recalculations when nothing changed. Set the flag when state mutates; check and clear it when the result is needed.

### RC Racing Example

Race standings: recalculate positions only when a vehicle passes a checkpoint or changes track segment, not every frame. The flag is set by checkpoint events and cleared when the leaderboard UI reads the standings.

### Code Example

```csharp
public class RaceStandings : MonoBehaviour
{
    private bool _isDirty = true;
    private List<RacerPosition> _cachedStandings;
    private List<RacerData> _racers;

    // Called by checkpoint trigger
    public void MarkPositionChanged(int racerId)
    {
        _isDirty = true;
    }

    // Called by UI when it needs to display standings
    public IReadOnlyList<RacerPosition> GetStandings()
    {
        if (_isDirty)
        {
            RecalculateStandings();
            _isDirty = false;
        }
        return _cachedStandings;
    }

    private void RecalculateStandings()
    {
        // Expensive: sort all racers by lap, checkpoint, distance to next checkpoint
        _cachedStandings = _racers
            .OrderByDescending(r => r.CurrentLap)
            .ThenByDescending(r => r.LastCheckpoint)
            .ThenBy(r => r.DistanceToNextCheckpoint)
            .Select((r, i) => new RacerPosition(r.Id, i + 1))
            .ToList();
    }
}
```

### When to Use

- An expensive computation depends on state that changes infrequently relative to how often the result is read
- Multiple sources can trigger recalculation but you only want to compute once
- The result is read many times between changes

### When NOT to Use

- State changes every frame anyway -- the flag check adds overhead without skipping any work
- The computation is cheap (a few arithmetic operations) -- caching is unnecessary
- Multiple independent dirty flags are needed for the same data -- complexity may outweigh benefit; consider Observer instead

---

## Object Pooling

### Concept

Pre-instantiate and reuse objects instead of Instantiate/Destroy to avoid GC spikes. Unity provides `UnityEngine.Pool.ObjectPool<T>` (Unity 2021+).

### RC Racing Example

Tire smoke particles, dust effects, and sound effects: these spawn and despawn frequently during races. Pooling eliminates frame hitches from GC collection during intense racing moments.

### Code Example

```csharp
using UnityEngine;
using UnityEngine.Pool;

public class DustEffectPool : MonoBehaviour
{
    [SerializeField] private ParticleSystem _dustPrefab;
    [SerializeField] private int _defaultCapacity = 20;
    [SerializeField] private int _maxSize = 50;

    private IObjectPool<ParticleSystem> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<ParticleSystem>(
            createFunc: () =>
            {
                ParticleSystem ps = Instantiate(_dustPrefab, transform);
                return ps;
            },
            actionOnGet: ps =>
            {
                ps.gameObject.SetActive(true);
            },
            actionOnRelease: ps =>
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            },
            actionOnDestroy: ps =>
            {
                Destroy(ps.gameObject);
            },
            collectionCheck: true,  // Debug: warns if releasing an already-pooled object
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize
        );
    }

    public ParticleSystem SpawnDust(Vector3 position, Quaternion rotation)
    {
        ParticleSystem ps = _pool.Get();
        ps.transform.SetPositionAndRotation(position, rotation);
        ps.Play();
        return ps;
    }

    public void ReturnDust(ParticleSystem ps)
    {
        _pool.Release(ps);
    }
}
```

**Constructor parameter: `collectionCheck`** (default: `false`). When `true`, the pool throws if you release an object that is already in the pool. Enable during development to catch double-release bugs; disable in release builds for performance.

**Additional pool types in `UnityEngine.Pool`:**
- `LinkedPool<T>` -- uses a linked list internally; lower memory overhead than `ObjectPool<T>` when pool size fluctuates significantly
- `DictionaryPool<TKey, TValue>` -- pools `Dictionary` instances to avoid allocation when you need temporary dictionaries
- `HashSetPool<T>` -- pools `HashSet` instances for temporary set operations
- `ListPool<T>` -- pools `List` instances (common for temporary query results)
- `GenericPool<T>` / `CollectionPool<TCollection, TItem>` -- base utilities for custom collection pooling

**Critical rules:**
- Always reset object state on release (velocities, animations, transforms, timers, particle state)
- Profile before implementing -- do not pool without evidence of GC pressure
- `defaultCapacity` is the initial backing collection size, `maxSize` caps total instances (excess are destroyed)

**Rigidbody velocity property name:**
- Unity 6+: `rb.linearVelocity` / `rb.angularVelocity`
- Unity 2022 LTS: `rb.velocity` / `rb.angularVelocity`

### When to Use

- Objects are spawned and destroyed frequently (particles, projectiles, sound effects)
- Profiler shows GC allocation spikes from Instantiate/Destroy patterns
- Object creation is expensive (complex prefab hierarchies, initialization)

### When NOT to Use

- Objects are created once and persist for the scene lifetime -- pooling adds complexity for no benefit
- Fewer than ~10 total instances over the scene's lifetime -- GC impact is negligible
- Object state is extremely difficult to reset reliably (complex component graphs with hidden state) -- stale state bugs may cost more than GC spikes

---

## Pattern Compatibility Matrix

Patterns frequently combine. Each row shows a proven combination with an RC racing context.

| Combination | How They Work Together | RC Racing Example |
|-------------|----------------------|-------------------|
| Factory + Pool | Factory pulls from pool instead of Instantiate | Obstacle factory reuses cone/barrier instances across races |
| Observer + MVP | Model emits events, Presenter formats and updates View | Speed model fires `OnSpeedChanged`, presenter formats "85 km/h" for HUD |
| State + Command | Each state generates commands for undo/redo history | Level editor: placing track pieces is a command, each editor state (place/delete/rotate) generates different commands |
| Strategy + SO | Strategies as ScriptableObject assets, swapped via Inspector | Surface physics SOs assigned per terrain material |
| Flyweight + SO | Shared data in SO, unique data in MonoBehaviour | Track segment data SO referenced by hundreds of placed segment instances |
| Dirty Flag + Observer | Flag set by event, checked on demand | Checkpoint event sets standings dirty; leaderboard UI reads on next frame |
| Command + Pool | Reuse command objects to avoid per-frame allocation | Input frame commands pooled during ghost car recording |
| State + Observer | State machine fires `OnStateChanged` for UI/audio reactions | Vehicle state change notifies HUD (show "AIRBORNE" label) and audio (switch engine sound) |
| Factory + Strategy | Factory selects creation strategy based on config | Vehicle factory uses different assembly strategies for buggy vs. truck vs. truggy |
| Observer + SO Event Channel | SO-based events decouple across scenes | `OnRaceFinished` SO channel notifies both in-race HUD and post-race results scene |

---

## Quick Reference: Which Pattern Do I Need?

```
Need to decouple event sender from receivers?
  --> Observer Pattern (Action events for same-object, SO Event Channels for cross-scene)

Need to manage complex object states with transitions?
  --> State Pattern (IState + StateMachine)

Need to create objects without exposing concrete types?
  --> Factory Pattern (abstract factory or dictionary lookup)

Need undo/redo or queued actions?
  --> Command Pattern (ICommand + undo/redo stacks)
  --> NOTE: For ghost car replay, use state recording (see unity-replay-ghost), NOT input commands

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

Need scene-decoupled pub/sub messaging?
  --> SO Event Channel (see unity-scriptable-objects skill)

Need to track active objects of a type without FindObjectsOfType?
  --> SO Runtime Set (see unity-scriptable-objects skill)

Need to store configuration data separate from behavior?
  --> SO Data Container (see unity-scriptable-objects skill)

Need type-safe "enum" values that survive refactoring?
  --> SO-Based Enum (see unity-scriptable-objects skill)
```

**WARNING: ScriptableObjects retain runtime state in the Editor.** When using RuntimeSetSO or any SO that stores runtime data, the Items list is NOT cleared automatically when exiting Play Mode. You must clear it manually:

```csharp
public abstract class RuntimeSetSO<T> : ScriptableObject
{
    [HideInInspector]
    public List<T> Items = new();

    private void OnDisable()
    {
        // Called when exiting Play Mode in Editor -- prevents stale data
        Items.Clear();
    }
}
```

This applies to any SO that accumulates runtime state. Without this cleanup, re-entering Play Mode starts with stale data from the previous run.

---

## Related Skills

| Skill | Relationship |
|-------|-------------|
| `unity-csharp-mastery` | C# lifecycle, attributes, naming conventions, anti-patterns |
| `unity-scriptable-objects` | Extended SO patterns: event channels, runtime sets, SO-based enums, delegate objects |
| `unity-state-machines` | Advanced FSM: hierarchical states, Animator integration |
| `unity-composition` | Component architecture, dependency injection, interfaces |
| `unity-performance-optimization` | Profiling, batching, GC reduction, LOD |
| `unity-testing-patterns` | Unit testing patterns for these architectures |
| `unity-project-foundations` | .asmdef setup, folder structure, YAML serialization, workflow optimization |
| `unity-3d-world-building` | NavMesh, terrain, level design, AI navigation |
| `unity-editor-scripting` | Custom inspectors for SO-heavy architectures |
| `unity-ui-toolkit` | UXML/USS for MVC/MVVM data binding |
