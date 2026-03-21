# Schema Versioning

> Part of the `unity-save-settings` skill. See [SKILL.md](SKILL.md) for the overview.

## Schema Versioning

Every save file must have `schemaVersion` at the root:

```json
{
    "schemaVersion": 3,
    "masterVolume": 0.8,
    "resolution": { "width": 1920, "height": 1080 }
}
```

On load, run a migration pipeline:

```csharp
while (data.schemaVersion < CurrentVersion)
{
    switch (data.schemaVersion)
    {
        case 1: MigrateV1ToV2(data); break;
        case 2: MigrateV2ToV3(data); break;
    }
    data.schemaVersion++;
}
```

Each migration method is a pure function that transforms the data in place. Never skip versions — always migrate sequentially.

