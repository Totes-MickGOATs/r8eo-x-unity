# Opponent LOD Audio

> Part of the `unity-rc-audio` skill. See [SKILL.md](SKILL.md) for the overview.

## Opponent LOD Audio

For performance, simplify opponent audio based on distance from the listener.

### Distance-Based LOD

```csharp
public class OpponentAudioLOD : MonoBehaviour
{
    [SerializeField] private AudioSource motorSource;
    [SerializeField] private float disableDistance = 25f;
    [SerializeField] private float fullVolumeDistance = 5f;
    [SerializeField] private float fadeStartDistance = 20f;

    private Transform listener;

    private void Update()
    {
        float distance = Vector3.Distance(transform.position, listener.position);

        if (distance > disableDistance)
        {
            motorSource.enabled = false;
            return;
        }

        motorSource.enabled = true;
        motorSource.volume = Mathf.InverseLerp(fadeStartDistance, fullVolumeDistance, distance)
                           * baseVolume;
    }
}
```

### LOD Guidelines

| Distance | Audio Level |
|----------|------------|
| 0-5m | Full detail: motor + servo + tires |
| 5-15m | Motor only, full volume |
| 15-20m | Motor only, fading volume |
| 20-25m | Motor only, minimal volume |
| 25m+ | AudioSource disabled entirely |

- Only **1 AudioSource per opponent** at distance — the motor source
- Servo and tire audio are inaudible beyond 5-10m; disable them early
- With 8+ opponents, this saves significant CPU on audio processing

---

