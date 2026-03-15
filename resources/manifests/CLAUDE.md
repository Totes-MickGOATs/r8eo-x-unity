# System Manifests

System manifests declare which files belong to which game system. Every game system should have a manifest file here.

## Format

Manifests can be JSON (engine-agnostic) or `.tres` (Godot-specific).

### JSON Format (Recommended)

```json
{
  "name": "system_name",
  "description": "What this system does",
  "status": "ACTIVE",
  "files": [
    "scripts/systems/my_system.gd",
    "scenes/my_system.tscn"
  ],
  "dependencies": ["other_system"],
  "replaced_by": "",
  "tests": {
    "editmode": ["MySystemTests"],
    "playmode": ["MySystemIntegrationTests"]
  }
}
```

### `tests` Field

| Key | Type | Description |
|-----|------|-------------|
| `tests.editmode` | `string[]` | EditMode test class names (without `.cs` extension) owned by this module |
| `tests.playmode` | `string[]` | PlayMode test class names owned by this module |

Values are test **class names** matching the `.cs` filename without extension.
Used by `scripts/tools/resolve_module_tests.py` to determine which tests to run
when files in this module change. Empty arrays are valid (no test coverage declared
will emit a validator warning if the module has source files).

### Status Values

| Status | Meaning |
|--------|---------|
| `ACTIVE` | In use, maintained |
| `DEPRECATED` | Replaced, do not modify |
| `EXPERIMENTAL` | WIP, not integrated |

## Validation

Run `just validate-registry` to check all manifests are valid and all declared files exist.
