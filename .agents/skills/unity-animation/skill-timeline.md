# Timeline

> Part of the `unity-animation` skill. See [SKILL.md](SKILL.md) for the overview.

## Timeline

Cinematic sequencing system for scripted scenes.

### Core Components

| Component | Purpose |
|-----------|---------|
| PlayableDirector | Controls playback, binds tracks to scene objects |
| Timeline Asset | Reusable sequence of tracks (.playable file) |

### Track Types

| Track | Purpose |
|-------|---------|
| Animation Track | Play clips on an Animator |
| Activation Track | Enable/disable GameObjects on a schedule |
| Audio Track | Play AudioClips |
| Signal Track | Fire events at specific times |
| Cinemachine Track | Control camera shots (via CinemachineBrain) |
| Control Track | Manage sub-timelines, particle systems, ITimeControl |

```csharp
// Controlling Timeline from code
[SerializeField] PlayableDirector _director;

void StartCutscene()
{
    _director.Play();
}

void SkipCutscene()
{
    _director.time = _director.duration;
    _director.Evaluate();
    _director.Stop();
}

// Signal receiver -- responds to Signal Track events
public class CutsceneSignalReceiver : MonoBehaviour, INotificationReceiver
{
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        // Handle signal
    }
}
```

