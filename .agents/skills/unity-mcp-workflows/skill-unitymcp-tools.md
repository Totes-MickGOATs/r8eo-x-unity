# UnityMCP Tools

> Part of the `unity-mcp-workflows` skill. See [SKILL.md](SKILL.md) for the overview.

## UnityMCP Tools

UnityMCP provides structured, domain-specific tools for Unity Editor manipulation.

### Scene Management — `manage_scene`

```
action: "get_hierarchy"
  page_size: 50          # paginate large scenes
  cursor: "next_page_id" # from previous response

action: "load"
  path: "Assets/Scenes/MainMenu.unity"

action: "save"
  # saves the currently open scene
```

**Best practice:** Always use `get_hierarchy` with `page_size` for scenes with many objects. Start with `page_size: 50` and paginate if needed.

### GameObject Operations — `manage_gameobject`

```
action: "create"
  name: "Player"
  parent: "GameWorld"        # optional parent by name
  position: [0, 1, 0]
  rotation: [0, 0, 0]
  scale: [1, 1, 1]

action: "modify"
  name: "Player"
  position: [5, 0, 3]

action: "get_components"
  name: "Player"
  include_properties: false  # start with false for overview
  page_size: 20

action: "get_components"
  name: "Player"
  include_properties: true   # then true for specific object details
```

**Important:** Use `include_properties: false` first to see what components exist. Then query with `include_properties: true` only when you need property values. This avoids huge responses.

### Component Operations — `manage_components`

```
action: "add"
  game_object: "Player"
  component_type: "Rigidbody"

action: "modify"
  game_object: "Player"
  component_type: "Rigidbody"
  properties: {
    "mass": 2.0,
    "linearDamping": 0.5,
    "useGravity": true,
    "isKinematic": false
  }

action: "remove"
  game_object: "Player"
  component_type: "AudioSource"
```

### Finding Objects — `find_gameobjects`

```
# Search by name (partial match)
search_type: "name"
query: "Enemy"

# Search by tag
search_type: "tag"
query: "Player"

# Search by layer
search_type: "layer"
query: "UI"

# Search by component type
search_type: "component"
query: "Rigidbody"
```

### Script Management — `manage_script`

```
action: "create"
  name: "PlayerController"
  path: "Assets/Scripts/Player"
  template: "MonoBehaviour"    # or "ScriptableObject", "Editor", etc.

action: "modify"
  path: "Assets/Scripts/Player/PlayerController.cs"
  content: "... full file content ..."
```

**Critical workflow after script changes:**

1. Create or modify the script
2. Call `read_console` to check for compilation errors
3. Wait if `editor_state.isCompiling` is true
4. Only proceed when compilation succeeds

### Console — `read_console`

```
# Check for errors after script changes
log_type: "error"     # "log", "warning", "error", or "all"
count: 10             # number of recent entries

# Always check after:
# - Creating a new script
# - Modifying an existing script
# - Adding/removing components that depend on scripts
```

### Editor Control — `manage_editor`

```
action: "play"    # Enter Play mode
action: "pause"   # Pause Play mode
action: "stop"    # Exit Play mode
action: "build"   # Build the project
  platform: "StandaloneWindows64"
  path: "Builds/Game.exe"
```

### Asset Management — `manage_asset`

```
action: "search"
  query: "Player"
  type: "Prefab"    # "Prefab", "Material", "Texture", "Script", etc.

action: "import"
  path: "Assets/Models/character.fbx"

action: "create"
  type: "Material"
  name: "PlayerMaterial"
  path: "Assets/Materials"
```

### Visual Pipeline Tools

```
# Materials
manage_material:
  action: "create" / "modify"
  name: "MetalFloor"
  shader: "Universal Render Pipeline/Lit"
  properties: { "_BaseColor": [0.8, 0.8, 0.8, 1.0], "_Metallic": 0.9 }

# Textures
manage_texture:
  action: "get_info" / "modify_import"
  path: "Assets/Textures/floor_albedo.png"

# Shaders
manage_shader:
  action: "list" / "get_properties"

# Prefabs
manage_prefabs:
  action: "create" / "modify" / "get_info"

# UI
manage_ui:
  action: "create" / "modify"
  # Canvas, panels, buttons, etc.

# VFX
manage_vfx:
  action: "create" / "modify"
  # Particle systems, VFX Graph
```

