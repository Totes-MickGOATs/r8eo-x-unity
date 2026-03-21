# Object Pooling

> Part of the `unity-performance-optimization` skill. See [SKILL.md](SKILL.md) for the overview.

## Object Pooling

Avoid Instantiate/Destroy overhead by reusing objects:

```csharp
// Unity's built-in ObjectPool<T> (2021.1+)
using UnityEngine.Pool;

public class BulletPool : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;

    private ObjectPool<GameObject> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(bulletPrefab),
            actionOnGet: bullet => bullet.SetActive(true),
            actionOnRelease: bullet => bullet.SetActive(false),
            actionOnDestroy: bullet => Destroy(bullet),
            collectionCheck: false,
            defaultCapacity: 50,
            maxSize: 200
        );
    }

    public GameObject GetBullet()
    {
        return _pool.Get();
    }

    public void ReturnBullet(GameObject bullet)
    {
        _pool.Release(bullet);
    }
}
```

### Generic Pool Pattern (pre-2021)

```csharp
public class GenericPool<T> where T : Component
{
    private readonly Queue<T> _available = new();
    private readonly T _prefab;
    private readonly Transform _parent;

    public GenericPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            var obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            _available.Enqueue(obj);
        }
    }

    public T Get()
    {
        T obj = _available.Count > 0
            ? _available.Dequeue()
            : Object.Instantiate(_prefab, _parent);

        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Release(T obj)
    {
        obj.gameObject.SetActive(false);
        _available.Enqueue(obj);
    }
}
```

## Garbage Collection Optimization

GC spikes cause frame hitches. The goal: zero allocations in hot paths (Update, FixedUpdate).

### Common Allocation Sources and Fixes

```csharp
// ALLOCATES — string concatenation
void Update()
{
    scoreText.text = "Score: " + score; // creates new string every frame
}

// FIX — cache or use StringBuilder
private readonly StringBuilder _sb = new(32);
void Update()
{
    _sb.Clear();
    _sb.Append("Score: ").Append(score);
    scoreText.text = _sb.ToString();
}

// ALLOCATES — LINQ in hot path
void Update()
{
    var enemies = allEntities.Where(e => e.IsEnemy).ToList(); // allocates
}

// FIX — manual loop, reuse list
private readonly List<Entity> _enemyCache = new(64);
void Update()
{
    _enemyCache.Clear();
    for (int i = 0; i < allEntities.Count; i++)
    {
        if (allEntities[i].IsEnemy) _enemyCache.Add(allEntities[i]);
    }
}

// ALLOCATES — foreach on some collections (boxing)
foreach (var item in myHashSet) { } // may box enumerator

// FIX — use for loop where possible, or List<T> (no boxing)
for (int i = 0; i < myList.Count; i++) { }

// ALLOCATES — GetComponent every frame
void Update()
{
    var rb = GetComponent<Rigidbody>(); // allocates if done repeatedly
}

// FIX — cache in Awake/Start
private Rigidbody _rb;
void Awake() { _rb = GetComponent<Rigidbody>(); }

// ALLOCATES — new arrays/lists
void Update()
{
    var hits = Physics.RaycastAll(ray); // allocates array
}

// FIX — NonAlloc variants
private readonly RaycastHit[] _hitBuffer = new RaycastHit[32];
void Update()
{
    int count = Physics.RaycastNonAlloc(ray, _hitBuffer);
    for (int i = 0; i < count; i++) { /* use _hitBuffer[i] */ }
}

// ALLOCATES — closures/lambdas capturing variables
void Update()
{
    int id = entityId;
    DoAsync(() => ProcessEntity(id)); // captures 'id', allocates closure
}

// FIX — avoid lambdas in hot paths, use direct method references
```

### Incremental GC

Unity 2019.1+ supports incremental GC. Enable in Project Settings > Player > Configuration.

Spreads GC work across multiple frames instead of one big spike. Recommended for all projects.

## Physics Optimization

### Collision Matrix

In Project Settings > Physics, uncheck layer pairs that never collide:

```
             Terrain  Vehicles  Projectiles  UI
Terrain         x
Vehicles        x        x
Projectiles     x        x
UI
```

This reduces broadphase pair checks significantly.

### Simulation Settings

```
Fixed Timestep: 0.02 (50 Hz) — standard
                0.01 (100 Hz) — high precision (vehicles, fighting games)
                0.04 (25 Hz)  — low precision (mobile, casual)

Solver Iterations: 6 (default) — increase for stacking/joints
Solver Velocity Iterations: 1 — increase for fast-moving objects

Auto Sync Transforms: OFF (default) — only sync when Physics.SyncTransforms() or FixedUpdate
```

### Rigidbody Best Practices

- Use `Rigidbody.isKinematic = true` for objects that move but don't need physics simulation
- `Rigidbody.Sleep()` — manually sleep objects that have settled
- Reduce collider complexity: prefer box/sphere/capsule over mesh colliders
- Convex mesh colliders are much cheaper than concave
- Avoid moving static colliders (objects without Rigidbody) — this rebuilds the physics broadphase

## UI Optimization

### Canvas Splitting

Every change to a Canvas element dirties the ENTIRE canvas batch. Split canvases by update frequency:

```
Canvas (Static HUD)        — health bar frame, minimap border, rarely changes
Canvas (Dynamic HUD)       — health fill, score counter, changes every frame
Canvas (Floating Text)     — damage numbers, frequent create/destroy
```

### Raycast Target

Disable **Raycast Target** on every Image/Text that is not interactive:

```csharp
// In code
image.raycastTarget = false;
textMesh.raycastTarget = false;
```

This is the most common Unity UI performance mistake. Background images, decorative text, and icons should never have raycast enabled.

### Canvas Group for Visibility

```csharp
// WRONG — disabling Canvas rebuilds on re-enable
canvas.enabled = false; // triggers full rebuild when re-enabled

// BETTER — CanvasGroup alpha for show/hide
canvasGroup.alpha = 0f;           // invisible
canvasGroup.blocksRaycasts = false; // non-interactive
canvasGroup.interactable = false;

// BEST for frequently toggling — just move off-screen or use CanvasGroup
```

