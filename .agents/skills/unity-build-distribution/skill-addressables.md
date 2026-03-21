# Addressables

> Part of the `unity-build-distribution` skill. See [SKILL.md](SKILL.md) for the overview.

## Addressables

### Group Strategy

Group assets by loading context, not by type:

| Group | Contents | Load When |
|-------|----------|-----------|
| `Persistent_Shared` | UI atlas, common materials, player vehicle | Boot, kept in memory |
| `Track_Outpost_Assets` | Outpost terrain, props, lighting | Entering Outpost track |
| `Track_Desert_Assets` | Desert terrain, props, lighting | Entering Desert track |
| `Vehicles_Common` | Shared vehicle parts, physics materials | Vehicle selection |

### Content Update Workflow

For post-release patches without requiring a full reinstall:

1. Build Addressables with **Build Remote Catalog** enabled
2. For patches: **Update a Previous Build** (do not do a clean build)
3. Upload changed bundles to CDN
4. Game checks remote catalog on launch, downloads only changed bundles

