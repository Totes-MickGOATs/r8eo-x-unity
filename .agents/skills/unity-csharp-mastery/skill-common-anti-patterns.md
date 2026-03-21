# Common Anti-Patterns

> Part of the `unity-csharp-mastery` skill. See [SKILL.md](SKILL.md) for the overview.

## Common Anti-Patterns

```csharp
// BAD: Find in Update
private void Update()
{
    var player = GameObject.Find("Player");           // Searches entire hierarchy every frame
    var rb = GetComponent<Rigidbody>();               // Hash lookup every frame
    var enemies = FindObjectsOfType<Enemy>();          // Scans every object in scene
    if (gameObject.CompareTag("Player")) { }          // OK -- CompareTag is fine
    if (gameObject.tag == "Player") { }               // BAD -- allocates string
}

// GOOD: Cache everything in Awake/Start
private Rigidbody _rb;
private Transform _player;

private void Awake()
{
    _rb = GetComponent<Rigidbody>();
}

private void Start()
{
    _player = GameObject.FindWithTag("Player").transform;
}

// BAD: Instantiate/Destroy in gameplay (GC spikes)
private void Fire()
{
    var bullet = Instantiate(_bulletPrefab);
    Destroy(bullet, 3f);
}

// GOOD: Object pooling
private void Fire()
{
    var bullet = _bulletPool.Get();
    bullet.Launch(_direction);
    // Return to pool instead of Destroy
}

// BAD: Camera.main in Update (calls FindWithTag internally every time in older Unity)
private void Update()
{
    transform.LookAt(Camera.main.transform); // Cached since Unity 2020.2, but still a property call
}

// GOOD: Cache it
private Camera _mainCamera;
private void Awake() => _mainCamera = Camera.main;
```

## String Handling in Hot Paths

```csharp
// BAD: string concatenation in Update (allocates new strings every frame)
private void Update()
{
    _debugText.text = "Health: " + _health + " / " + _maxHealth;
    _debugText.text = $"FPS: {1f / Time.deltaTime:F1}";  // Also allocates
}

// GOOD: StringBuilder for frequently updated text
private readonly StringBuilder _sb = new(64);

private void Update()
{
    _sb.Clear();
    _sb.Append("Health: ").Append(_health).Append(" / ").Append(_maxHealth);
    _debugText.text = _sb.ToString();
}

// GOOD: Only update when value changes
private int _lastDisplayedHealth = -1;

private void Update()
{
    if (_health != _lastDisplayedHealth)
    {
        _lastDisplayedHealth = _health;
        _healthText.text = $"Health: {_health} / {_maxHealth}";
    }
}
```

