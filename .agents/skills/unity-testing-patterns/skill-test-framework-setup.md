# Test Framework Setup

> Part of the `unity-testing-patterns` skill. See [SKILL.md](SKILL.md) for the overview.

## Test Framework Setup

Unity Test Framework comes pre-installed. Tests live in assembly definitions:

```
Assets/
  Tests/
    EditMode/
      Game.Tests.EditMode.asmdef    ← Editor platform only
      TestPlayerStats.cs
      TestInventorySystem.cs
    PlayMode/
      Game.Tests.PlayMode.asmdef    ← Any platform (includes Editor)
      TestPlayerMovement.cs
      TestSceneLoading.cs
```

### Edit Mode Assembly Definition

```json
{
    "name": "Game.Tests.EditMode",
    "rootNamespace": "",
    "references": [
        "Game.Runtime",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "autoReferenced": false
}
```

### Play Mode Assembly Definition

```json
{
    "name": "Game.Tests.PlayMode",
    "rootNamespace": "",
    "references": [
        "Game.Runtime",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "autoReferenced": false
}
```

**Key difference:** Edit Mode tests set `includePlatforms: ["Editor"]`. Play Mode tests leave it empty (any platform).

## Edit Mode Tests — Pure Logic

Edit Mode tests run without entering Play mode. They are fast and ideal for testing:

- Pure C# classes and structs
- ScriptableObject data
- Serialization / deserialization
- Math utilities
- State machines (logic only)
- Data validation

```csharp
using NUnit.Framework;

[TestFixture]
public class TestPlayerStats
{
    private PlayerStats _stats;

    [SetUp]
    public void SetUp()
    {
        _stats = new PlayerStats(maxHealth: 100, maxStamina: 50);
    }

    [Test]
    public void InitialHealth_EqualsMaxHealth()
    {
        Assert.AreEqual(100, _stats.CurrentHealth);
    }

    [Test]
    public void TakeDamage_ReducesHealth()
    {
        _stats.TakeDamage(30);
        Assert.AreEqual(70, _stats.CurrentHealth);
    }

    [Test]
    public void TakeDamage_ClampsAtZero()
    {
        _stats.TakeDamage(999);
        Assert.AreEqual(0, _stats.CurrentHealth);
    }

    [Test]
    public void TakeDamage_NegativeAmount_ThrowsException()
    {
        Assert.Throws<System.ArgumentException>(() => _stats.TakeDamage(-10));
    }

    [Test]
    public void Heal_RestoresHealth()
    {
        _stats.TakeDamage(50);
        _stats.Heal(20);
        Assert.AreEqual(70, _stats.CurrentHealth);
    }

    [Test]
    public void Heal_DoesNotExceedMax()
    {
        _stats.TakeDamage(10);
        _stats.Heal(999);
        Assert.AreEqual(100, _stats.CurrentHealth);
    }

    [Test]
    public void IsDead_WhenHealthIsZero()
    {
        _stats.TakeDamage(100);
        Assert.IsTrue(_stats.IsDead);
    }

    [Test]
    public void IsDead_FalseWhenHealthAboveZero()
    {
        _stats.TakeDamage(99);
        Assert.IsFalse(_stats.IsDead);
    }
}
```

### Testing ScriptableObjects

```csharp
[TestFixture]
public class TestWeaponData
{
    [Test]
    public void DPS_CalculatedCorrectly()
    {
        var weapon = ScriptableObject.CreateInstance<WeaponData>();
        weapon.damage = 25;
        weapon.fireRate = 2f; // shots per second

        Assert.AreApproximatelyEqual(50f, weapon.DPS, 0.001f);

        Object.DestroyImmediate(weapon);
    }

    [Test]
    public void WeaponData_Serialization_RoundTrips()
    {
        var weapon = ScriptableObject.CreateInstance<WeaponData>();
        weapon.damage = 42;
        weapon.weaponName = "Blaster";

        string json = JsonUtility.ToJson(weapon);
        var restored = ScriptableObject.CreateInstance<WeaponData>();
        JsonUtility.FromJsonOverwrite(json, restored);

        Assert.AreEqual(42, restored.damage);
        Assert.AreEqual("Blaster", restored.weaponName);

        Object.DestroyImmediate(weapon);
        Object.DestroyImmediate(restored);
    }
}
```

