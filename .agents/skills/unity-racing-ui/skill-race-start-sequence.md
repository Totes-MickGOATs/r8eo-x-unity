# Race Start Sequence

> Part of the `unity-racing-ui` skill. See [SKILL.md](SKILL.md) for the overview.

## Race Start Sequence

### F1-Style Traffic Lights

1. **Formation lap complete** — vehicles on grid, motor authority LOCKED (throttle input ignored).
2. **5 sequential red lights** — each light illuminates 1 second apart (5 seconds total).
3. **Random hold** — all 5 reds stay lit for a random duration (0.5-3.0 seconds). This prevents anticipation.
4. **Lights out** — all reds extinguish simultaneously. Motor authority UNLOCKED. GO.
5. **Jump start detection** — if any vehicle crosses the start line before lights out, apply a time penalty.

```csharp
// Race start sequence coroutine
public async Awaitable RunStartSequence()
{
    motorAuthority.Lock(); // Prevent throttle input

    for (int i = 0; i < 5; i++)
    {
        lights[i].SetRed(true);
        await Awaitable.WaitForSecondsAsync(1.0f);
    }

    float holdTime = Random.Range(0.5f, 3.0f);
    await Awaitable.WaitForSecondsAsync(holdTime);

    foreach (var light in lights)
        light.SetRed(false);

    motorAuthority.Unlock(); // GO
    raceTimer.Start();
}
```

---

