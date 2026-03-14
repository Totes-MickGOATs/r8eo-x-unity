# Assets/Scripts/Editor/

Editor-only scripts: menu items, debug tools, and scene setup automation. These scripts only run inside the Unity Editor and are excluded from builds.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `SceneSetup.cs` | `SceneSetup` | Menu item to create/configure the TestTrack scene with terrain, lighting, and vehicle |
| `OutpostTrackSetup.cs` | `OutpostTrackSetup` | Menu item to create/configure the OutpostTrack scene |
| `R8EOX.Editor.asmdef` | — | Assembly definition (Editor-only platform) |

## Conventions

- Namespace: `R8EOX.Editor`
- All scripts use `[MenuItem]` attributes for Unity menu integration
- Assembly definition targets Editor platform only — excluded from player builds

## Relevant Skills

- **`unity-editor-scripting`** — Custom editor tools, menu items, and inspector extensions
