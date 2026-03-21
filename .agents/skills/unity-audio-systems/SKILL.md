---
name: unity-audio-systems
description: Unity Audio Systems
---


# Unity Audio Systems

Use this skill when implementing audio playback, configuring AudioMixers, setting up spatial audio, or building music and sound effect systems in Unity.

## Audio in Timeline

Add **Audio Track** to a Timeline:
1. Create Audio Track, bind to an AudioSource
2. Drag AudioClips onto the track at desired times
3. Adjust volume curves per clip in the Timeline editor
4. Clips can overlap for crossfades

```csharp
// The bound AudioSource handles playback automatically.
// For dynamic audio events, use Signal Track instead:
// Signal Track -> Signal Emitter -> SignalReceiver component -> method call
```



## Topic Pages

- [Core Components](skill-core-components.md)
- [Common Patterns](skill-common-patterns.md)

