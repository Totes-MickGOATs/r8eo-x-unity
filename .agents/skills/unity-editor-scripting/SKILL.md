---
name: unity-editor-scripting
description: Unity Editor Scripting
---


# Unity Editor Scripting

Use this skill when building custom inspectors, editor windows, property drawers, build pipeline extensions, or other tools using the UnityEditor API.

## Undo System

Always use Undo for editor operations so users can Ctrl+Z:

```csharp
// Record changes to existing object
Undo.RecordObject(transform, "Move Object");
transform.position = newPosition;

// Register newly created objects
var go = new GameObject("New Object");
Undo.RegisterCreatedObjectUndo(go, "Create Object");

// Register object destruction
Undo.DestroyObjectImmediate(gameObject);

// Group multiple operations into one undo step
Undo.SetCurrentGroupName("Complex Operation");
int group = Undo.GetCurrentGroup();
// ... multiple operations ...
Undo.CollapseUndoOperations(group);

// Register complete object state (for complex changes)
Undo.RegisterFullObjectHierarchyUndo(root, "Modify Hierarchy");
```

## EditorPrefs and SessionState

```csharp
// EditorPrefs — persists between editor sessions (stored in registry/plist)
EditorPrefs.SetString("MyTool_LastPath", path);
string lastPath = EditorPrefs.GetString("MyTool_LastPath", defaultValue: "Assets/");

// SessionState — persists only during current editor session
SessionState.SetBool("MyTool_ShowAdvanced", true);
bool showAdvanced = SessionState.GetBool("MyTool_ShowAdvanced", false);
```



## Topic Pages

- [Custom Inspector](skill-custom-inspector.md)
- [Build Pipeline](skill-build-pipeline.md)

