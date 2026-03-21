# Leaderboards

> Part of the `unity-save-settings` skill. See [SKILL.md](SKILL.md) for the overview.

## Leaderboards

Per-track leaderboard files with a 10-entry cap:

```csharp
[Serializable]
public class TrackLeaderboard
{
    public int schemaVersion;
    public string trackId;
    public List<LeaderboardEntry> entries; // max 10, sorted ascending by time
}

[Serializable]
public class LeaderboardEntry
{
    public float time;
    public string vehicleId;
    public string date;
    public string ghostFileName; // reference to ghosts/ directory
}
```

Insertion strategy:
1. Add new entry to list
2. Sort by time ascending
3. Trim to 10 entries
4. If a ghost reference was removed by trim, delete the ghost file