## Play Mode Tests — Runtime Behavior

Play Mode tests run inside a real game loop. They test MonoBehaviours, physics, coroutines, and scene interactions.

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestUtils;
using System.Collections;

[TestFixture]
public class TestPlayerMovement
{
    private GameObject _playerObj;
    private PlayerMovement _player;
    private Rigidbody _rb;

    [SetUp]
    public void SetUp()
    {
        _playerObj = new GameObject("TestPlayer");
        _rb = _playerObj.AddComponent<Rigidbody>();
        _rb.useGravity = false; // isolate horizontal movement
        _player = _playerObj.AddComponent<PlayerMovement>();
        _player.moveSpeed = 5f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_playerObj);
    }

    [UnityTest]
    public IEnumerator MoveForward_ChangesPosition()
    {
        Vector3 startPos = _playerObj.transform.position;

        // Simulate input
        _player.SetMoveInput(Vector3.forward);

        // Wait several physics frames
        for (int i = 0; i < 10; i++)
            yield return new WaitForFixedUpdate();

        Vector3 endPos = _playerObj.transform.position;
        Assert.Greater(endPos.z, startPos.z, "Player should move forward on Z axis");
    }

    [UnityTest]
    public IEnumerator NoInput_PlayerStaysStill()
    {
        Vector3 startPos = _playerObj.transform.position;
        _player.SetMoveInput(Vector3.zero);

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.AreEqual(startPos, _playerObj.transform.position);
    }

    [UnityTest]
    public IEnumerator Sprint_IncreasesSpeed()
    {
        _player.SetMoveInput(Vector3.forward);
        _player.SetSprint(false);
        yield return new WaitForFixedUpdate();
        float normalSpeed = _rb.linearVelocity.magnitude;

        _player.SetSprint(true);
        for (int i = 0; i < 10; i++)
            yield return new WaitForFixedUpdate();

        float sprintSpeed = _rb.linearVelocity.magnitude;
        Assert.Greater(sprintSpeed, normalSpeed);
    }
}
```

### Testing Physics Interactions

```csharp
[UnityTest]
public IEnumerator Projectile_DestroysOnImpact()
{
    // Create wall
    var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
    wall.transform.position = new Vector3(0, 0, 5);
    wall.AddComponent<Rigidbody>().isKinematic = true;

    // Create projectile
    var projectileObj = new GameObject("Projectile");
    projectileObj.transform.position = Vector3.zero;
    var rb = projectileObj.AddComponent<Rigidbody>();
    rb.useGravity = false;
    var collider = projectileObj.AddComponent<SphereCollider>();
    var projectile = projectileObj.AddComponent<Projectile>();
    projectile.destroyOnImpact = true;

    // Fire toward wall
    rb.linearVelocity = Vector3.forward * 50f;

    // Wait for collision
    yield return new WaitForSeconds(0.5f);

    // Projectile should be destroyed
    Assert.IsTrue(projectileObj == null, "Projectile should be destroyed after impact");

    Object.Destroy(wall);
}
```

### Scene-Based Tests

```csharp
using UnityEngine.SceneManagement;

[UnityTest]
public IEnumerator GameScene_SpawnsPlayer()
{
    SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    yield return null; // wait one frame for scene load
    yield return null; // extra frame for Start() calls

    var player = GameObject.FindWithTag("Player");
    Assert.IsNotNull(player, "Player should be spawned in GameScene");
}

[UnityTest]
public IEnumerator MainMenu_PlayButton_LoadsGame()
{
    SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    yield return null;
    yield return null;

    var playButton = GameObject.Find("PlayButton").GetComponent<Button>();
    Assert.IsNotNull(playButton);

    // Simulate click
    playButton.onClick.Invoke();

    // Wait for scene transition
    yield return new WaitForSeconds(2f);

    Assert.AreEqual("GameScene", SceneManager.GetActiveScene().name);
}
```

## Assertions Reference

### NUnit Assertions

```csharp
// Equality
Assert.AreEqual(expected, actual);
Assert.AreNotEqual(unexpected, actual);

// Boolean
Assert.IsTrue(condition);
Assert.IsFalse(condition);

// Null
Assert.IsNull(obj);
Assert.IsNotNull(obj);

