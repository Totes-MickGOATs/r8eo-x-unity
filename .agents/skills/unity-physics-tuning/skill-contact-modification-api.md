# Contact Modification API

> Part of the `unity-physics-tuning` skill. See [SKILL.md](SKILL.md) for the overview.

## Contact Modification API

For advanced tire friction, use `Physics.ContactModifyEvent` (Unity 2022.2+):

```csharp
void OnEnable()
{
    Physics.ContactModifyEvent += OnContactModify;
}

void OnContactModify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
{
    for (int i = 0; i < pairs.Length; i++)
    {
        var pair = pairs[i];
        // Modify friction, restitution, or contact normal per-contact
        for (int j = 0; j < pair.contactCount; j++)
        {
            pair.SetDynamicFriction(j, customFriction);
            pair.SetStaticFriction(j, customStaticFriction);
        }
    }
}
```

**Use case:** Runtime friction that varies per-contact based on tire slip angle, surface wetness, or tire temperature. More accurate than per-material friction for racing.

---

