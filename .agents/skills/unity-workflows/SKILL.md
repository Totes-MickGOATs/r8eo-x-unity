---
name: Unity Workflows
description: This skill should be used when the user asks about "Unity editor scripting", "Custom Inspector", "EditorWindow", "PropertyDrawer", "Unity Input System", "new Input System", "UI Toolkit", "uGUI", "Canvas", "asset management", "AssetDatabase", "build pipeline", "Editor utilities", or needs guidance on Unity editor extensions, input handling, UI systems, and workflow optimization.
version: 0.1.0
---


# Unity Workflows and Editor Tools

Essential workflows for Unity editor scripting, input systems, UI development, and asset management.

## Overview

Efficient Unity workflows accelerate development and reduce errors. This skill covers editor customization, modern input handling, UI systems, and asset pipeline optimization.

**Core workflow areas:**
- Editor scripting and custom tools
- Input System (new and legacy)
- UI development (UI Toolkit, uGUI)
- Asset management and build pipeline

## Best Practices

✅ **DO:**
- Use Editor scripting to automate repetitive tasks
- Implement Custom Inspectors for complex components
- Use new Input System for multi-platform projects
- Optimize UI with separate canvases for static/dynamic content
- Use Addressables for large games and mobile
- Create editor tools for designers/artists
- Validate assets before builds

❌ **DON'T:**
- Hardcode editor paths (use AssetDatabase)
- Forget to unsubscribe from Input System actions
- Mix Layout Groups excessively (UI performance killer)
- Use Resources folder for new projects (use Addressables)
- Create UI in Update loop
- Skip editor validation for critical scripts

**Golden rule**: Automate workflows with editor tools. Time spent creating tools saves exponentially more time in iteration.

---

Apply these workflow optimizations and modern systems for efficient, scalable Unity development.


## Topic Pages

- [Editor Scripting Basics](skill-editor-scripting-basics.md)

