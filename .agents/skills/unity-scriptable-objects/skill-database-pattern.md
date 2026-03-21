# Database Pattern

> Part of the `unity-scriptable-objects` skill. See [SKILL.md](SKILL.md) for the overview.

## Database Pattern

For medium-sized collections, a single SO holding an array is cleaner than hundreds of individual SO assets.

```csharp
[CreateAssetMenu(menuName = "Database/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class ItemEntry
    {
        public string Id;
        public string DisplayName;
        public Sprite Icon;
        public int BasePrice;
        public ItemCategory Category;
    }

    [SerializeField] private List<ItemEntry> _items = new();

    // Runtime lookup cache (built on first access)
    private Dictionary<string, ItemEntry> _lookup;

    public ItemEntry GetItem(string id)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<string, ItemEntry>();
            foreach (var item in _items)
                _lookup[item.Id] = item;
        }
        return _lookup.TryGetValue(id, out var entry) ? entry : null;
    }

    public IReadOnlyList<ItemEntry> AllItems => _items;
}
```

