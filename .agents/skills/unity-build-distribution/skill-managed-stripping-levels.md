# Managed Stripping Levels

> Part of the `unity-build-distribution` skill. See [SKILL.md](SKILL.md) for the overview.

## Managed Stripping Levels

IL2CPP strips unused code to reduce binary size. Set in Player Settings > Other Settings:

| Level | Behavior | Risk |
|-------|----------|------|
| Minimal | Almost nothing stripped | Safe, large binary |
| **Low** | Conservative strip | **Safe default for shipping** |
| Medium | Aggressive strip | May break reflection |
| High | Maximum strip | Will break reflection-heavy code |

Use **Low** as the shipping default. If you need Medium/High, create a `link.xml` to preserve reflection-accessed types:

```xml
<linker>
    <assembly fullname="Assembly-CSharp">
        <type fullname="MyNamespace.SerializedClass" preserve="all"/>
    </assembly>
    <assembly fullname="UnityEngine.InputModule" preserve="all"/>
</linker>
```

Place `link.xml` in the `Assets/` root.

