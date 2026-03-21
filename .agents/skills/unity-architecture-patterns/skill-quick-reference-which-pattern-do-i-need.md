# Quick Reference: Which Pattern Do I Need?

> Part of the `unity-architecture-patterns` skill. See [SKILL.md](SKILL.md) for the overview.

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

