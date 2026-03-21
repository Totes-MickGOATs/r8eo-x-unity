---
name: unity-project-foundations
description: Unity Project Foundations
---


# Unity Project Foundations

Use this skill when setting up project folder structure, configuring assembly definitions, managing packages, or establishing code style and version control conventions for a Unity project.

## Editor Settings Checklist

Before first commit, verify these settings:

| Setting | Location | Required Value |
|---------|----------|----------------|
| Serialization Mode | Editor Settings | Force Text |
| Visible Meta Files | Editor Settings | Visible (version control mode) |
| Default Behavior Mode | Editor Settings | 3D or 2D (match your project) |
| Color Space | Player Settings | Linear (for PBR) or Gamma |
| Scripting Backend | Player Settings | IL2CPP (for builds) or Mono (for dev) |
| API Compatibility | Player Settings | .NET Standard 2.1 or .NET Framework |
| Enter Play Mode Settings | Project Settings | Enabled, with Domain/Scene Reload disabled for fast iteration |

### Enter Play Mode Settings

Edit -> Project Settings -> Editor -> Enter Play Mode Settings.

When enabled with "Reload Domain" unchecked, entering play mode is near-instant. But you must handle static state manually:

```csharp
public class ScoreManager : MonoBehaviour
{
    private static int _totalScore;

    // Reset static state when entering play mode without domain reload
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _totalScore = 0;
    }
}
```

**Rule:** Every class with `static` fields needs a `[RuntimeInitializeOnLoadMethod]` reset method when using fast enter play mode. Without it, static state bleeds between play sessions.

## Common Mistakes

1. **No assembly definitions** -- every change recompiles everything, 30-second iteration time
2. **Binary serialization mode** -- impossible to diff or merge scenes
3. **No .gitignore** -- Library/ (10+ GB) gets committed, repo becomes unusable
4. **No Git LFS** -- textures and models bloat the repo permanently (git history keeps every version)
5. **Scripts in Assets root** -- no namespace boundaries, everything depends on everything
6. **Using Resources folder liberally** -- everything in Resources is included in the build
7. **No .editorconfig** -- inconsistent code style, noisy diffs from formatting changes
8. **Skipping Enter Play Mode Settings** -- waiting 5-10 seconds to test every change


## Topic Pages

- [Recommended Folder Structure](skill-recommended-folder-structure.md)
- [Build Pipeline Basics](skill-build-pipeline-basics.md)