// Comparison
Assert.Greater(bigger, smaller);
Assert.Less(smaller, bigger);
Assert.GreaterOrEqual(a, b);

// Collections
Assert.Contains(item, collection);
CollectionAssert.AreEqual(expected, actual);       // same order
CollectionAssert.AreEquivalent(expected, actual);  // same items, any order
CollectionAssert.IsEmpty(collection);

// Exceptions
Assert.Throws<ArgumentException>(() => SomeMethod());
Assert.DoesNotThrow(() => SomeMethod());

// Constraint-based (more expressive)
Assert.That(value, Is.EqualTo(42));
Assert.That(value, Is.GreaterThan(0).And.LessThan(100));
Assert.That(list, Has.Count.EqualTo(5));
Assert.That(text, Does.Contain("error"));
Assert.That(value, Is.InRange(0f, 1f));
```

### Unity-Specific Assertions

```csharp
// Float comparison with tolerance (essential for physics)
Assert.AreApproximatelyEqual(expected, actual, tolerance);
// Default tolerance is 0.00001f

// Log assertions — verify Debug.Log was called
LogAssert.Expect(LogType.Error, "Expected error message");
LogAssert.Expect(LogType.Warning, new Regex("pattern.*match"));
// Test fails if the expected log is NOT emitted

// Ignore specific logs that would fail the test
LogAssert.ignoreFailingMessages = true; // use sparingly
```

## Setup and Teardown

```csharp
[TestFixture]
public class TestInventory
{
    private static ItemDatabase _database; // shared across all tests

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Runs once before ALL tests in this fixture
        _database = ScriptableObject.CreateInstance<ItemDatabase>();
        _database.Initialize();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Runs once after ALL tests
        Object.DestroyImmediate(_database);
    }

    [SetUp]
    public void SetUp()
    {
        // Runs before EACH test
    }

    [TearDown]
    public void TearDown()
    {
        // Runs after EACH test — clean up GameObjects, etc.
    }
}
```

## Mocking and Dependency Injection

Unity is not compatible with IL2CPP mocking frameworks (Moq, NSubstitute). Use interface-based manual stubs:

```csharp
// Production interface
public interface IInputProvider
{
    Vector2 GetMovement();
    bool GetJump();
    bool GetAttack();
}

// Production implementation
public class GameInputProvider : IInputProvider
{
    public Vector2 GetMovement() => new Vector2(
        Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    public bool GetJump() => Input.GetButtonDown("Jump");
    public bool GetAttack() => Input.GetButtonDown("Fire1");
}

// Test stub
public class MockInputProvider : IInputProvider
{
    public Vector2 Movement { get; set; }
    public bool Jump { get; set; }
    public bool Attack { get; set; }

    public Vector2 GetMovement() => Movement;
    public bool GetJump() => Jump;
    public bool GetAttack() => Attack;
}

// System under test accepts the interface
public class PlayerController : MonoBehaviour
{
    private IInputProvider _input;

    public void Initialize(IInputProvider input) => _input = input;

    private void Update()
    {
        var move = _input.GetMovement();
        // ... use movement
    }
}

// Test
[UnityTest]
public IEnumerator Player_MovesWithInput()
{
    var player = new GameObject().AddComponent<PlayerController>();
    var mockInput = new MockInputProvider { Movement = new Vector2(1, 0) };
    player.Initialize(mockInput);

    yield return null; // wait one frame for Update

    // Assert player moved right
    Assert.Greater(player.transform.position.x, 0);

    Object.Destroy(player.gameObject);
}
```

### Event/Signal Testing

```csharp
[Test]
public void OnDeath_Event_Fires_When_Health_Reaches_Zero()
{
    var stats = new PlayerStats(maxHealth: 100);
    bool deathFired = false;

    stats.OnDeath += () => deathFired = true;
    stats.TakeDamage(100);

    Assert.IsTrue(deathFired, "OnDeath event should fire when health reaches 0");
}

[Test]
public void OnDeath_Event_DoesNotFire_When_Alive()
{
    var stats = new PlayerStats(maxHealth: 100);
    bool deathFired = false;

    stats.OnDeath += () => deathFired = true;
    stats.TakeDamage(50);

    Assert.IsFalse(deathFired, "OnDeath should not fire when health is above 0");
}
```

