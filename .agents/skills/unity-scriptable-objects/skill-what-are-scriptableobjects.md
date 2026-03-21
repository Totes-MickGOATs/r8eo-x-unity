# What Are ScriptableObjects?

> Part of the `unity-scriptable-objects` skill. See [SKILL.md](SKILL.md) for the overview.

## What Are ScriptableObjects?

ScriptableObjects (SOs) are data containers that exist as `.asset` files in your project. Unlike MonoBehaviours, they do not live on GameObjects in scenes. They are:

- **Shared** -- multiple objects can reference the same SO instance
- **Persistent** -- survive scene loads (they are project assets, not scene objects)
- **Inspector-editable** -- designers can tweak values without touching code
- **Lightweight** -- no Transform, no GameObject overhead

```csharp
// A simple ScriptableObject
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] private string _weaponName;
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _fireRate = 1f;
    [SerializeField] private float _range = 50f;

    [Header("Audio")]
    [SerializeField] private AudioClip _fireSound;
    [SerializeField] private AudioClip _reloadSound;

    [Header("Visuals")]
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Sprite _icon;

    // Public read-only access
    public string WeaponName => _weaponName;
    public int Damage => _damage;
    public float FireRate => _fireRate;
    public float Range => _range;
    public AudioClip FireSound => _fireSound;
    public GameObject Prefab => _prefab;
    public Sprite Icon => _icon;
}
```

**Creating assets:** Right-click in Project window -> Create -> Game -> Weapon Data. The `[CreateAssetMenu]` attribute controls menu path and default filename.

