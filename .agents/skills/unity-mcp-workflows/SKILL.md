---
name: unity-mcp-workflows
description: Unity MCP Workflows
---


# Unity MCP Workflows

Use this skill when interacting with the Unity Editor from Claude Code via MCP tools, including scene manipulation, script management, test execution, and asset operations.

## Setup

UnityMCP is configured in `.mcp.json` at the project root:

```json
{
  "mcpServers": {
    "UnityMCP": {
      "command": "npx",
      "args": ["-y", "@anthropic/unity-mcp@latest"]
    }
  }
}
```

**Prerequisites:**
- Unity Editor must be open with the project loaded
- The corresponding Unity package/addon must be installed in the project
- Node.js/npx available on PATH

## Additional UnityMCP Tools

The following UnityMCP tools are available but not covered in detail above:

| Tool | Purpose |
|------|--------|
| `run_tests` | Run Unity Test Runner tests (EditMode/PlayMode) and retrieve results |
| `validate_script` | Check a C# script for compilation errors without modifying it |
| `batch_execute` | Execute multiple MCP tool calls in a single request for efficiency |
| `manage_probuilder` | Create and modify ProBuilder meshes (track elements, ramps, barriers) |

Use `run_tests` after script changes to verify nothing broke. Use `batch_execute` when you need to create multiple GameObjects or modify multiple components in sequence -- it reduces round-trips.

## Workflow Best Practices

### 1. Always Check Compilation After Script Changes

```
Step 1: manage_script action:"modify" (or create)
Step 2: read_console log_type:"error"
Step 3: If errors → fix and repeat
Step 4: If clean → proceed
```

### 2. Use Paging for Large Scenes

```
Step 1: manage_scene action:"get_hierarchy" page_size:50
Step 2: Check if there's a cursor for next page
Step 3: Continue paging until all objects are retrieved (or you found what you need)
```

### 3. Properties Query Strategy

```
Step 1: manage_gameobject action:"get_components" include_properties:false
        → See which components exist (lightweight response)
Step 2: manage_gameobject action:"get_components" include_properties:true
        → Only when you need actual property values
```

### 4. Check Editor State Before Proceeding

Before performing operations that depend on compilation:

```
Step 1: Check editor_state.isCompiling in responses
Step 2: If compiling, wait and re-check
Step 3: Only proceed when compilation is complete
```

### 5. Use Resources for Reads, Tools for Mutations

- **Reading** scene hierarchy, component data, file contents → use resource endpoints when available (faster, cached)
- **Modifying** objects, creating assets, changing properties → use tool endpoints (these modify state)

### 6. Use batch_execute for Multiple Operations

When you need to create multiple GameObjects or modify multiple components in sequence, use `batch_execute` to reduce round-trips:

```
batch_execute:
  calls:
    - tool: manage_gameobject
      params: { action: "set_component_property", ... }
    - tool: manage_gameobject
      params: { action: "set_component_property", ... }
```

## UnityMCP Tool Selection Guide

| Task | Tool |
|------|------|
| Scene hierarchy queries | `manage_scene` with paging |
| Component property reads | `manage_gameobject` with `include_properties` |
| Script creation/modification | `manage_script` |
| Running tests | `run_tests` |
| Validating C# without saving | `validate_script` |
| Multiple sequential operations | `batch_execute` |
| ProBuilder mesh work | `manage_probuilder` |
| Material/shader work | `manage_material` |

## Troubleshooting

| Issue | Solution |
|-------|----------|
| MCP server not connecting | Ensure Unity Editor is open and the MCP addon/package is installed and enabled |
| Tools return errors | Check `read_console` for Unity errors. Editor may need a script recompile. |
| Slow hierarchy queries | Use `page_size` parameter, start with 50. Don't query `include_properties: true` unnecessarily. |
| Script changes not taking effect | Check `editor_state.isCompiling`. Wait for compilation to complete. |
| `execute_script` errors | Ensure the C# code is valid. Check for missing `using` statements. Results appear in read_console. |
| Objects not found by name | Names are case-sensitive. Use `find_gameobjects` to search. Hierarchy paths use `/` separators. |


## Topic Pages

- [UnityMCP Tools](skill-unitymcp-tools.md)

