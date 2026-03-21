---
name: unity-composition
description: Unity Composition Patterns
---


# Unity Composition Patterns

Use this skill when structuring Unity code with component architecture, interface-based design, dependency injection, or cross-object communication patterns.

## When to Use Inheritance

Inheritance is not forbidden -- it is useful for:

```csharp
// Base class for common MonoBehaviour boilerplate
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = (T)(MonoBehaviour)this;
        DontDestroyOnLoad(gameObject);
    }
}

// Clean singleton declaration
public class AudioManager : Singleton<AudioManager>
{
    protected override void Awake()
    {
        base.Awake();
        // AudioManager-specific init
    }
}
```

```csharp
// Abstract base for similar UI screens
public abstract class MenuScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;

    public virtual void Show()
    {
        gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    public virtual void Hide()
    {
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
}

public class MainMenuScreen : MenuScreen
{
    public override void Show()
    {
        base.Show();
        // Refresh leaderboard, etc.
    }
}
```

**Rule of thumb:** Use inheritance for framework-level base classes (singletons, UI screens, state bases). Use composition for gameplay entities (enemies, weapons, interactables).

## Architecture Decision Guide

| Situation | Pattern |
|-----------|---------|
| Components on same GameObject | Direct reference via GetComponent (cached) |
| Parent-child components | GetComponentInChildren / GetComponentInParent |
| Same entity, loose coupling | C# events (subscribe in OnEnable, unsubscribe in OnDisable) |
| Cross-entity, same scene | ScriptableObject event channels or direct reference |
| Cross-scene | ScriptableObject event channels or Service Locator |
| Global services (audio, input) | Singleton or Service Locator |
| Large project, testability | DI framework (VContainer) |
| Spawned objects need references | Init method injection |
| Entity variants (easy/hard enemy) | Prefab variants |
| Entity types (enemy/turret/trap) | Component composition on prefabs |


## Topic Pages

- [Composition Over Inheritance](skill-composition-over-inheritance.md)

